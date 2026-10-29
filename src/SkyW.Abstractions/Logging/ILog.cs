namespace SkyW.Abstractions.Logging;

// Журнал не тащит ни консоль, ни файловую систему в каждого потребителя, а поведение
// получает его в узком контексте (docs/engine-architecture.md разделы 1.2 и 4.5).
public interface ILog
{
    bool IsEnabled(LogLevel level);
    void Log(LogLevel level, string message);
    // Исключение дописывается под сообщением со стеком: при разборе падения нужен
    // стек, а не текст.
    void Log(LogLevel level, string message, Exception exception);
    void Debug(string message);
    void Debug(string message, Exception exception);
    void Information(string message);
    void Information(string message, Exception exception);
    void Warning(string message);
    void Warning(string message, Exception exception);
    void Error(string message);
    void Error(string message, Exception exception);
}
