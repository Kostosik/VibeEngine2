using Engine.Physics.Collision;

namespace Engine.Physics.BroadPhase;

public interface IPhysicsBroadPhase
{
    void FindPairs(
        IReadOnlyList<PhysicsColliderProxy> colliders,
        List<CollisionPair> pairs);
}