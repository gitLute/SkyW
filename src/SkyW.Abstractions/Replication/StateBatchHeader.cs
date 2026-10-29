namespace SkyW.Abstractions.Replication;

// Заголовок непрозрачного батча: раскладку секций внутри payload знает
// только Simulation/Replication, поэтому Engine не зависит от формата SoA.
public readonly struct StateBatchHeader(int version, StateBatchKind kind, long tick, long sequence, int payloadBytes)
{
    public int Version { get; } = version;
    public StateBatchKind Kind { get; } = kind;
    public long Tick { get; } = tick;
    public long Sequence { get; } = sequence;
    public int PayloadBytes { get; } = payloadBytes;
}
