namespace Engine.Core.Math;

public readonly struct FixedVector2 :
    IEquatable<FixedVector2>
{
    public FixedVector2(
        Fixed32 x,
        Fixed32 y)
    {
        X = x;
        Y = y;
    }

    public Fixed32 X { get; }

    public Fixed32 Y { get; }

    public static FixedVector2 Zero =>
        new(
            Fixed32.Zero,
            Fixed32.Zero);

    public static FixedVector2 operator +(
        FixedVector2 left,
        FixedVector2 right)
    {
        return new(
            left.X + right.X,
            left.Y + right.Y);
    }

    public static FixedVector2 operator -(
        FixedVector2 left,
        FixedVector2 right)
    {
        return new(
            left.X - right.X,
            left.Y - right.Y);
    }

    public static FixedVector2 operator -(
        FixedVector2 value)
    {
        return new(
            -value.X,
            -value.Y);
    }

    public static FixedVector2 operator *(
        FixedVector2 value,
        Fixed32 scalar)
    {
        return new(
            value.X * scalar,
            value.Y * scalar);
    }

    public static FixedVector2 operator *(
        Fixed32 scalar,
        FixedVector2 value)
    {
        return value * scalar;
    }

    public static FixedVector2 operator /(
        FixedVector2 value,
        Fixed32 scalar)
    {
        return new(
            value.X / scalar,
            value.Y / scalar);
    }

    public bool Equals(
        FixedVector2 other)
    {
        return X == other.X &&
               Y == other.Y;
    }

    public override bool Equals(
        object? obj)
    {
        return obj is FixedVector2 other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            X,
            Y);
    }

    public static bool operator ==(
        FixedVector2 left,
        FixedVector2 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        FixedVector2 left,
        FixedVector2 right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }

    public Fixed32 LengthSquared()
    {
        return
            X * X +
            Y * Y;
    }

    public Fixed32 Length()
    {
        return Fixed32.Sqrt(
            LengthSquared());
    }

    public Fixed32 Dot(
        FixedVector2 other)
    {
        return
            X * other.X +
            Y * other.Y;
    }

    public static Fixed32 DistanceSquared(
        FixedVector2 left,
        FixedVector2 right)
    {
        return
            (left - right)
                .LengthSquared();
    }

    public static Fixed32 Distance(
        FixedVector2 left,
        FixedVector2 right)
    {
        return Fixed32.Sqrt(
            DistanceSquared(
                left,
                right));
    }

    public FixedVector2 Normalize()
    {
        var length =
            Length();

        if (length == Fixed32.Zero)
            return Zero;

        return this / length;
    }

    public static FixedVector2 Lerp(
        FixedVector2 from,
        FixedVector2 to,
        Fixed32 amount)
    {
        return from +
               (to - from) *
               amount;
    }

    public static FixedVector2 Clamp(
        FixedVector2 value,
        FixedVector2 min,
        FixedVector2 max)
    {
        return new(
            Fixed32.Clamp(
                value.X,
                min.X,
                max.X),

            Fixed32.Clamp(
                value.Y,
                min.Y,
                max.Y));
    }

    public FixedVector2 WithX(
        Fixed32 x)
    {
        return new(
            x,
            Y);
    }

    public FixedVector2 WithY(
        Fixed32 y)
    {
        return new(
            X,
            y);
    }
}