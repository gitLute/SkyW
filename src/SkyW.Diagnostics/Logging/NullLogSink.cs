using SkyW.Abstractions.Logging;

namespace SkyW.Diagnostics.Logging;

/// <summary>
/// Заглушка приёмника: сообщения отбрасываются, логирование ничего не стоит.
/// </summary>
/// <remarks>
/// Нужна там, где журнал обязателен по контракту, а вывод не требуется: тесты,
/// инструменты контента, однопоточная диагностика. «Ничего не стоит» означает,
/// что отключение лога не требует ветвлений в коде.
/// </remarks>
public sealed class NullLogSink : ILogSink
{
    /// <inheritdoc/>
    /// <remarks>
    /// Всегда <c>false</c>: вызывающий код не тратит время на сбор строк.
    /// </remarks>
    public bool IsEnabled(LogLevel level) => false;

    /// <inheritdoc/>
    public void Write(LogLevel level, string message)
    {
        // Намеренно пусто: заглушка не пишет никуда.
    }
}