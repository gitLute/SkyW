using System.Globalization;
using SkyW.Abstractions.Logging;

namespace SkyW.Diagnostics.Logging;

// Строка журнала: [время] [поток / уровень]: сообщение. Правило живёт здесь,
// а не в приёмниках, чтобы все выводы давали одну и ту же строку.
public sealed class LogFormatter
{
    // Местное время с миллисекундами: сортируется как текст.
    public const string DefaultTimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";

    private readonly string _timestampFormat;
    private readonly Func<DateTimeOffset> _clock;

    // Источник времени внедряется, а не берётся из статического поля: это подставное
    // время из требования тестируемости (docs/engine-architecture.md раздел 9).
    public LogFormatter(
        string timestampFormat = DefaultTimestampFormat,
        Func<DateTimeOffset>? clock = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(timestampFormat);

        _timestampFormat = timestampFormat;
        _clock = clock ?? (static () => DateTimeOffset.Now);
    }

    public string TimestampFormat => _timestampFormat;

    public string Format(LogLevel level, string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return $"[{Timestamp()}] [{ThreadName()} / {LevelName(level)}]: {message}";
    }

    // Инвариантная культура: журнал читается одинаково при любой локали машины.
    public string Timestamp() =>
        _clock().ToString(_timestampFormat, CultureInfo.InvariantCulture);

    // Без Thread.Name в строке остаётся только #<ManagedThreadId>: потоки симуляции
    // и рендера неразличимы.
    public string ThreadName()
    {
        Thread thread = Thread.CurrentThread;

        return thread.Name ?? $"#{thread.ManagedThreadId}";
    }

    public string LevelName(LogLevel level) => level switch
    {
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        _ => "???",
    };
}