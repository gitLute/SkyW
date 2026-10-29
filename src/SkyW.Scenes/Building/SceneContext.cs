using SkyW.Abstractions.Scenes;

namespace SkyW.Scenes.Building;

// Builder-контекст видит только свои узлы и не получает доступа к миру.
public sealed class SceneContext(IPrefabManager prefabs)
{
    public IPrefabManager Prefabs { get; } = prefabs;
}
