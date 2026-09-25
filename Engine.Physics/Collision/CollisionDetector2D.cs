using Engine.Core.Math;
using Engine.Physics.BroadPhase;

namespace Engine.Physics.Collision;

public sealed class CollisionDetector2D
{
    public bool TryDetect(
        PhysicsColliderProxy first,
        PhysicsColliderProxy second,
        out CollisionManifold manifold)
    {
        manifold = default;

        if (!first.Collider.Enabled ||
            !second.Collider.Enabled)
        {
            return false;
        }

        if (!first.Collider.CanCollideWith(
                second.Collider))
        {
            return false;
        }

        var firstBounds =
            first.Bounds;

        var secondBounds =
            second.Bounds;

        if (!firstBounds.Intersects(
                secondBounds))
        {
            return false;
        }

        var overlapX =
            Fixed32.Min(
                firstBounds.Max.X,
                secondBounds.Max.X) -
            Fixed32.Max(
                firstBounds.Min.X,
                secondBounds.Min.X);

        var overlapY =
            Fixed32.Min(
                firstBounds.Max.Y,
                secondBounds.Max.Y) -
            Fixed32.Max(
                firstBounds.Min.Y,
                secondBounds.Min.Y);

        if (overlapX < Fixed32.Zero ||
            overlapY < Fixed32.Zero)
        {
            return false;
        }

        var firstCenter =
            firstBounds.Center;

        var secondCenter =
            secondBounds.Center;

        var delta =
            secondCenter -
            firstCenter;

        FixedVector2 normal;
        Fixed32 penetration;

        if (overlapX < overlapY)
        {
            normal =
                delta.X < Fixed32.Zero
                    ? new FixedVector2(
                        Fixed32.FromInt(-1),
                        Fixed32.Zero)
                    : new FixedVector2(
                        Fixed32.One,
                        Fixed32.Zero);

            penetration =
                overlapX;
        }
        else
        {
            normal =
                delta.Y < Fixed32.Zero
                    ? new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.FromInt(-1))
                    : new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.One);

            penetration =
                overlapY;
        }

        var contactMin =
            new FixedVector2(
                Fixed32.Max(
                    firstBounds.Min.X,
                    secondBounds.Min.X),

                Fixed32.Max(
                    firstBounds.Min.Y,
                    secondBounds.Min.Y));

        var contactMax =
            new FixedVector2(
                Fixed32.Min(
                    firstBounds.Max.X,
                    secondBounds.Max.X),

                Fixed32.Min(
                    firstBounds.Max.Y,
                    secondBounds.Max.Y));

        var contactPoint =
            (contactMin + contactMax) *
            Fixed32.FromRatio(
                1,
                2);

        manifold =
            new CollisionManifold(
                new CollisionPair(
                    first.Entity,
                    second.Entity),

                new ContactPoint(
                    contactPoint,
                    normal,
                    penetration));

        return true;
    }
}