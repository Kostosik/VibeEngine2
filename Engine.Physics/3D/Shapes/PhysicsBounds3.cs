using Engine.Core.Math;

namespace Engine.Physics.ThreeD.Shapes;

public readonly struct PhysicsBounds3 :
    IEquatable<PhysicsBounds3>
{
    private PhysicsBounds3(bool isInfinite)
    {
        Min = FixedVector3.Zero;
        Max = FixedVector3.Zero;
        IsInfinite = isInfinite;
    }

    public PhysicsBounds3(
        FixedVector3 min,
        FixedVector3 max)
    {
        if (max.X < min.X ||
            max.Y < min.Y ||
            max.Z < min.Z)
        {
            throw new ArgumentException(
                "Maximum bounds cannot be less than minimum bounds.",
                nameof(max));
        }

        Min = min;
        Max = max;
        IsInfinite = false;
    }

    public FixedVector3 Min { get; }

    public FixedVector3 Max { get; }

    public bool IsInfinite { get; }

    public FixedVector3 Center =>
        (Min + Max) * Fixed32.FromRatio(1, 2);

    public FixedVector3 Size =>
        Max - Min;

    public static PhysicsBounds3 Infinite =>
        new(true);

    public static PhysicsBounds3 FromCenterAndExtents(
        FixedVector3 center,
        FixedVector3 extents)
    {
        if (extents.X < Fixed32.Zero ||
            extents.Y < Fixed32.Zero ||
            extents.Z < Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(extents));
        }

        return new PhysicsBounds3(
            center - extents,
            center + extents);
    }

    public bool Contains(FixedVector3 point)
    {
        if (IsInfinite)
            return true;

        return
            point.X >= Min.X && point.X <= Max.X &&
            point.Y >= Min.Y && point.Y <= Max.Y &&
            point.Z >= Min.Z && point.Z <= Max.Z;
    }

    public bool Intersects(PhysicsBounds3 other)
    {
        if (IsInfinite || other.IsInfinite)
            return true;

        return
            Min.X <= other.Max.X &&
            Max.X >= other.Min.X &&
            Min.Y <= other.Max.Y &&
            Max.Y >= other.Min.Y &&
            Min.Z <= other.Max.Z &&
            Max.Z >= other.Min.Z;
    }

    public PhysicsBounds3 Translate(FixedVector3 offset)
    {
        if (IsInfinite)
            return Infinite;

        return new PhysicsBounds3(
            Min + offset,
            Max + offset);
    }

    public PhysicsBounds3 Expand(Fixed32 amount)
    {
        if (amount < Fixed32.Zero)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (IsInfinite)
            return Infinite;

        var expansion = new FixedVector3(
            amount,
            amount,
            amount);

        return new PhysicsBounds3(
            Min - expansion,
            Max + expansion);
    }

    public bool Equals(PhysicsBounds3 other)
    {
        return
            Min == other.Min &&
            Max == other.Max &&
            IsInfinite == other.IsInfinite;
    }

    public override bool Equals(object? obj)
    {
        return obj is PhysicsBounds3 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Min, Max, IsInfinite);
    }

    public static bool operator ==(
        PhysicsBounds3 left,
        PhysicsBounds3 right) => left.Equals(right);

    public static bool operator !=(
        PhysicsBounds3 left,
        PhysicsBounds3 right) => !left.Equals(right);
}