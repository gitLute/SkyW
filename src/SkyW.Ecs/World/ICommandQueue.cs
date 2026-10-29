using SkyW.Abstractions.Scenes;

namespace SkyW.Ecs;

// Структурные команды применяются в S1 следующего шага, в порядке отправки.
public interface ICommandQueue
{
    void Spawn(PrefabId prefab, EntityRef parent);
    void Despawn(EntityRef entity);
    void Set<T>(EntityRef entity, in T value) where T : struct, IComponent;
    void Remove<T>(EntityRef entity) where T : struct, IComponent;
}
