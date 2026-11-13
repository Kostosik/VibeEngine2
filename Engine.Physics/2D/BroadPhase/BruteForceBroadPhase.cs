using Engine.Physics.Collision;

namespace Engine.Physics.BroadPhase;

public sealed class BruteForceBroadPhase :
    IPhysicsBroadPhase
{
    public void FindPairs(
        IReadOnlyList<PhysicsColliderProxy> colliders,
        List<CollisionPair> pairs)
    {
        ArgumentNullException.ThrowIfNull(colliders);
        ArgumentNullException.ThrowIfNull(pairs);

        pairs.Clear();

        for (var i = 0; i < colliders.Count - 1; i++)
        {
            var first =
                colliders[i];

            if (!first.Collider.Enabled)
                continue;

            for (var j = i + 1; j < colliders.Count; j++)
            {
                var second =
                    colliders[j];

                if (!second.Collider.Enabled)
                    continue;

                if (!first.Collider.CanCollideWith(
                        second.Collider))
                {
                    continue;
                }

                if (!first.Bounds.Intersects(
                        second.Bounds))
                {
                    continue;
                }

                pairs.Add(
                    new CollisionPair(
                        first.Entity,
                        second.Entity));
            }
        }
    }
}