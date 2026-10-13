using SkyW.Abstractions.Logging;

namespace SkyW.Diagnostics.Logging;

// Единственное место в подсистемах, где обращение к Console допустимо: остальной
// код пишет через журнал, иначе вывод нельзя перенаправить и собрать в тесте.
public sealed class ConsoleLogSink : ILogSink
{
    private readonly LogLevel _minimumLevel;
    private readonly LogFormatter _formatter;
    private readonly Lock _writeLock = new();

    public ConsoleLogSink(
        LogLevel minimumLevel = LogLevel.Information,
        LogFormatter? formatter = null)
    {
        _minimumLevel = minimumLevel;
        _formatter = formatter ?? new LogFormatter();
    }

    public bool IsEnabled(LogLevel level) => level >= _minimumLevel;

    public void Write(LogLevel level, string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!IsEnabled(level))
        {
            return;
        }

        lock (_writeLock)
        {
            // Запись сериализована: журнал зовут два потока, а Console не гарантирует.
            Console.WriteLine(_formatter.Format(level, message));
        }
    }
}