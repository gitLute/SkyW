namespace SkyW.Ecs;

// Публичный фасад мира: системы читают и меняют значения, структуру меняют
// только через очередь команд. Create сюда сознательно не входит.
public interface IWorld
{
    ICommandQueue Commands { get; }

    IQuery<T1> Query<T1>() where T1 : struct, IComponent;
    IQuery<T1, T2> Query<T1, T2>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent;
    IQuery<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent;

    bool Has<T>(EntityRef entity) where T : struct, IComponent;
    ref T Get<T>(EntityRef entity) where T : struct, IComponent;
    bool TryGet<T>(EntityRef entity, out T value) where T : struct, IComponent;
    // Set добавляет компонент или заменяет значение; вне обхода применяется сразу.
    void Set<T>(EntityRef entity, in T value) where T : struct, IComponent;
    void Remove<T>(EntityRef entity) where T : struct, IComponent;
    // Изменение значения не наблюдается: пометка — отдельная операция.
    void MarkChanged<T>(EntityRef entity) where T : struct, IComponent;
}
