using Engine.ECS.Entities;

namespace Engine.ECS.Queries;

public delegate void ReadOnlyComponentJob<T>(
    EntityId entity,
    in T component)
    where T : struct;