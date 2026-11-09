
namespace Engine.Core.Math;

public readonly struct FixedVector3 :
    IEquatable<FixedVector3>
{
    public FixedVector3(
        Fixed32 x,
        Fixed32 y,
        Fixed32 z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Fixed32 X { get; }
    public Fixed32 Y { get; }
    public Fixed32 Z { get; }

    public static FixedVector3 Zero =>
        new(Fixed32.Zero, Fixed32.Zero, Fixed32.Zero);

    public static FixedVector3 One =>
        new(Fixed32.One, Fixed32.One, Fixed32.One);

    public static FixedVector3 UnitX =>
        new(Fixed32.One, Fixed32.Zero, Fixed32.Zero);

    public static FixedVector3 UnitY =>
        new(Fixed32.Zero, Fixed32.One, Fixed32.Zero);

    public static FixedVector3 UnitZ =>
        new(Fixed32.Zero, Fixed32.Zero, Fixed32.One);

    public static FixedVector3 operator +(
        FixedVector3 left,
        FixedVector3 right)
    {
        return new(
            left.X + right.X,
            left.Y + right.Y,
            left.Z + right.Z);
    }

    public static FixedVector3 operator -(
        FixedVector3 left,
        FixedVector3 right)
    {
        return new(
            left.X - right.X,
            left.Y - right.Y,
            left.Z - right.Z);
    }

    public static FixedVector3 operator -(
        FixedVector3 value)
    {
        return new(-value.X, -value.Y, -value.Z);
    }

    public static FixedVector3 operator *(
        FixedVector3 value,
        Fixed32 scalar)
    {
        return new(
            value.X * scalar,
            value.Y * scalar,
            value.Z * scalar);
    }

    public static FixedVector3 operator *(
        Fixed32 scalar,
        FixedVector3 value)
    {
        return value * scalar;
    }

    public static FixedVector3 operator /(
        FixedVector3 value,
        Fixed32 scalar)
    {
        return new(
            value.X / scalar,
            value.Y / scalar,
            value.Z / scalar);
    }

    public bool Equals(FixedVector3 other)
    {
        return X == other.X &&
               Y == other.Y &&
               Z == other.Z;
    }

    public override bool Equals(object? obj)
    {
        return obj is FixedVector3 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }

    public static bool operator ==(
        FixedVector3 left,
        FixedVector3 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        FixedVector3 left,
        FixedVector3 right)
    {
        return !left.Equals(right);
    }

    public Fixed32 LengthSquared()
    {
        return X * X + Y * Y + Z * Z;
    }

    public Fixed32 Length()
    {
        return Fixed32.Sqrt(LengthSquared());
    }

    public Fixed32 Dot(FixedVector3 other)
    {
        return X * other.X +
               Y * other.Y +
               Z * other.Z;
    }

    public FixedVector3 Cross(FixedVector3 other)
    {
        return new FixedVector3(
            Y * other.Z - Z * other.Y,
            Z * other.X - X * other.Z,
            X * other.Y - Y * other.X);
    }

    public FixedVector3 Normalize()
    {
        var length = Length();

        if (length == Fixed32.Zero)
            return Zero;

        return this / length;
    }

    public static Fixed32 DistanceSquared(
        FixedVector3 left,
        FixedVector3 right)
    {
        return (left - right).LengthSquared();
    }

    public static Fixed32 Distance(
        FixedVector3 left,
        FixedVector3 right)
    {
        return Fixed32.Sqrt(DistanceSquared(left, right));
    }

    public static FixedVector3 Lerp(
        FixedVector3 from,
        FixedVector3 to,
        Fixed32 amount)
    {
        return from + (to - from) * amount;
    }

    public FixedVector3 WithX(Fixed32 x)
    {
        return new(x, Y, Z);
    }

    public FixedVector3 WithY(Fixed32 y)
    {
        return new(X, y, Z);
    }

    public FixedVector3 WithZ(Fixed32 z)
    {
        return new(X, Y, z);
    }

    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }
}
