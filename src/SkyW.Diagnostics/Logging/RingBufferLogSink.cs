using System.Text;
using SkyW.Abstractions.Logging;

namespace SkyW.Diagnostics.Logging;

// Хранит последние строки в памяти там, где файла нет: инструменты контента журналы
// не пишут, а хост при аварии хочет показать последние строки до гибели процесса.
public sealed class RingBufferLogSink : ILogSink
{
    // Ёмкость по умолчанию: сотни строк хватает на последние секунды работы.
    public const int DefaultCapacity = 256;

    private readonly LogLevel _minimumLevel;
    private readonly LogFormatter _formatter;
    private readonly string?[] _lines;
    private readonly Lock _writeLock = new();

    private int _next;
    private int _count;
    private long _accepted;

    // Ёмкость фиксирована: переполнение перезаписывает самую старую строку, а не
    // отбрасывает новую, и аллокаций на каждое сообщение не появляется.
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

    public int Capacity => _lines.Length;

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

    // Не сбрасывается при переполнении и в Clear: по нему видно, сколько сообщений
    // прошло за запуск.
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
            _lines[_next] = line;
            _next = (_next + 1) % _lines.Length;

            if (_count < _lines.Length)
            {
                _count++;
            }

            _accepted++;
        }
    }

    // Копия, а не срез массива: читатель не держит блокировку приёмника и может
    // работать со снимком, пока пишет поток симуляции.
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