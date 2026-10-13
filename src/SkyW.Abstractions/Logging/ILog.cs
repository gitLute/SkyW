namespace SkyW.Abstractions.Logging;

/// <summary>
/// Журнал движка: единственный способ сообщить о том, что произошло.
/// </summary>
/// <remarks>
/// Контракт не зависит ни от одного приёмника: журнал не тащит ни консоль, ни
/// файловую систему в каждого потребителя (docs/engine-architecture.md раздел 1.2).
/// </remarks>
/// <remarks>
/// Поведение получает журнал в узком контексте
/// (docs/engine-architecture.md раздел 4.5), поэтому здесь нет ни порога
/// уровня, ни списка приёмников, ни освобождения — это забота создавшего.
/// </remarks>
public interface ILog
{
    /// <summary>
    /// Проверяет, попадёт ли сообщение уровня куда-нибудь.
    /// </summary>
    /// <remarks>
    /// Дорогой текст оборачивают этой проверкой: строка не собирается зря.
    /// </remarks>
    bool IsEnabled(LogLevel level);

    /// <summary>Записывает сообщение заданного уровня.</summary>
    void Log(LogLevel level, string message);

    /// <summary>
    /// Записывает сообщение заданного уровня с исключением.
    /// </summary>
    /// <param name="exception">
    /// Дописывается под сообщением со стеком: при разборе падения нужен стек,
    /// а не текст.
    /// </param>
    void Log(LogLevel level, string message, Exception exception);

    /// <summary>Записывает отладочное сообщение.</summary>
    void Debug(string message);

    /// <summary>Записывает отладочное сообщение с исключением.</summary>
    void Debug(string message, Exception exception);

    /// <summary>Записывает сообщение о ходе работы.</summary>
    void Information(string message);

    /// <summary>Записывает сообщение о ходе работы с исключением.</summary>
    void Information(string message, Exception exception);

    /// <summary>Записывает предупреждение.</summary>
    void Warning(string message);

    /// <summary>Записывает предупреждение с исключением.</summary>
    void Warning(string message, Exception exception);

    /// <summary>Записывает ошибку.</summary>
    void Error(string message);

    /// <summary>Записывает ошибку с исключением.</summary>
    void Error(string message, Exception exception);
}