namespace SkyW.Abstractions.Scheduling;

// Реальное время приходит из выходной полосы, шаг всегда фиксированный.
public interface ISimulationClock
{
    long Tick { get; }
    TimeSpan SimulationTime { get; }
    // Доля до следующего шага, клампится в 1: экстраполяции нет.
    float Alpha { get; }
    int StepsDue { get; }

    void Advance(TimeSpan realDelta);
    bool TryTakeStep();
    void Reset();
}
