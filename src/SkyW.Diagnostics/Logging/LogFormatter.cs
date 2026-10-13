using System.Globalization;
using SkyW.Abstractions.Logging;

namespace SkyW.Diagnostics.Logging;

/// <summary>
/// Собирает строку журнала в формате, принятом в движке.
/// </summary>
/// <remarks>
/// Формат: <c>[время] [поток / уровень]: сообщение</c>. Правило живёт здесь,
/// а не в приёмниках, чтобы консоль, файл и буфер давали одну и ту же строку.
/// </remarks>
public sealed class LogFormatter
{
    /// <summary>
    /// Формат отметки времени по умолчанию: местное время с миллисекундами,
    /// сортируемое как текст.
    /// </summary>
    public const string DefaultTimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";

    private readonly string _timestampFormat;
    private readonly Func<DateTimeOffset> _clock;

    /// <summary>
    /// Создаёт форматтер строк журнала.
    /// </summary>
    /// <param name="clock">
    /// Источник времени: внедрение вместо статического поля даёт подставное
    /// время из требования тестируемости (docs/engine-architecture.md раздел 9).
    /// </param>
    /// <exception cref="ArgumentException">Формат отметки времени пуст.</exception>
    public LogFormatter(
        string timestampFormat = DefaultTimestampFormat,
        Func<DateTimeOffset>? clock = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(timestampFormat);

        _timestampFormat = timestampFormat;
        _clock = clock ?? (static () => DateTimeOffset.Now);
    }

    /// <summary>Формат отметки времени, заданный при создании.</summary>
    public string TimestampFormat => _timestampFormat;

    /// <summary>Собирает готовую строку журнала.</summary>
    /// <exception cref="ArgumentNullException">Текст сообщения не задан.</exception>
    public string Format(LogLevel level, string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return $"[{Timestamp()}] [{ThreadName()} / {LevelName(level)}]: {message}";
    }

    /// <summary>Отметка времени текущего момента по заданному формату.</summary>
    /// <remarks>
    /// Инвариантная культура: журнал читается одинаково при любой локали машины.
    /// </remarks>
    public string Timestamp() =>
        _clock().ToString(_timestampFormat, CultureInfo.InvariantCulture);

    /// <summary>Имя вызывающего потока.</summary>
    /// <remarks>
    /// Без <see cref="Thread.Name"/> в строке остаётся только
    /// <c>#&lt;ManagedThreadId&gt;</c>: потоки симуляции и рендера неразличимы.
    /// </remarks>
    public string ThreadName()
    {
        Thread thread = Thread.CurrentThread;

        return thread.Name ?? $"#{thread.ManagedThreadId}";
    }

    /// <summary>Короткое имя уровня для строки журнала.</summary>
    public string LevelName(LogLevel level) => level switch
    {
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        _ => "???",
    };
}