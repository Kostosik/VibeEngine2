using Engine.Core.Math;
using Engine.Physics.ThreeD.Components;
using Engine.Physics.ThreeD.Shapes;
using Engine.Transform;
using Xunit;

namespace Engine.Tests.Physics;

public sealed class PhysicsShape3DTests
{
    [Fact]
    public void SphereBounds_ApplyMaximumScale()
    {
        var shape = new SphereShape3D(Fixed32.FromInt(2));

        var bounds = shape.GetBounds(
            new FixedVector3(
                Fixed32.FromInt(4),
                Fixed32.FromInt(5),
                Fixed32.FromInt(6)),
            Fixed32.FromInt(3));

        Assert.Equal(
            new FixedVector3(
                Fixed32.FromInt(-2),
                Fixed32.FromInt(-1),
                Fixed32.Zero),
            bounds.Min);

        Assert.Equal(
            new FixedVector3(
                Fixed32.FromInt(10),
                Fixed32.FromInt(11),
                Fixed32.FromInt(12)),
            bounds.Max);
    }

    [Fact]
    public void BoxBounds_ApplyWorldScale()
    {
        var shape = new BoxShape3D(
            new FixedVector3(
                Fixed32.One,
                Fixed32.FromInt(2),
                Fixed32.FromInt(3)));

        var bounds = shape.GetBounds(
            FixedVector3.Zero,
            FixedQuaternion.Identity,
            new FixedVector3(
                Fixed32.FromInt(2),
                Fixed32.FromInt(3),
                Fixed32.FromInt(4)));

        Assert.Equal(
            new FixedVector3(
                Fixed32.FromInt(-2),
                Fixed32.FromInt(-6),
                Fixed32.FromInt(-12)),
            bounds.Min);

        Assert.Equal(
            new FixedVector3(
                Fixed32.FromInt(2),
                Fixed32.FromInt(6),
                Fixed32.FromInt(12)),
            bounds.Max);
    }

    [Fact]
    public void CapsuleBounds_ApplyScale()
    {
        var shape = new CapsuleShape3D(
            Fixed32.FromFloat(0.5f),
            Fixed32.FromInt(2));

        var bounds = shape.GetBounds(
            FixedVector3.Zero,
            FixedQuaternion.Identity,
            new FixedVector3(
                Fixed32.One,
                Fixed32.FromInt(2),
                Fixed32.One));

        Assert.Equal(
            new FixedVector3(
                Fixed32.FromInt(-1),
                Fixed32.FromInt(-5),
                Fixed32.FromInt(-1)),
            bounds.Min);

        Assert.Equal(
            new FixedVector3(
                Fixed32.One,
                Fixed32.FromInt(5),
                Fixed32.One),
            bounds.Max);
    }

    [Fact]
    public void Plane_NormalIsNormalized()
    {
        var plane = new PlaneShape3D(
            new FixedVector3(
                Fixed32.Zero,
                Fixed32.FromInt(2),
                Fixed32.Zero),
            Fixed32.FromInt(4));

        Assert.Equal(FixedVector3.UnitY, plane.Normal);
        Assert.Equal(Fixed32.FromInt(2), plane.Distance);

        Assert.True(plane.GetBounds().IsInfinite);
    }

    [Fact]
    public void Bounds_InfiniteIntersectsFiniteBounds()
    {
        var infinite = PhysicsBounds3.Infinite;

        var finite = PhysicsBounds3.FromCenterAndExtents(
            FixedVector3.Zero,
            FixedVector3.One);

        Assert.True(infinite.Intersects(finite));
        Assert.True(finite.Intersects(infinite));
    }

    [Fact]
    public void Collider3D_AppliesWorldOffset()
    {
        var collider = new Collider3D(
            new BoxShape3D(FixedVector3.One))
        {
            Offset = FixedVector3.UnitX
        };

        var transform = new FixedTransform3D(
            new FixedVector3(
                Fixed32.FromInt(10),
                Fixed32.Zero,
                Fixed32.Zero))
        {
            Scale = new FixedVector3(
                Fixed32.FromInt(2),
                Fixed32.One,
                Fixed32.One)
        };

        var position = collider.GetWorldPosition(transform);

        Assert.Equal(
            new FixedVector3(
                Fixed32.FromInt(12),
                Fixed32.Zero,
                Fixed32.Zero),
            position);
    }

    [Fact]
    public void Collider3D_FiltersCollisionLayers()
    {
        var first = new Collider3D(
            new SphereShape3D(Fixed32.One))
        {
            CollisionLayer = 1,
            CollisionMask = 2
        };

        var second = new Collider3D(
            new SphereShape3D(Fixed32.One))
        {
            CollisionLayer = 2,
            CollisionMask = 1
        };

        Assert.True(first.CanCollideWith(second));

        second.CollisionMask = 4;

        Assert.False(first.CanCollideWith(second));
    }

    [Fact]
    public void Collider3D_RejectsUninitializedShape()
    {
        Assert.Throws<ArgumentException>(
            () => new Collider3D(default(PhysicsShape3D)));
    }
}