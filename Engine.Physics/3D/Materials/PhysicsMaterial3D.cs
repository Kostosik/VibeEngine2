using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Physics.ThreeD.Materials;

public readonly struct PhysicsMaterial3D :
    IDeterministicState,
    IEquatable<PhysicsMaterial3D>
{
    public PhysicsMaterial3D(
        Fixed32 friction,
        Fixed32 restitution)
    {
        if (friction < Fixed32.Zero)
            throw new ArgumentOutOfRangeException(nameof(friction));

        if (restitution < Fixed32.Zero ||
            restitution > Fixed32.One)
        {
            throw new ArgumentOutOfRangeException(
                nameof(restitution));
        }

        Friction = friction;
        Restitution = restitution;
    }

    public Fixed32 Friction { get; }

    public Fixed32 Restitution { get; }

    public static PhysicsMaterial3D Default =>
        new(
            Fixed32.FromFloat(0.5f),
            Fixed32.Zero);

    public void AddToHash(ref DeterministicStateHasher hasher)
    {
        hasher.AddFixed32(Friction);
        hasher.AddFixed32(Restitution);
    }

    public bool Equals(PhysicsMaterial3D other)
    {
        return Friction == other.Friction &&
               Restitution == other.Restitution;
    }

    public override bool Equals(object? obj) =>
        obj is PhysicsMaterial3D other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Friction, Restitution);

    public static bool operator ==(
        PhysicsMaterial3D left,
        PhysicsMaterial3D right) => left.Equals(right);

    public static bool operator !=(
        PhysicsMaterial3D left,
        PhysicsMaterial3D right) => !left.Equals(right);
}