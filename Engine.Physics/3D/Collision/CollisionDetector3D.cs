using Engine.Core.Math;
using Engine.Physics._3D.Collision;
using Engine.Physics.ThreeD.BroadPhase;
using Engine.Physics.ThreeD.Shapes;

namespace Engine.Physics.ThreeD.Collision;

public sealed class CollisionDetector3D
{
    public bool TryDetect(
        PhysicsColliderProxy3D first,
        PhysicsColliderProxy3D second,
        out CollisionManifold3D manifold)
    {
        manifold = default;

        if (!first.Collider.Enabled ||
            !second.Collider.Enabled)
        {
            return false;
        }

        if (!first.Collider.CanCollideWith(second.Collider))
            return false;

        if (!first.Bounds.Intersects(second.Bounds))
            return false;

        var firstType = first.Collider.Shape.Type;
        var secondType = second.Collider.Shape.Type;

        return (firstType, secondType) switch
        {
            (PhysicsShapeType3D.Sphere, PhysicsShapeType3D.Sphere) =>
                TryDetectSphereSphere(first, second, out manifold),

            (PhysicsShapeType3D.Sphere, PhysicsShapeType3D.Box) =>
                TryDetectSphereBox(first, second, out manifold),

            (PhysicsShapeType3D.Box, PhysicsShapeType3D.Sphere) =>
                TryDetectReverse(
                    second,
                    first,
                    TryDetectSphereBox,
                    out manifold),

            (PhysicsShapeType3D.Sphere, PhysicsShapeType3D.Plane) =>
                TryDetectSpherePlane(first, second, out manifold),

            (PhysicsShapeType3D.Plane, PhysicsShapeType3D.Sphere) =>
                TryDetectSpherePlaneReverse(first, second, out manifold),

            (PhysicsShapeType3D.Box, PhysicsShapeType3D.Plane) =>
                TryDetectBoxPlane(first, second, out manifold),

            (PhysicsShapeType3D.Plane, PhysicsShapeType3D.Box) =>
                TryDetectBoxPlaneReverse(first, second, out manifold),

            (PhysicsShapeType3D.Capsule, PhysicsShapeType3D.Plane) =>
                TryDetectCapsulePlane(first, second, out manifold),

            (PhysicsShapeType3D.Plane, PhysicsShapeType3D.Capsule) =>
                TryDetectCapsulePlaneReverse(first, second, out manifold),

            _ => false
        };
    }

    private static bool TryDetectReverse(
        PhysicsColliderProxy3D first,
        PhysicsColliderProxy3D second,
        Func<PhysicsColliderProxy3D, PhysicsColliderProxy3D,
            out CollisionManifold3D, bool> detector,
        out CollisionManifold3D manifold)
    {
        if (!detector(second, first, out var reverse))
        {
            manifold = default;
            return false;
        }

        manifold = Invert(reverse, first.Entity, second.Entity);
        return true;
    }

    private static CollisionManifold3D Invert(
        CollisionManifold3D manifold,
        Engine.ECS.Entities.EntityId first,
        Engine.ECS.Entities.EntityId second)
    {
        return new CollisionManifold3D(
            new CollisionPair3D(first, second),
            new ContactPoint3D(
                manifold.Contact.Position,
                -manifold.Contact.Normal,
                manifold.Contact.Penetration));
    }

    private static bool TryDetectSphereSphere(
        PhysicsColliderProxy3D first,
        PhysicsColliderProxy3D second,
        out CollisionManifold3D manifold)
    {
        manifold = default;

        var delta = second.WorldPosition - first.WorldPosition;

        var distanceSquared = delta.LengthSquared();

        var radiusSum = first.SphereRadius + second.SphereRadius;

        if (distanceSquared > radiusSum * radiusSum)
            return false;

        Fixed32 distance;
        FixedVector3 normal;

        if (distanceSquared == Fixed32.Zero)
        {
            distance = Fixed32.Zero;
            normal = FixedVector3.UnitX;
        }
        else
        {
            distance = Fixed32.Sqrt(distanceSquared);
            normal = delta / distance;
        }

        var firstSurface =
            first.WorldPosition + normal * first.SphereRadius;

        var secondSurface =
            second.WorldPosition - normal * second.SphereRadius;

        var contact = (firstSurface + secondSurface) *
                      Fixed32.FromRatio(1, 2);

        manifold = CreateManifold(
            first,
            second,
            contact,
            normal,
            radiusSum - distance);

        return true;
    }

