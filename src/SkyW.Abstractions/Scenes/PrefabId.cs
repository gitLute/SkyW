namespace SkyW.Abstractions.Scenes;

// Номер определения префаба в реестре сцены: строка не ходит через горячий путь.
public readonly struct PrefabId(int value) : IEquatable<PrefabId>
{
    // Значение
    public static PrefabId Invalid => new(-1);
    public int Value { get; } = value;
    public bool IsValid => Value >= 0;
    // Равенство
    public bool Equals(PrefabId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is PrefabId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(PrefabId left, PrefabId right) => left.Equals(right);
    public static bool operator !=(PrefabId left, PrefabId right) => !left.Equals(right);

    public override string ToString() => IsValid ? $"PrefabId({Value})" : "PrefabId.Invalid";
}
