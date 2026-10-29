namespace SkyW.Ecs;

// Делегаты запросов создаются один раз при регистрации системы, а не в шаге:
// иначе каждый обход выделял бы замыкание.
public delegate void EntityAction<T1>(EntityRef entity, ref T1 component)
    where T1 : struct, IComponent;

public delegate void EntityAction<T1, T2>(EntityRef entity, ref T1 component1, ref T2 component2)
    where T1 : struct, IComponent
    where T2 : struct, IComponent;

public delegate void EntityAction<T1, T2, T3>(EntityRef entity, ref T1 component1, ref T2 component2, ref T3 component3)
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent;
