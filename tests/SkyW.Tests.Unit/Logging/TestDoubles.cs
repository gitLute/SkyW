using SkyW.Abstractions.Logging;
using Xunit;

namespace SkyW.Tests.Unit.Logging;

// Тесты, перехватывающие Console, идут без параллелизации: Console.SetOut меняет
// состояние процесса, и два таких теста сразу перехватывают вывод друг друга.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConsoleCollection
{
    public const string Name = "console";
}

internal sealed record LogEntry(LogLevel Level, string Message);

// Приёмник, копящий принятые сообщения: проверяет, что дошло и в каком порядке.
internal sealed class RecordingLogSink : ILogSink
{
    private readonly List<LogEntry> _entries = [];
    private readonly Lock _lock = new();

    public LogLevel MinimumLevel { get; init; } = LogLevel.Debug;

    public IReadOnlyList<LogEntry> Entries
    {
        get
        {
            lock (_lock)
            {
                return _entries.ToArray();
            }
        }
    }

    public bool IsEnabled(LogLevel level) => level >= MinimumLevel;

    public void Write(LogLevel level, string message)
    {
        lock (_lock)
        {
            _entries.Add(new LogEntry(level, message));
        }
    }
}

// Приёмник, который всегда падает: проверяет, что сбой вывода не роняет кадр.
internal sealed class ThrowingLogSink : ILogSink
{
    private int _writeAttempts;

    public int WriteAttempts => Volatile.Read(ref _writeAttempts);

    public bool IsEnabled(LogLevel level) => true;

    public void Write(LogLevel level, string message)
    {
        Interlocked.Increment(ref _writeAttempts);
        throw new IOException("диск недоступен");
    }
}

// Приёмник со счётчиком освобождения: проверяет, чьим приёмником владеет журнал.
internal sealed class TrackedDisposableSink : ILogSink, IDisposable
{
    public bool IsDisposed { get; private set; }

    public bool IsEnabled(LogLevel level) => true;

    public void Write(LogLevel level, string message)
    {
    }

    public void Dispose() => IsDisposed = true;
}

// Каталог теста удаляется вместе с тестом, чтобы файлы журнала не оставались в системном.
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "SkyW.Tests.Unit",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
            // Каталог уже удалён.
        }
        catch (IOException)
        {
            // Тест проверяет чтение файла, а не удаление мусора.
        }
    }
}