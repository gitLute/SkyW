namespace SkyW.Abstractions.Logging;

/// <summary>
/// Приёмник сообщений журнала: место, куда попадает готовая строка.
/// </summary>
/// <remarks>
/// Зовётся с потока симуляции и с потока рендера
/// (docs/engine-architecture.md раздел 2.3), поэтому реализация обязана
/// сериализовать запись.
/// </remarks>
public interface ILogSink
{
    /// <summary>
    /// Проверяет, примет ли приёмник сообщение уровня.
    /// </summary>
    /// <remarks>
    /// Проверка нужна, чтобы не собирать строку ради сообщения, которое всё
    /// равно отбросят.
    /// </remarks>
    bool IsEnabled(LogLevel level);

    /// <summary>
    /// Записывает сообщение.
    /// </summary>
    /// <remarks>
    /// Приёмник вправе бросить любое исключение: сбой вывода не должен ронять
    /// кадр (docs/engine-architecture.md раздел 8). Обратный вызов в журнал
    /// запрещён — запись уже идёт под его блокировкой.
    /// </remarks>
    void Write(LogLevel level, string message);
}