namespace SkyW.Ecs;

// Маркер компонента наследует Friflo, чтобы типизированный шов мог вызывать
// AddComponent<T> без импорта внешней библиотеки в прикладном коде.
public interface IComponent : Friflo.Engine.ECS.IComponent { }
