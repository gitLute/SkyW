using SkyW.Abstractions.Replication;
using SkyW.Ecs;
using Xunit;

namespace SkyW.Tests.Unit.Ecs;

public sealed class EcsContractTests
{
    [Fact]
    public void SeamComponentMarker_InheritsFrifloMarker()
    {
        // Friflo накладывает своё ограничение на generic-операции: без наследования
        // шов не смог бы вызывать AddComponent<T> (docs/answers.md).
        string[] inherited = typeof(IComponent)
            .GetInterfaces()
            .Select(type => type.FullName!)
            .ToArray();
        Assert.Contains("Friflo.Engine.ECS.IComponent", inherited);
    }

    [Fact]
    public void SeamComponent_IsUsableThroughGenericConstraint()
    {
        Probe<Health> probe = new(new Health { Value = 42 });
        Assert.Equal(42, probe.Read());
    }

    [Fact]
    public void EntityRef_NullIsZeroAndDistinctFromAnyOther()
    {
        EntityRef created = new(7);
        Assert.True(EntityRef.Null.IsNull);
        Assert.False(created.IsNull);
        Assert.NotEqual(EntityRef.Null, created);
        Assert.Equal(created, new EntityRef(7));
        Assert.NotEqual(created, new EntityRef(8));
    }

    [Fact]
    public void ComponentId_InvalidIsNegative()
    {
        Assert.False(ComponentId.Invalid.IsValid);
        Assert.True(new ComponentId(0).IsValid);
    }

    [Fact]
    public void VisualId_ComparesIndexAndGeneration()
    {
        VisualId id = new(3, 2);
        Assert.True(id.IsValid);
        Assert.Equal(id, new VisualId(3, 2));
        Assert.NotEqual(id, new VisualId(3, 3));
        Assert.NotEqual(id, new VisualId(4, 2));
        Assert.False(VisualId.Invalid.IsValid);
    }

    [Fact]
    public void StateBatchHeader_KeepsKindTickAndSize()
    {
        StateBatchHeader header = new(version: 1, StateBatchKind.Keyframe, tick: 900, sequence: 12, payloadBytes: 4096);
        Assert.Equal(1, header.Version);
        Assert.Equal(StateBatchKind.Keyframe, header.Kind);
        Assert.Equal(900, header.Tick);
        Assert.Equal(12, header.Sequence);
        Assert.Equal(4096, header.PayloadBytes);
    }

    private struct Health : IComponent
    {
        public int Value;
    }

    // Проверка идёт через ограничение шва, а не через прямую ссылку на Friflo:
    // так тест фиксирует, что прикладной код видит только SkyW.Ecs.
    private readonly struct Probe<T>(T value)
        where T : struct, IComponent
    {
        public int Read() => value switch
        {
            Health health => health.Value,
            _ => throw new InvalidOperationException("Неизвестный компонент."),
        };
    }
}
