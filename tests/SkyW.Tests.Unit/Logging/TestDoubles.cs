using SkyW.Abstractions.Logging;
using Xunit;

namespace SkyW.Tests.Unit.Logging;

/// <summary>
/// Тесты, перехватывающие <see cref="Console"/>, идут без параллелизации:
/// <c>Console.SetOut</c> меняет состояние процесса, и два таких теста сразу
/// перехватывают вывод друг друга.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConsoleCollection
{
    public const string Name = "console";
}

/// <summary>
/// Запись, принятая приёмником: уровень и готовый текст.
/// </summary>
/// <param name="Level">Уровень сообщения.</param>
/// <param name="Message">Текст сообщения без форматирования.</param>
internal sealed record LogEntry(LogLevel Level, string Message);

/// <summary>
/// Приёмник, копящий принятые сообщения: проверяет, что дошло и в каком
/// порядке.
/// </summary>
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

/// <summary>
/// Приёмник, который всегда падает: проверяет, что сбой вывода не роняет
/// кадр и не мешает остальным приёмникам.
/// </summary>
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

/// <summary>
/// Приёмник с явным счётчиком освобождения: проверяет, чьим приёмником владеет
/// журнал.
/// </summary>
internal sealed class TrackedDisposableSink : ILogSink, IDisposable
{
    public bool IsDisposed { get; private set; }

    public bool IsEnabled(LogLevel level) => true;

    public void Write(LogLevel level, string message)
    {
    }

    public void Dispose() => IsDisposed = true;
}

/// <summary>
/// Временный каталог теста: удаляется вместе с тестом, чтобы файлы журнала не
/// оставались в системном каталоге.
/// </summary>
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
            // Каталог уже удалён: ничего страшного.
        }
        catch (IOException)
        {
            // Тест проверяет чтение файла, а не удаление мусора.
        }
    }
}