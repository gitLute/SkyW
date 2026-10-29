namespace SkyW.Abstractions.Replication;

// Keyframe очищает зеркало и заполняет его заново; дельта применяется поверх.
public enum StateBatchKind
{
    Delta,
    Keyframe,
}
