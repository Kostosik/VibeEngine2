using Engine.ECS.Entities;

namespace Engine.ECS.Queries;

public delegate void ReadOnlyComponentPairJob<T1, T2>(
    EntityId entity,
    in T1 first,
    in T2 second)
    where T1 : struct
    where T2 : struct;