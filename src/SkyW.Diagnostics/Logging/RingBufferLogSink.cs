using System.Text;
using SkyW.Abstractions.Logging;

namespace SkyW.Diagnostics.Logging;

/// <summary>
/// Приёмник сообщений, хранящий последние строки в памяти по кругу.
/// </summary>
/// <remarks>
/// Нужен там, где файла нет: инструменты контента не пишут журналы на диск, а
/// хост при аварийном завершении хочет показать последние строки до исчезновения
/// процесса.
/// </remarks>
/// <remarks>
/// Ёмкость фиксирована при создании и строки не выделяются заново: приёмник не
/// растёт и не делает аллокаций на каждом сообщении.
/// </remarks>
public sealed class RingBufferLogSink : ILogSink
{
    /// <summary>
    /// Ёмкость по умолчанию: две сотни строк помещают в буфер последние секунды
    /// работы и не занимают заметной памяти.
    /// </summary>
    public const int DefaultCapacity = 256;

    private readonly LogLevel _minimumLevel;
    private readonly LogFormatter _formatter;
    private readonly string?[] _lines;
    private readonly Lock _writeLock = new();

    private int _next;
    private int _count;
    private long _accepted;

    /// <summary>
    /// Создаёт приёмник с фиксированной ёмкостью.
    /// </summary>
    /// <param name="capacity">
    /// Сколько последних строк хранить: переполнение перезаписывает самую старую,
    /// а не отбрасывает новую.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">Ёмкость не положительна.</exception>
    public RingBufferLogSink(
        int capacity = DefaultCapacity,
        LogLevel minimumLevel = LogLevel.Information,
        LogFormatter? formatter = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        _lines = new string?[capacity];
        _minimumLevel = minimumLevel;
        _formatter = formatter ?? new LogFormatter();
    }

    /// <summary>Сколько строк буфер хранит всего.</summary>
    public int Capacity => _lines.Length;

    /// <summary>Сколько строк хранится сейчас: не больше <see cref="Capacity"/>.</summary>
    public int Count
    {
        get
        {
            lock (_writeLock)
            {
                return _count;
            }
        }
    }

    /// <summary>
    /// Сколько сообщений принято за всё время, включая затёртые переполнением.
    /// </summary>
    /// <remarks>
    /// Не сбрасывается при переполнении и в <see cref="Clear"/>: по нему видно,
    /// сколько сообщений прошло за запуск.
    /// </remarks>
    public long AcceptedCount
    {
        get
        {
            lock (_writeLock)
            {
                return _accepted;
            }
        }
    }

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel level) => level >= _minimumLevel;

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Текст сообщения не задан.</exception>
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
            _lines[_next] = line;
            _next = (_next + 1) % _lines.Length;

            if (_count < _lines.Length)
            {
                _count++;
            }

            _accepted++;
        }
    }

    /// <summary>
    /// Снимок хранимых строк от самой старой к самой новой.
    /// </summary>
    /// <returns>
    /// Копия содержимого: читатель не держит блокировку приёмника и может
    /// работать с ней, пока пишет поток симуляции.
    /// </returns>
    public IReadOnlyList<string> Snapshot()
    {
        lock (_writeLock)
        {
            string[] result = new string[_count];
            int start = (_next - _count + _lines.Length) % _lines.Length;

            for (int index = 0; index < _count; index++)
            {
                result[index] = _lines[(start + index) % _lines.Length]!;
            }

            return result;
        }
    }

    /// <summary>Снимок строк, склеенных переводом строки платформы.</summary>
    public string SnapshotText()
    {
        IReadOnlyList<string> lines = Snapshot();

        if (lines.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();

        for (int index = 0; index < lines.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(Environment.NewLine);
            }

            builder.Append(lines[index]);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Очищает буфер, не трогая счётчик принятых сообщений.
    /// </summary>
    public void Clear()
    {
        lock (_writeLock)
        {
            Array.Clear(_lines);
            _next = 0;
            _count = 0;
        }
    }
}