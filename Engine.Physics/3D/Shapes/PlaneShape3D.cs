using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Physics.ThreeD.Shapes;

public readonly struct PlaneShape3D :
    IDeterministicState,
    IEquatable<PlaneShape3D>
{
    public PlaneShape3D(
        FixedVector3 normal,
        Fixed32 distance)
    {
        var length = normal.Length();

        if (length <= Fixed32.Zero)
        {
            throw new ArgumentException(
                "Plane normal cannot be zero.",
                nameof(normal));
        }

        Normal = normal / length;
        Distance = distance / length;
    }

    public FixedVector3 Normal { get; }

    public Fixed32 Distance { get; }

    // Plane equation: Dot(Normal, point) = Distance.
    public PhysicsBounds3 GetBounds()
    {
        return PhysicsBounds3.Infinite;
    }

    public void AddToHash(ref DeterministicStateHasher hasher)
    {
        hasher.AddFixedVector3(Normal);
        hasher.AddFixed32(Distance);
    }

    public bool Equals(PlaneShape3D other)
    {
        return Normal == other.Normal &&
               Distance == other.Distance;
    }

    public override bool Equals(object? obj) =>
        obj is PlaneShape3D other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Normal, Distance);

    public static bool operator ==(
        PlaneShape3D left,
        PlaneShape3D right) => left.Equals(right);

    public static bool operator !=(
        PlaneShape3D left,
        PlaneShape3D right) => !left.Equals(right);
}