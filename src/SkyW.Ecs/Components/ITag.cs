namespace SkyW.Ecs;

// Тег не хранит данных: он нужен запросам и фильтрам, а не репликации.
public interface ITag : Friflo.Engine.ECS.ITag { }
