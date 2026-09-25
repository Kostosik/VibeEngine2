using Engine.ECS.Entities;

namespace Engine.ECS.Queries;

public delegate void ComponentPairJob<T1, T2>(
    EntityId entity,
    ref T1 first,
    ref T2 second)
    where T1 : struct
    where T2 : struct;