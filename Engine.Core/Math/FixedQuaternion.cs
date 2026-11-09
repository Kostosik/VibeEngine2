
namespace Engine.Core.Math;

public readonly struct FixedQuaternion :
    IEquatable<FixedQuaternion>
{
    public FixedQuaternion(
        Fixed32 x,
        Fixed32 y,
        Fixed32 z,
        Fixed32 w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public Fixed32 X { get; }
    public Fixed32 Y { get; }
    public Fixed32 Z { get; }
    public Fixed32 W { get; }

    public static FixedQuaternion Identity =>
        new(
            Fixed32.Zero,
            Fixed32.Zero,
            Fixed32.Zero,
            Fixed32.One);

    public static FixedQuaternion operator -(
        FixedQuaternion value)
    {
        return new(
            -value.X,
            -value.Y,
            -value.Z,
            -value.W);
    }

    public static FixedQuaternion operator *(
        FixedQuaternion left,
        FixedQuaternion right)
    {
        return new FixedQuaternion(
            left.W * right.X + left.X * right.W +
            left.Y * right.Z - left.Z * right.Y,

            left.W * right.Y - left.X * right.Z +
            left.Y * right.W + left.Z * right.X,

            left.W * right.Z + left.X * right.Y -
            left.Y * right.X + left.Z * right.W,

            left.W * right.W - left.X * right.X -
            left.Y * right.Y - left.Z * right.Z);
    }

    public static FixedQuaternion operator *(
        FixedQuaternion value,
        Fixed32 scalar)
    {
        return new FixedQuaternion(
            value.X * scalar,
            value.Y * scalar,
            value.Z * scalar,
            value.W * scalar);
    }

    public static FixedQuaternion operator /(
        FixedQuaternion value,
        Fixed32 scalar)
    {
        return new FixedQuaternion(
            value.X / scalar,
            value.Y / scalar,
            value.Z / scalar,
            value.W / scalar);
    }

    public Fixed32 LengthSquared()
    {
        return X * X + Y * Y + Z * Z + W * W;
    }

    public Fixed32 Length()
    {
        return Fixed32.Sqrt(LengthSquared());
    }

    public FixedQuaternion Normalize()
    {
        var length = Length();

        if (length == Fixed32.Zero)
            return Identity;

        return this / length;
    }

    public FixedQuaternion Conjugate()
    {
        return new FixedQuaternion(-X, -Y, -Z, W);
    }

    public FixedQuaternion Inverse()
    {
        var lengthSquared = LengthSquared();

        if (lengthSquared == Fixed32.Zero)
            return Identity;

        return Conjugate() / lengthSquared;
    }

    public Fixed32 Dot(FixedQuaternion other)
    {
        return X * other.X +
               Y * other.Y +
               Z * other.Z +
               W * other.W;
    }

    public FixedVector3 Transform(FixedVector3 vector)
    {
        // Оптимизированное вращение:
        // v' = v + w * t + cross(q.xyz, t)
        // t = 2 * cross(q.xyz, v)
        //
        // Quaternion должен быть нормализован.

        var q = new FixedVector3(X, Y, Z);

        var two = Fixed32.FromInt(2);
        var t = q.Cross(vector) * two;

        return vector +
               t * W +
               q.Cross(t);
    }

    public static FixedQuaternion CreateFromAxisAngle(
        FixedVector3 axis,
        Fixed32 angle)
    {
        var normalizedAxis = axis.Normalize();

        if (normalizedAxis == FixedVector3.Zero)
            return Identity;

        var halfAngle = angle / Fixed32.FromInt(2);

        var sine = Fixed32.Sin(halfAngle);
        var cosine = Fixed32.Cos(halfAngle);

        return new FixedQuaternion(
            normalizedAxis.X * sine,
            normalizedAxis.Y * sine,
            normalizedAxis.Z * sine,
            cosine).Normalize();
    }

    public bool Equals(FixedQuaternion other)
    {
        return X == other.X &&
               Y == other.Y &&
               Z == other.Z &&
               W == other.W;
    }

    public override bool Equals(object? obj)
    {
        return obj is FixedQuaternion other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z, W);
    }

    public static bool operator ==(
        FixedQuaternion left,
        FixedQuaternion right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        FixedQuaternion left,
        FixedQuaternion right)
    {
        return !left.Equals(right);
    }
}
