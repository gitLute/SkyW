namespace SkyW.Ecs;

// Низкоуровневое хранилище для команд и бэкенда. В публичный фасад IWorld
// не выносится: иначе у систем появился бы запрещённый Create.
public interface IEntityStore
{
    int Count { get; }
    EntityRef Create();
    void Delete(EntityRef entity);
    bool IsAlive(EntityRef entity);
    bool Has<T>(EntityRef entity) where T : struct, IComponent;
    ref T Get<T>(EntityRef entity) where T : struct, IComponent;
    bool TryGet<T>(EntityRef entity, out T value) where T : struct, IComponent;
    void Set<T>(EntityRef entity, in T value) where T : struct, IComponent;
    void Remove<T>(EntityRef entity) where T : struct, IComponent;
}
