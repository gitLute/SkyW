using SkyW.Abstractions.Logging;
using SkyW.Abstractions.Scheduling;

namespace SkyW.Engine;

// Конфигурация Build: значения по умолчанию совпадают с docs/answers.md.
public sealed class EngineOptions
{
    // Частота и шаги
    public int SimulationRateHz { get; set; } = 60;
    public int MaxStepsPerFrame { get; set; } = 8;
    public double DebtLimitSeconds { get; set; } = 2;
    // Потоки и каналы
    public ThreadingMode Threading { get; set; } = ThreadingMode.Multi;
    public int BatchBytes { get; set; } = 256 * 1024;
    public int KeyframeBytes { get; set; } = 4 * 1024 * 1024;
    // Журнал
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
}
