using Engine.Core.Math;
using Engine.ECS.Entities;
using Engine.Physics.Components;

namespace Engine.Physics.BroadPhase;

public readonly record struct PhysicsColliderProxy(
    EntityId Entity,
    FixedBounds2 Bounds,
    Collider2D Collider);