namespace SkyW.Abstractions.Scheduling;

// Стадия получает зависимости в конструкторе; Execute не ищет их в мире.
public interface IStage
{
    SimulationStage Stage { get; }

    void Execute(long tick);
}
