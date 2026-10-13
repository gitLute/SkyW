using System.Text;
using SkyW.Abstractions.Logging;

namespace SkyW.Diagnostics.Logging;

/// <summary>
/// Приёмник сообщений, дописывающий их в файл.
/// </summary>
/// <remarks>
/// В отличие от консоли файл переживает перезапуск, поэтому открывается сразу и
/// пишется без буфера: иначе последние записи терялись бы именно в том запуске,
/// ради которого журнал и нужен.
/// </remarks>
/// <remarks>
/// Файл открыт на чтение и удаление извне: журнал можно смотреть во время игры,
/// не останавливая движок. Работа идёт через <see cref="FileStream"/> напрямую,
/// мимо слоя ассетов: путь задаёт хост при старте.
/// </remarks>
public sealed class FileLogSink : ILogSink, IDisposable
{
    /// <summary>
    /// UTF-8 без метки порядка байтов: метка ломает разбор журнала сторонними
    /// инструментами.
    /// </summary>
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private const int BufferSize = 1024;

    private readonly LogLevel _minimumLevel;
    private readonly LogFormatter _formatter;
    private readonly long _maxFileSizeBytes;
    private readonly int _retainedFiles;
    private readonly string _path;
    private readonly int _newLineByteCount;
    private readonly Lock _writeLock = new();

    private StreamWriter? _writer;
    private bool _disposed;

    /// <summary>
    /// Создаёт приёмник и открывает файл на дозапись.
    /// </summary>
    /// <param name="path">
    /// Недостающие каталоги создаются, существующий файл дописывается: несколько
    /// запусков подряд остаются в одном файле.
    /// </param>
    /// <param name="maxFileSizeBytes">
    /// Предел размера, ноль — без предела. Файл сворачивается перед строкой,
    /// которая в предел не влезет, поэтому свежий журнал не остаётся пустым.
    /// </param>
    /// <param name="retainedFiles">
    /// Сколько прошлых файлов хранить как <c>.1</c>, <c>.2</c> и так далее.
    /// Ноль вместе с ненулевым пределом — прошлый файл удаляется без сохранения.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Путь пустой либо предел размера или число хранимых файлов отрицательны.
    /// </exception>
    /// <exception cref="IOException">Файл не удалось открыть.</exception>
    /// <exception cref="UnauthorizedAccessException">Нет прав на файл.</exception>
    public FileLogSink(
        string path,
        LogLevel minimumLevel = LogLevel.Information,
        long maxFileSizeBytes = 0,
        int retainedFiles = 0,
        LogFormatter? formatter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentOutOfRangeException.ThrowIfNegative(maxFileSizeBytes);
        ArgumentOutOfRangeException.ThrowIfNegative(retainedFiles);

        _path = path;
        _minimumLevel = minimumLevel;
        _maxFileSizeBytes = maxFileSizeBytes;
        _retainedFiles = retainedFiles;
        _formatter = formatter ?? new LogFormatter();
        _newLineByteCount = Utf8NoBom.GetByteCount(Environment.NewLine);

        OpenWriter();
    }

    /// <summary>Путь к текущему файлу журнала.</summary>
    public string FilePath => _path;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel level) => level >= _minimumLevel;

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Текст сообщения не задан.</exception>
    /// <exception cref="ObjectDisposedException">Приёмник уже освобождён.</exception>
    public void Write(LogLevel level, string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!IsEnabled(level))
        {
            return;
        }

        string line = _formatter.Format(level, message);

        lock (_writeLock)
        {
            StreamWriter writer = Writer;
            long required = RequiredBytes(line);

            // Свёртка до записи: иначе файл переполнился бы первой же строкой,
            // ушёл в сторонний и остался пустым.
            if (_maxFileSizeBytes > 0
                && writer.BaseStream.Length > 0
                && writer.BaseStream.Length + required > _maxFileSizeBytes)
            {
                Roll();
                writer = Writer;
            }

            writer.WriteLine(line);
        }
    }

    /// <summary>Дописывает накопленное на диск.</summary>
    /// <remarks>
    /// Строки и так уходят на диск сразу, поэтому метод нужен только для проверки
    /// состояния после сбоя записи. После освобождения ничего не делает.
    /// </remarks>
    public void Flush()
    {
        lock (_writeLock)
        {
            _writer?.Flush();
        }
    }

    /// <summary>Закрывает файл. Повторный вызов безопасен.</summary>
    public void Dispose()
    {
        lock (_writeLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            CloseWriter();
        }
    }

    private StreamWriter Writer =>
        _writer ?? throw new ObjectDisposedException(nameof(FileLogSink));

    private void OpenWriter()
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(_path));

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        Stream stream = new FileStream(
            _path,
            FileMode.Append,
            FileAccess.Write,
            FileShare.ReadWrite | FileShare.Delete,
            BufferSize,
            FileOptions.None);

        _writer = new StreamWriter(stream, Utf8NoBom, BufferSize)
        {
            AutoFlush = true,
        };
    }

    private void CloseWriter()
    {
        StreamWriter? writer = _writer;
        _writer = null;

        if (writer is null)
        {
            return;
        }

        try
        {
            writer.Flush();
            writer.Dispose();
        }
        catch (IOException)
        {
            // Приёмник уже освобождается: исключение здесь затерело бы результат
            // освобождения, а изменить уже ничего нельзя.
        }
    }

    /// <summary>
    /// Сворачивает переполненный файл: уводит его в сторонний, сдвигает прежние
    /// и продолжает писать в новый.
    /// </summary>
    private void Roll()
    {
        CloseWriter();

        if (_retainedFiles > 0)
        {
            File.Delete(RolledPath(_retainedFiles));

            for (int index = _retainedFiles - 1; index >= 1; index--)
            {
                string from = RolledPath(index);

                if (File.Exists(from))
                {
                    File.Move(from, RolledPath(index + 1), overwrite: true);
                }
            }

            if (File.Exists(_path))
            {
                File.Move(_path, RolledPath(1), overwrite: true);
            }
        }
        else
        {
            File.Delete(_path);
        }

        OpenWriter();
    }

    private string RolledPath(int index) => $"{_path}.{index}";

    /// <summary>
    /// Размер строки вместе с переводом строки — столько места она займёт.
    /// </summary>
    /// <remarks>
    /// Перевод строки тот же, что у <see cref="StreamWriter"/>, поэтому расчёт
    /// совпадает с фактической записью.
    /// </remarks>
    private long RequiredBytes(string line) =>
        Utf8NoBom.GetByteCount(line) + _newLineByteCount;
}