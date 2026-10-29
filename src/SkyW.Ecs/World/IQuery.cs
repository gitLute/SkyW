namespace SkyW.Ecs;

// Запрос создаётся в Build или Start и переиспользуется: создание в шаге
// выделяло бы память и нарушало бюджет кадра.
public interface IQuery<T1>
    where T1 : struct, IComponent
{
    int Count { get; }
    void ForEach(EntityAction<T1> action);
}

public interface IQuery<T1, T2>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
{
    int Count { get; }
    void ForEach(EntityAction<T1, T2> action);
}

public interface IQuery<T1, T2, T3>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
{
    int Count { get; }
    void ForEach(EntityAction<T1, T2, T3> action);
}
