using SkyW.Scenes.Scripts;

namespace SkyW.Engine;

// Контракт композиции: Build собирает движок, здесь только жизненный цикл.
public interface IEngine : IDisposable
{
    void Start(ISceneScript scene);
    
    // Ручной кадр нужен тестам и однопоточному режиму.
    FrameResult Step();
    int Run();
    void Stop();
}
