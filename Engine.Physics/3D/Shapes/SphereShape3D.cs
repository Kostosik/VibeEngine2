using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Physics.ThreeD.Shapes;

public readonly struct SphereShape3D :
    IDeterministicState,
    IEquatable<SphereShape3D>
{
    public SphereShape3D(Fixed32 radius)
    {
        if (radius <= Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(radius));
        }

        Radius = radius;
    }

    public Fixed32 Radius { get; }

    public PhysicsBounds3 GetBounds(FixedVector3 center)
    {
        return GetBounds(center, Fixed32.One);
    }

    public PhysicsBounds3 GetBounds(
        FixedVector3 center,
        Fixed32 scale)
    {
        var scaledRadius = Radius * Fixed32.Abs(scale);

        var extents = new FixedVector3(
            scaledRadius,
            scaledRadius,
            scaledRadius);

        return PhysicsBounds3.FromCenterAndExtents(
            center,
            extents);
    }

    public void AddToHash(ref DeterministicStateHasher hasher)
    {
        hasher.AddFixed32(Radius);
    }

    public bool Equals(SphereShape3D other) =>
        Radius == other.Radius;

    public override bool Equals(object? obj) =>
        obj is SphereShape3D other && Equals(other);

    public override int GetHashCode() => Radius.GetHashCode();

    public static bool operator ==(
        SphereShape3D left,
        SphereShape3D right) => left.Equals(right);

    public static bool operator !=(
        SphereShape3D left,
        SphereShape3D right) => !left.Equals(right);
}