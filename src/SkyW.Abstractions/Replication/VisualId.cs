namespace SkyW.Abstractions.Replication;

// Пара «индекс, генерация»: идентификаторы ECS в поток не попадают.
public readonly struct VisualId(int index, int generation) : IEquatable<VisualId>
{
    // Значение
    public static VisualId Invalid => new(-1, 0);
    public int Index { get; } = index;
    public int Generation { get; } = generation;
    public bool IsValid => Index >= 0;
    // Равенство
    public bool Equals(VisualId other) => Index == other.Index && Generation == other.Generation;
    public override bool Equals(object? obj) => obj is VisualId other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Index, Generation);
    public static bool operator ==(VisualId left, VisualId right) => left.Equals(right);
    public static bool operator !=(VisualId left, VisualId right) => !left.Equals(right);

    public override string ToString() => IsValid ? $"VisualId({Index}:{Generation})" : "VisualId.Invalid";
}