    private static bool TryDetectSphereBox(
        PhysicsColliderProxy3D sphere,
        PhysicsColliderProxy3D box,
        out CollisionManifold3D manifold)
    {
        manifold = default;

        var inverseRotation = box.WorldRotation.Inverse();

        var localCenter = inverseRotation.Transform(
            sphere.WorldPosition - box.WorldPosition);

        var extents = box.BoxHalfExtents;

        var closest = new FixedVector3(
            Fixed32.Clamp(localCenter.X, -extents.X, extents.X),
            Fixed32.Clamp(localCenter.Y, -extents.Y, extents.Y),
            Fixed32.Clamp(localCenter.Z, -extents.Z, extents.Z));

        var delta = closest - localCenter;
        var distanceSquared = delta.LengthSquared();
        var radius = sphere.SphereRadius;

        if (distanceSquared > radius * radius)
            return false;

        FixedVector3 normalLocal;
        Fixed32 penetration;

        if (distanceSquared > Fixed32.Zero)
        {
            var distance = Fixed32.Sqrt(distanceSquared);
            normalLocal = delta / distance;
            penetration = radius - distance;
        }
        else
        {
            var left = localCenter.X + extents.X;
            var right = extents.X - localCenter.X;
            var bottom = localCenter.Y + extents.Y;
            var top = extents.Y - localCenter.Y;
            var back = localCenter.Z + extents.Z;
            var front = extents.Z - localCenter.Z;

            var minimum = Fixed32.Min(
                Fixed32.Min(Fixed32.Min(left, right),
                    Fixed32.Min(bottom, top)),
                Fixed32.Min(back, front));

            if (minimum == left)
            {
                normalLocal = FixedVector3.UnitX;
                closest = new FixedVector3(-extents.X, localCenter.Y, localCenter.Z);
            }
            else if (minimum == right)
            {
                normalLocal = -FixedVector3.UnitX;
                closest = new FixedVector3(extents.X, localCenter.Y, localCenter.Z);
            }
            else if (minimum == bottom)
            {
                normalLocal = FixedVector3.UnitY;
                closest = new FixedVector3(localCenter.X, -extents.Y, localCenter.Z);
            }
            else if (minimum == top)
            {
                normalLocal = -FixedVector3.UnitY;
                closest = new FixedVector3(localCenter.X, extents.Y, localCenter.Z);
            }
            else if (minimum == back)
            {
                normalLocal = FixedVector3.UnitZ;
                closest = new FixedVector3(localCenter.X, localCenter.Y, -extents.Z);
            }
            else
            {
                normalLocal = -FixedVector3.UnitZ;
                closest = new FixedVector3(localCenter.X, localCenter.Y, extents.Z);
            }

            penetration = radius + minimum;
        }

        var normal = box.WorldRotation.Transform(normalLocal).Normalize();

        var contact = box.WorldPosition +
                      box.WorldRotation.Transform(closest);

        manifold = CreateManifold(
            sphere,
            box,
            contact,
            normal,
            penetration);

        return true;
    }

    private static bool TryDetectSpherePlane(
        PhysicsColliderProxy3D sphere,
        PhysicsColliderProxy3D plane,
        out CollisionManifold3D manifold)
    {
        manifold = default;

        var (planeNormal, planeDistance) = plane.GetWorldPlane();

        var signedDistance =
            planeNormal.Dot(sphere.WorldPosition) - planeDistance;

        var absoluteDistance = Fixed32.Abs(signedDistance);
        var radius = sphere.SphereRadius;

        if (absoluteDistance > radius)
            return false;

        var normal = signedDistance >= Fixed32.Zero
            ? -planeNormal
            : planeNormal;

        var contact = sphere.WorldPosition -
                      planeNormal * signedDistance;

        manifold = CreateManifold(
            sphere,
            plane,
            contact,
            normal,
            radius - absoluteDistance);

        return true;
    }

    private static bool TryDetectSpherePlaneReverse(
        PhysicsColliderProxy3D plane,
        PhysicsColliderProxy3D sphere,
        out CollisionManifold3D manifold)
    {
        if (!TryDetectSpherePlane(
                sphere,
                plane,
                out var reverse))
        {
            manifold = default;
            return false;
        }

        manifold = Invert(reverse, plane.Entity, sphere.Entity);
        return true;
    }

