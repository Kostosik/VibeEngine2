namespace Engine.Core.Math;

public readonly struct FixedBounds2 :
    IEquatable<FixedBounds2>
{
    public FixedBounds2(
        FixedVector2 min,
        FixedVector2 max)
    {
        if (max.X < min.X)
        {
            throw new ArgumentException(
                "Max.X cannot be less than Min.X.",
                nameof(max));
        }

        if (max.Y < min.Y)
        {
            throw new ArgumentException(
                "Max.Y cannot be less than Min.Y.",
                nameof(max));
        }

        Min = min;
        Max = max;
    }

    public FixedVector2 Min { get; }

    public FixedVector2 Max { get; }

    public FixedVector2 Size =>
        Max - Min;

    public FixedVector2 Center =>
        (Min + Max) *
        Fixed32.FromFloat(0.5f);

    public bool Contains(
        FixedVector2 point)
    {
        return
            point.X >= Min.X &&
            point.X <= Max.X &&
            point.Y >= Min.Y &&
            point.Y <= Max.Y;
    }

    public bool Intersects(
        FixedBounds2 other)
    {
        return
            Min.X <= other.Max.X &&
            Max.X >= other.Min.X &&
            Min.Y <= other.Max.Y &&
            Max.Y >= other.Min.Y;
    }

    public bool Contains(
        FixedBounds2 other)
    {
        return
            other.Min.X >= Min.X &&
            other.Max.X <= Max.X &&
            other.Min.Y >= Min.Y &&
            other.Max.Y <= Max.Y;
    }

    public FixedBounds2 Translate(
        FixedVector2 offset)
    {
        return new(
            Min + offset,
            Max + offset);
    }

    public FixedBounds2 Expand(
        Fixed32 amount)
    {
        return new(
            Min - new FixedVector2(
                amount,
                amount),

            Max + new FixedVector2(
                amount,
                amount));
    }

    public static FixedBounds2 FromPositionSize(
        FixedVector2 position,
        FixedVector2 size)
    {
        if (size.X < Fixed32.Zero ||
            size.Y < Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                "Size cannot contain negative values.");
        }

        return new(
            position,
            position + size);
    }

    public bool Equals(
        FixedBounds2 other)
    {
        return
            Min == other.Min &&
            Max == other.Max;
    }

    public override bool Equals(
        object? obj)
    {
        return obj is FixedBounds2 other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Min,
            Max);
    }

    public static bool operator ==(
        FixedBounds2 left,
        FixedBounds2 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        FixedBounds2 left,
        FixedBounds2 right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"[{Min} - {Max}]";
    }
}