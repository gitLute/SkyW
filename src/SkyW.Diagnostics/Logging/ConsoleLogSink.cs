using SkyW.Abstractions.Logging;

namespace SkyW.Diagnostics.Logging;

// Единственное место в подсистемах, где обращение к Console допустимо: остальной
// код пишет через журнал, иначе вывод нельзя перенаправить и собрать в тесте.
public sealed class ConsoleLogSink(
    LogLevel minimumLevel = LogLevel.Information,
    LogFormatter? formatter = null) : ILogSink
{
    private readonly LogLevel _minimumLevel = minimumLevel;
    private readonly LogFormatter _formatter = formatter ?? new LogFormatter();
    private readonly Lock _writeLock = new();
    public bool IsEnabled(LogLevel level) => level >= _minimumLevel;

    public void Write(LogLevel level, string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!IsEnabled(level)) return;
        // Запись сериализована: журнал зовут два потока, а Console не гарантирует.
        lock (_writeLock) Console.WriteLine(_formatter.Format(level, message));
    }
}
