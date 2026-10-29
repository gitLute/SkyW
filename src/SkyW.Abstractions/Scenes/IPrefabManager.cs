namespace SkyW.Abstractions.Scenes;

// Менеджер префабов отделён от генератора сцены: у них разные права и место жизни.
public interface IPrefabManager
{
    bool TryGet(PrefabId id, out IPrefab prefab);
}
