using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Physics.Shapes;

public readonly struct AabbShape2D :
    IDeterministicState,
    IEquatable<AabbShape2D>
{
    public AabbShape2D(
        FixedVector2 size)
    {
        if (size.X <= Fixed32.Zero ||
            size.Y <= Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                "AABB size must be greater than zero.");
        }

        Size = size;
    }

    public FixedVector2 Size { get; }

    public FixedVector2 HalfSize =>
        Size *
        Fixed32.FromRatio(1, 2);

    public FixedBounds2 GetBounds(
        FixedVector2 position)
    {
        return FixedBounds2.FromPositionSize(
            position - HalfSize,
            Size);
    }

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        hasher.AddFixedVector2(Size);
    }

    public bool Equals(
        AabbShape2D other)
    {
        return Size == other.Size;
    }

    public override bool Equals(
        object? obj)
    {
        return obj is AabbShape2D other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        return Size.GetHashCode();
    }

    public static bool operator ==(
        AabbShape2D left,
        AabbShape2D right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        AabbShape2D left,
        AabbShape2D right)
    {
        return !left.Equals(right);
    }
}