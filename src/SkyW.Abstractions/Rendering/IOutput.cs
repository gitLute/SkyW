using SkyW.Abstractions.Replication;

namespace SkyW.Abstractions.Rendering;

// Потребитель вывода владеет зеркалом и сам вычитывает поток состояния.
public interface IOutput
{
    void Present(IStateStream stream, float alpha);
}
