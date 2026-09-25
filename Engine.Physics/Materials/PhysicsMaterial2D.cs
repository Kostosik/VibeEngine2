using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Physics.Materials;

public readonly struct PhysicsMaterial2D :
    IDeterministicState,
    IEquatable<PhysicsMaterial2D>
{
    public PhysicsMaterial2D(
        Fixed32 friction,
        Fixed32 restitution)
    {
        if (friction < Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(friction));
        }

        if (restitution < Fixed32.Zero ||
            restitution > Fixed32.One)
        {
            throw new ArgumentOutOfRangeException(
                nameof(restitution),
                "Restitution must be between zero and one.");
        }

        Friction = friction;
        Restitution = restitution;
    }

    public Fixed32 Friction { get; }

    public Fixed32 Restitution { get; }

    public static PhysicsMaterial2D Default =>
        new(
            Fixed32.FromFloat(0.5f),
            Fixed32.Zero);

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        hasher.AddFixed32(Friction);
        hasher.AddFixed32(Restitution);
    }

    public bool Equals(
        PhysicsMaterial2D other)
    {
        return Friction == other.Friction &&
               Restitution == other.Restitution;
    }

    public override bool Equals(
        object? obj)
    {
        return obj is PhysicsMaterial2D other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Friction,
            Restitution);
    }

    public static bool operator ==(
        PhysicsMaterial2D left,
        PhysicsMaterial2D right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        PhysicsMaterial2D left,
        PhysicsMaterial2D right)
    {
        return !left.Equals(right);
    }
}