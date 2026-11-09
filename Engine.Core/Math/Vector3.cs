
namespace Engine.Core.Math;

public readonly record struct Vector3(
    float X,
    float Y,
    float Z)
{
    public static Vector3 Zero =>
        new(0.0f, 0.0f, 0.0f);

    public static Vector3 One =>
        new(1.0f, 1.0f, 1.0f);

    public static Vector3 UnitX =>
        new(1.0f, 0.0f, 0.0f);

    public static Vector3 UnitY =>
        new(0.0f, 1.0f, 0.0f);

    public static Vector3 UnitZ =>
        new(0.0f, 0.0f, 1.0f);

    public float LengthSquared()
    {
        return X * X + Y * Y + Z * Z;
    }

    public float Length()
    {
        return MathF.Sqrt(LengthSquared());
    }

    public Vector3 Normalize()
    {
        var length = Length();

        if (length <= 0.0f)
        {
            return Zero;
        }

        return this / length;
    }

    public static float Dot(
        Vector3 left,
        Vector3 right)
    {
        return
            left.X * right.X +
            left.Y * right.Y +
            left.Z * right.Z;
    }

    public static Vector3 Cross(
        Vector3 left,
        Vector3 right)
    {
        return new Vector3(
            left.Y * right.Z - left.Z * right.Y,
            left.Z * right.X - left.X * right.Z,
            left.X * right.Y - left.Y * right.X);
    }

    public static float Distance(
        Vector3 left,
        Vector3 right)
    {
        return (right - left).Length();
    }

    public static Vector3 Lerp(
        Vector3 start,
        Vector3 end,
        float amount)
    {
        return start + (end - start) * amount;
    }

    public static Vector3 operator +(
        Vector3 left,
        Vector3 right)
    {
        return new Vector3(
            left.X + right.X,
            left.Y + right.Y,
            left.Z + right.Z);
    }

    public static Vector3 operator -(
        Vector3 left,
        Vector3 right)
    {
        return new Vector3(
            left.X - right.X,
            left.Y - right.Y,
            left.Z - right.Z);
    }

    public static Vector3 operator -(
        Vector3 value)
    {
        return new Vector3(
            -value.X,
            -value.Y,
            -value.Z);
    }

    public static Vector3 operator *(
        Vector3 value,
        float scalar)
    {
        return new Vector3(
            value.X * scalar,
            value.Y * scalar,
            value.Z * scalar);
    }

    public static Vector3 operator *(
        float scalar,
        Vector3 value)
    {
        return value * scalar;
    }

    public static Vector3 operator /(
        Vector3 value,
        float scalar)
    {
        if (scalar == 0.0f)
        {
            throw new DivideByZeroException();
        }

        return new Vector3(
            value.X / scalar,
            value.Y / scalar,
            value.Z / scalar);
    }
}
