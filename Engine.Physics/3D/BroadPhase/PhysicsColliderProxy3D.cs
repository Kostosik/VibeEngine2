using Engine.Core.Math;
using Engine.ECS.Entities;
using Engine.Physics.ThreeD.Components;
using Engine.Physics.ThreeD.Shapes;
using Engine.Transform;

namespace Engine.Physics.ThreeD.BroadPhase;

public readonly struct PhysicsColliderProxy3D
{
    public PhysicsColliderProxy3D(
        EntityId entity,
        Collider3D collider,
        FixedTransform3D transform)
    {
        if (!entity.IsValid)
            throw new ArgumentException("Entity must be valid.", nameof(entity));

        collider.Validate();

        Entity = entity;
        Collider = collider;

        WorldPosition = collider.GetWorldPosition(transform);
        WorldRotation = collider.GetWorldRotation(transform);
        WorldScale = transform.Scale;

        Bounds = collider.GetWorldBounds(transform);
    }

    public EntityId Entity { get; }

    public Collider3D Collider { get; }

    public PhysicsBounds3 Bounds { get; }

    public FixedVector3 WorldPosition { get; }

    public FixedQuaternion WorldRotation { get; }

    public FixedVector3 WorldScale { get; }

    public Fixed32 SphereRadius =>
        Collider.Shape.Sphere.Radius *
        MaximumScale(WorldScale);

    public FixedVector3 BoxHalfExtents =>
        Multiply(
            Collider.Shape.Box.HalfExtents,
            Abs(WorldScale));

    public Fixed32 CapsuleRadius =>
        Collider.Shape.Capsule.Radius *
        MaximumScale(WorldScale);

    public Fixed32 CapsuleHalfHeight =>
        Collider.Shape.Capsule.HalfHeight *
        Fixed32.Abs(WorldScale.Y);

    public FixedVector3 CapsuleAxis =>
        WorldRotation.Transform(FixedVector3.UnitY).Normalize();

    public (FixedVector3 Normal, Fixed32 Distance) GetWorldPlane()
    {
        var plane = Collider.Shape.Plane;

        var inverseScale = new FixedVector3(
            Fixed32.One / WorldScale.X,
            Fixed32.One / WorldScale.Y,
            Fixed32.One / WorldScale.Z);

        var normal = WorldRotation.Transform(
            Multiply(plane.Normal, inverseScale)).Normalize();

        var localPoint = plane.Normal * plane.Distance;

        var worldPoint = WorldPosition +
            WorldRotation.Transform(
                Multiply(localPoint, WorldScale));

        return (normal, normal.Dot(worldPoint));
    }

    private static Fixed32 MaximumScale(FixedVector3 scale)
    {
        return Fixed32.Max(
            Fixed32.Abs(scale.X),
            Fixed32.Max(
                Fixed32.Abs(scale.Y),
                Fixed32.Abs(scale.Z)));
    }

    private static FixedVector3 Abs(FixedVector3 value)
    {
        return new FixedVector3(
            Fixed32.Abs(value.X),
            Fixed32.Abs(value.Y),
            Fixed32.Abs(value.Z));
    }

    private static FixedVector3 Multiply(
        FixedVector3 left,
        FixedVector3 right)
    {
        return new FixedVector3(
            left.X * right.X,
            left.Y * right.Y,
            left.Z * right.Z);
    }
}