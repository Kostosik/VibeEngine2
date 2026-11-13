using Engine.Core.Math;
using Engine.ECS.Entities;

namespace Engine.Physics.Queries;

public readonly record struct PhysicsRaycastHit2D(
    EntityId Entity,
    FixedVector2 Point,
    FixedVector2 Normal,
    Fixed32 Distance);