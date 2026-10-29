namespace SkyW.Abstractions.Scheduling;

// Порядок объявления задаёт порядок стадий в шаге: Begin → ... → Summary.
public enum SimulationStage
{
    Begin,
    Structural,
    Logic,
    Physics,
    Synchronization,
    Publish,
    Summary,
}
