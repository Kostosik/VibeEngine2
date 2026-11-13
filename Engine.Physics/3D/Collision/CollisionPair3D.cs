using Engine.ECS.Entities;

namespace Engine.Physics.ThreeD.Collision;

public readonly record struct CollisionPair3D(
    EntityId First,
    EntityId Second);