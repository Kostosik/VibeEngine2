using Engine.Core.Events;

namespace Engine.Physics.Collision;

public enum PhysicsContactType
{
    Collision = 0,
    Trigger = 1
}

public enum PhysicsContactPhase
{
    Enter = 0,
    Stay = 1,
    Exit = 2
}

public readonly record struct PhysicsContactEvent(
    PhysicsContactType Type,
    PhysicsContactPhase Phase,
    CollisionManifold Manifold) : IEvent;