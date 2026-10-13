using SkyW.Abstractions.Logging;

namespace SkyW.Diagnostics.Logging;

// Журнал обязателен по контракту, а вывод не требуется: логирование ничего не стоит.
public sealed class NullLogSink : ILogSink
{
    public bool IsEnabled(LogLevel level) => false;

    public void Write(LogLevel level, string message)
    {
        // Намеренно пусто.
    }
}