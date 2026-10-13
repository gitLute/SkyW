using SkyW.Abstractions.Logging;

namespace SkyW.Diagnostics.Logging;

/// <summary>
/// Приёмник сообщений, пишущий в консоль.
/// </summary>
/// <remarks>
/// Единственное место в подсистемах, где обращение к <see cref="Console"/>
/// допустимо: остальной код пишет через журнал, иначе вывод нельзя ни
/// перенаправить, ни собрать в тесте (docs/engine-architecture.md раздел 1.2).
/// </remarks>
public sealed class ConsoleLogSink : ILogSink
{
    private readonly LogLevel _minimumLevel;
    private readonly LogFormatter _formatter;
    private readonly Lock _writeLock = new();

    /// <summary>
    /// Создаёт приёмник с порогом уровня сообщений.
    /// </summary>
    /// <param name="minimumLevel">
    /// По умолчанию <see cref="LogLevel.Information"/>: отладочная выжимка без
    /// запроса не нужна.
    /// </param>
    public ConsoleLogSink(
        LogLevel minimumLevel = LogLevel.Information,
        LogFormatter? formatter = null)
    {
        _minimumLevel = minimumLevel;
        _formatter = formatter ?? new LogFormatter();
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

        lock (_writeLock)
        {
            // Запись сериализована: журнал зовут два потока, а Console этого не
            // гарантирует.
            Console.WriteLine(_formatter.Format(level, message));
        }
    }
}