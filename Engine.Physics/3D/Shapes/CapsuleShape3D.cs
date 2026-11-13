using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Physics.ThreeD.Shapes;

public readonly struct CapsuleShape3D :
    IDeterministicState,
    IEquatable<CapsuleShape3D>
{
    public CapsuleShape3D(
        Fixed32 radius,
        Fixed32 halfHeight)
    {
        if (radius <= Fixed32.Zero)
            throw new ArgumentOutOfRangeException(nameof(radius));

        if (halfHeight < Fixed32.Zero)
            throw new ArgumentOutOfRangeException(nameof(halfHeight));

        Radius = radius;
        HalfHeight = halfHeight;
    }

    public Fixed32 Radius { get; }

    public Fixed32 HalfHeight { get; }

    public Fixed32 Height =>
        (HalfHeight + Radius) * Fixed32.FromInt(2);

    public PhysicsBounds3 GetBounds(
        FixedVector3 center,
        FixedQuaternion rotation,
        FixedVector3 scale)
    {
        var scaleX = Fixed32.Abs(scale.X);
        var scaleY = Fixed32.Abs(scale.Y);
        var scaleZ = Fixed32.Abs(scale.Z);

        // Консервативный радиус при неравномерном масштабе.
        var radiusScale = Fixed32.Max(
            scaleX,
            Fixed32.Max(scaleY, scaleZ));

        var scaledRadius = Radius * radiusScale;
        var scaledHalfHeight = HalfHeight * scaleY;

        var axisY = rotation.Transform(FixedVector3.UnitY);
        var segment = axisY * scaledHalfHeight;

        var extents = new FixedVector3(
            Fixed32.Abs(segment.X) + scaledRadius,
            Fixed32.Abs(segment.Y) + scaledRadius,
            Fixed32.Abs(segment.Z) + scaledRadius);

        return PhysicsBounds3.FromCenterAndExtents(
            center,
            extents);
    }

    public void AddToHash(ref DeterministicStateHasher hasher)
    {
        hasher.AddFixed32(Radius);
        hasher.AddFixed32(HalfHeight);
    }

    public bool Equals(CapsuleShape3D other)
    {
        return Radius == other.Radius &&
               HalfHeight == other.HalfHeight;
    }

    public override bool Equals(object? obj) =>
        obj is CapsuleShape3D other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Radius, HalfHeight);

    public static bool operator ==(
        CapsuleShape3D left,
        CapsuleShape3D right) => left.Equals(right);

    public static bool operator !=(
        CapsuleShape3D left,
        CapsuleShape3D right) => !left.Equals(right);
}