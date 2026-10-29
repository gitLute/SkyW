using Xunit;

namespace SkyW.Tests.Architecture;

public sealed class LayerRulesTests
{
    private static readonly ProjectGraph Graph = ProjectGraph.Load();

    [Fact]
    public void Abstractions_HasNoProjectOrPackageReferences()
    {
        Assert.Empty(Graph.ReferencesOf("SkyW.Abstractions"));
        Assert.Empty(Graph.PackageReferencesOf("SkyW.Abstractions"));
    }

    [Fact]
    public void Ecs_ReferencesOnlyAbstractions() => Assert.Equal(["SkyW.Abstractions"], Graph.ReferencesOf("SkyW.Ecs"));

    [Fact]
    public void FrifloPackage_IsUsedOnlyByEcsAndBackend()
    {
        foreach (string project in Graph.Names)
        {
            bool usesFriflo = Graph.PackageReferencesOf(project).Contains("Friflo.Engine.ECS");
            bool allowed = project is "SkyW.Ecs" or "SkyW.Backends.Friflo";
            Assert.True(usesFriflo == allowed, $"{project}: ссылка на Friflo.Engine.ECS = {usesFriflo}, ожидалось {allowed}.");
        }
    }

    [Fact]
    public void Engine_ReferencesOnlyCoreProjects()
    {
        string[] references = Graph.ReferencesOf("SkyW.Engine");
        Assert.Contains("SkyW.Abstractions", references);
        Assert.Contains("SkyW.Backends.Friflo", references);
        Assert.Contains("SkyW.Diagnostics", references);
        Assert.Contains("SkyW.Ecs", references);
        Assert.Contains("SkyW.Mathematics", references);
        Assert.Contains("SkyW.Scenes", references);
        Assert.Contains("SkyW.Simulation", references);
        Assert.DoesNotContain("SkyW.Content", references);
        Assert.DoesNotContain("SkyW.Input", references);
        Assert.DoesNotContain("SkyW.Rendering", references);
        Assert.DoesNotContain("SkyW.Backends.Silk", references);
        Assert.DoesNotContain("SkyW.Backends.Jolt", references);
    }

    [Fact]
    public void Host_IsThinEntryPoint()
    {
        string[] references = Graph.ReferencesOf("SkyW.Host");
        Assert.Contains("SkyW.Engine", references);
        Assert.Contains("SkyW.Scenes", references);
        Assert.Equal(2, references.Length);
    }

    [Fact]
    public void Sandbox_DoesNotReferenceHost()
    {
        string[] references = Graph.ReferencesOf("SkyW.Sandbox");
        Assert.Contains("SkyW.Content", references);
        Assert.Contains("SkyW.Engine", references);
        Assert.Contains("SkyW.Scenes", references);
        Assert.DoesNotContain("SkyW.Host", references);
        Assert.Equal(3, references.Length);
    }

    [Fact]
    public void Simulation_DoesNotReferenceContentRenderingEngineOrBackends()
    {
        string[] references = Graph.ReferencesOf("SkyW.Simulation");
        Assert.Contains("SkyW.Ecs", references);
        Assert.DoesNotContain("SkyW.Content", references);
        Assert.DoesNotContain("SkyW.Engine", references);
        Assert.DoesNotContain("SkyW.Input", references);
        Assert.DoesNotContain("SkyW.Rendering", references);
        Assert.DoesNotContain("SkyW.Backends.Friflo", references);
        Assert.DoesNotContain("SkyW.Backends.Silk", references);
        Assert.DoesNotContain("SkyW.Backends.Jolt", references);
    }

    [Fact]
    public void Rendering_DoesNotReferenceSimulationOrEngine()
    {
        string[] references = Graph.ReferencesOf("SkyW.Rendering");
        Assert.DoesNotContain("SkyW.Engine", references);
        Assert.DoesNotContain("SkyW.Simulation", references);
    }

    [Fact]
    public void Friflo_DoesNotReferenceScenesSimulationOrEngine()
    {
        string[] references = Graph.ReferencesOf("SkyW.Backends.Friflo");
        Assert.Contains("SkyW.Ecs", references);
        Assert.DoesNotContain("SkyW.Engine", references);
        Assert.DoesNotContain("SkyW.Scenes", references);
        Assert.DoesNotContain("SkyW.Simulation", references);
    }

    [Fact]
    public void Scenes_DoesNotReferenceSimulationEngineOrBackends()
    {
        string[] references = Graph.ReferencesOf("SkyW.Scenes");
        Assert.Contains("SkyW.Ecs", references);
        Assert.DoesNotContain("SkyW.Engine", references);
        Assert.DoesNotContain("SkyW.Simulation", references);
        Assert.DoesNotContain("SkyW.Backends.Friflo", references);
        Assert.DoesNotContain("SkyW.Backends.Silk", references);
        Assert.DoesNotContain("SkyW.Backends.Jolt", references);
    }

    [Fact]
    public void Tools_DoNotReferenceBackends()
    {
        foreach (string project in Graph.Names.Where(name => name.StartsWith("SkyW.Tools.", StringComparison.Ordinal)))
        {
            string[] references = Graph.ReferencesOf(project);
            Assert.DoesNotContain("SkyW.Backends.Friflo", references);
            Assert.DoesNotContain("SkyW.Backends.Silk", references);
            Assert.DoesNotContain("SkyW.Backends.Jolt", references);
        }
    }

    [Fact]
    public void Backends_DoNotReferenceApplications()
    {
        foreach (string project in Graph.Names.Where(name => name.StartsWith("SkyW.Backends.", StringComparison.Ordinal)))
        {
            string[] references = Graph.ReferencesOf(project);
            Assert.DoesNotContain("SkyW.Engine", references);
            Assert.DoesNotContain("SkyW.Host", references);
            Assert.DoesNotContain("SkyW.Sandbox", references);
        }
    }
}