    private static bool TryDetectBoxPlane(
        PhysicsColliderProxy3D box,
        PhysicsColliderProxy3D plane,
        out CollisionManifold3D manifold)
    {
        manifold = default;

        var (planeNormal, planeDistance) = plane.GetWorldPlane();

        var extents = box.BoxHalfExtents;

        var axisX = box.WorldRotation.Transform(FixedVector3.UnitX);
        var axisY = box.WorldRotation.Transform(FixedVector3.UnitY);
        var axisZ = box.WorldRotation.Transform(FixedVector3.UnitZ);

        var projectedRadius =
            Fixed32.Abs(planeNormal.Dot(axisX)) * extents.X +
            Fixed32.Abs(planeNormal.Dot(axisY)) * extents.Y +
            Fixed32.Abs(planeNormal.Dot(axisZ)) * extents.Z;

        var signedDistance =
            planeNormal.Dot(box.WorldPosition) - planeDistance;

        var absoluteDistance = Fixed32.Abs(signedDistance);

        if (absoluteDistance > projectedRadius)
            return false;

        var normal = signedDistance >= Fixed32.Zero
            ? -planeNormal
            : planeNormal;

        var supportSign = signedDistance >= Fixed32.Zero
            ? Fixed32.One
            : -Fixed32.One;

        var boxSurface = box.WorldPosition -
                         planeNormal * (projectedRadius * supportSign);

        var planeSurface = boxSurface -
                           planeNormal *
                           (planeNormal.Dot(boxSurface) - planeDistance);

        var contact = (boxSurface + planeSurface) *
                      Fixed32.FromRatio(1, 2);

        manifold = CreateManifold(
            box,
            plane,
            contact,
            normal,
            projectedRadius - absoluteDistance);

        return true;
    }

    private static bool TryDetectBoxPlaneReverse(
        PhysicsColliderProxy3D plane,
        PhysicsColliderProxy3D box,
        out CollisionManifold3D manifold)
    {
        if (!TryDetectBoxPlane(
                box,
                plane,
                out var reverse))
        {
            manifold = default;
            return false;
        }

        manifold = Invert(reverse, plane.Entity, box.Entity);
        return true;
    }

    private static bool TryDetectCapsulePlane(
        PhysicsColliderProxy3D capsule,
        PhysicsColliderProxy3D plane,
        out CollisionManifold3D manifold)
    {
        manifold = default;

        var (planeNormal, planeDistance) = plane.GetWorldPlane();

        var segment = capsule.CapsuleAxis *
                      capsule.CapsuleHalfHeight;

        var firstPoint = capsule.WorldPosition - segment;
        var secondPoint = capsule.WorldPosition + segment;

        var firstDistance =
            planeNormal.Dot(firstPoint) - planeDistance;

        var secondDistance =
            planeNormal.Dot(secondPoint) - planeDistance;

        FixedVector3 closestPoint;
        Fixed32 signedDistance;

        if ((firstDistance < Fixed32.Zero &&
             secondDistance > Fixed32.Zero) ||
            (firstDistance > Fixed32.Zero &&
             secondDistance < Fixed32.Zero))
        {
            var amount = firstDistance /
                         (firstDistance - secondDistance);

            closestPoint = firstPoint +
                           (secondPoint - firstPoint) * amount;

            signedDistance = Fixed32.Zero;
        }
        else if (Fixed32.Abs(firstDistance) <=
                 Fixed32.Abs(secondDistance))
        {
            closestPoint = firstPoint;
            signedDistance = firstDistance;
        }
        else
        {
            closestPoint = secondPoint;
            signedDistance = secondDistance;
        }

        var absoluteDistance = Fixed32.Abs(signedDistance);
        var radius = capsule.CapsuleRadius;

        if (absoluteDistance > radius)
            return false;

        var normal = signedDistance >= Fixed32.Zero
            ? -planeNormal
            : planeNormal;

        var planePoint = closestPoint -
                         planeNormal * signedDistance;

        var capsuleSurface = closestPoint + normal * radius;

        var contact = (planePoint + capsuleSurface) *
                      Fixed32.FromRatio(1, 2);

        manifold = CreateManifold(
            capsule,
            plane,
            contact,
            normal,
            radius - absoluteDistance);

        return true;
    }

    private static bool TryDetectCapsulePlaneReverse(
        PhysicsColliderProxy3D plane,
        PhysicsColliderProxy3D capsule,
        out CollisionManifold3D manifold)
    {
        if (!TryDetectCapsulePlane(
                capsule,
                plane,
                out var reverse))
        {
            manifold = default;
            return false;
        }

        manifold = Invert(reverse, plane.Entity, capsule.Entity);
        return true;
    }

    private delegate bool PairDetector(
    PhysicsColliderProxy3D first,
    PhysicsColliderProxy3D second,
    out CollisionManifold3D manifold);

    private static CollisionManifold3D CreateManifold(
        PhysicsColliderProxy3D first,
        PhysicsColliderProxy3D second,
        FixedVector3 position,
        FixedVector3 normal,
        Fixed32 penetration)
    {
        return new CollisionManifold3D(
            new CollisionPair3D(first.Entity, second.Entity),
            new ContactPoint3D(
                position,
                normal.Normalize(),
                Fixed32.Max(Fixed32.Zero, penetration)));
    }
}