namespace SkyW.Abstractions.Logging;

// Зовётся с потока симуляции и с потока рендера (docs/engine-architecture.md
// раздел 2.3), поэтому реализация обязана сериализовать запись.
public interface ILogSink
{
    bool IsEnabled(LogLevel level);
    // Приёмник вправе бросить любое исключение: сбой вывода не должен ронять кадр
    // (docs/engine-architecture.md раздел 8).
    void Write(LogLevel level, string message);
}
