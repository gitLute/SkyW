using SkyW.Scenes.Building;

namespace SkyW.Scenes.Scripts;

// Генератор сцены и определение префаба — скрипты одного типа; пока это C#-шов.
public interface ISceneScript
{
    void Build(SceneContext context);
}
