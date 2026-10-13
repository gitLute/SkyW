using System.Text;
using SkyW.Abstractions.Logging;

namespace SkyW.Diagnostics.Logging;

// Файл переживает перезапуск движка: открывается сразу и пишется без буфера, иначе
// последние записи терялись бы в том запуске, ради которого журнал и нужен.
public sealed class FileLogSink : ILogSink, IDisposable
{
    // UTF-8 без метки порядка байтов: метка ломает разбор журнала сторонними
    // инструментами.
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

    // maxFileSizeBytes — предел размера, ноль без предела, retainedFiles — сколько
    // прошлых файлов хранить как .1, .2. Каталоги создаются, файл дописывается.
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

    public string FilePath => _path;

    public bool IsEnabled(LogLevel level) => level >= _minimumLevel;

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

    // Строки и так уходят на диск сразу: метод нужен только для проверки состояния
    // после сбоя записи.
    public void Flush()
    {
        lock (_writeLock)
        {
            _writer?.Flush();
        }
    }

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
            // Приёмник уже освобождается: исключение затерело бы результат освобождения,
            // а изменить уже ничего нельзя.
        }
    }

    // Свёртка: текущий файл уходит в сторонний, прежние сдвигаются, запись идёт в новый.
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

    // Перевод строки тот же, что у StreamWriter, поэтому расчёт совпадает с записью.
    private long RequiredBytes(string line) =>
        Utf8NoBom.GetByteCount(line) + _newLineByteCount;
}