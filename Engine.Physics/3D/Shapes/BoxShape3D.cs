using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Physics.ThreeD.Shapes;

public readonly struct BoxShape3D :
    IDeterministicState,
    IEquatable<BoxShape3D>
{
    public BoxShape3D(FixedVector3 halfExtents)
    {
        if (halfExtents.X <= Fixed32.Zero ||
            halfExtents.Y <= Fixed32.Zero ||
            halfExtents.Z <= Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(halfExtents));
        }

        HalfExtents = halfExtents;
    }

    public FixedVector3 HalfExtents { get; }

    public FixedVector3 Size => HalfExtents * Fixed32.FromInt(2);

    public PhysicsBounds3 GetBounds(
        FixedVector3 center,
        FixedQuaternion rotation,
        FixedVector3 scale)
    {
        var extents = Multiply(
            HalfExtents,
            Abs(scale));

        var axisX = rotation.Transform(FixedVector3.UnitX);
        var axisY = rotation.Transform(FixedVector3.UnitY);
        var axisZ = rotation.Transform(FixedVector3.UnitZ);

        var worldExtents = new FixedVector3(
            Fixed32.Abs(axisX.X) * extents.X +
            Fixed32.Abs(axisY.X) * extents.Y +
            Fixed32.Abs(axisZ.X) * extents.Z,

            Fixed32.Abs(axisX.Y) * extents.X +
            Fixed32.Abs(axisY.Y) * extents.Y +
            Fixed32.Abs(axisZ.Y) * extents.Z,

            Fixed32.Abs(axisX.Z) * extents.X +
            Fixed32.Abs(axisY.Z) * extents.Y +
            Fixed32.Abs(axisZ.Z) * extents.Z);

        return PhysicsBounds3.FromCenterAndExtents(
            center,
            worldExtents);
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

    private static FixedVector3 Abs(FixedVector3 value)
    {
        return new FixedVector3(
            Fixed32.Abs(value.X),
            Fixed32.Abs(value.Y),
            Fixed32.Abs(value.Z));
    }

    public void AddToHash(ref DeterministicStateHasher hasher)
    {
        hasher.AddFixedVector3(HalfExtents);
    }

    public bool Equals(BoxShape3D other) =>
        HalfExtents == other.HalfExtents;

    public override bool Equals(object? obj) =>
        obj is BoxShape3D other && Equals(other);

    public override int GetHashCode() => HalfExtents.GetHashCode();

    public static bool operator ==(
        BoxShape3D left,
        BoxShape3D right) => left.Equals(right);

    public static bool operator !=(
        BoxShape3D left,
        BoxShape3D right) => !left.Equals(right);
}