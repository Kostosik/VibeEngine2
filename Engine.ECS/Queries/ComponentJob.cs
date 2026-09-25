using Engine.ECS.Entities;

namespace Engine.ECS.Queries;

public delegate void ComponentJob<T>(
    EntityId entity,
    ref T component)
    where T : struct;