namespace SkyW.Ecs;

// Идентификатор сущности не раскрывает устройство бэкенда: смена ECS не должна
// менять контракт. Ноль зарезервирован под отсутствующую ссылку.
public readonly struct EntityRef(long value) : IEquatable<EntityRef>
{
    // Значение
    public static EntityRef Null => default;
    public long Value { get; } = value;
    public bool IsNull => Value == 0;
    
    // Равенство
    public bool Equals(EntityRef other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is EntityRef other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(EntityRef left, EntityRef right) => left.Equals(right);
    public static bool operator !=(EntityRef left, EntityRef right) => !left.Equals(right);

    public override string ToString() => IsNull ? "EntityRef.Null" : $"EntityRef({Value})";
}
