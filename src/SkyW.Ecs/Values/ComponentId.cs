namespace SkyW.Ecs;

// Номер типа в реестре компонентов. Отрицательное значение означает «не зарегистрирован».
public readonly struct ComponentId(int value) : IEquatable<ComponentId>
{
    // Значение
    public static ComponentId Invalid => new(-1);
    public int Value { get; } = value;
    public bool IsValid => Value >= 0;
    
    // Равенство
    public bool Equals(ComponentId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is ComponentId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(ComponentId left, ComponentId right) => left.Equals(right);
    public static bool operator !=(ComponentId left, ComponentId right) => !left.Equals(right);

    public override string ToString() => IsValid ? $"ComponentId({Value})" : "ComponentId.Invalid";
}
