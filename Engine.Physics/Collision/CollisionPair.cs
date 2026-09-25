using Engine.ECS.Entities;

namespace Engine.Physics.Collision;

public readonly record struct CollisionPair(
    EntityId First,
    EntityId Second);