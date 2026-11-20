namespace Engine.Core.Math;

public readonly record struct Vector4(
    float X,
    float Y,
    float Z,
    float W)
{
    public static Vector4 Zero =>
        new(0.0f, 0.0f, 0.0f, 0.0f);

    public static Vector4 One =>
        new(1.0f, 1.0f, 1.0f, 1.0f);

    public static Vector4 UnitX =>
        new(1.0f, 0.0f, 0.0f, 0.0f);

    public static Vector4 UnitY =>
        new(0.0f, 1.0f, 0.0f, 0.0f);

    public static Vector4 UnitZ =>
        new(0.0f, 0.0f, 1.0f, 0.0f);

    public static Vector4 UnitW =>
        new(0.0f, 0.0f, 0.0f, 1.0f);

    public float LengthSquared()
    {
        return X * X + Y * Y + Z * Z + W * W;
    }

    public float Length()
    {
        return MathF.Sqrt(LengthSquared());
    }

    public Vector4 Normalize()
    {
        var length = Length();

        if (length <= 0.0f)
            return Zero;

        return this / length;
    }

    public static float Dot(
        Vector4 left,
        Vector4 right)
    {
        return
            left.X * right.X +
            left.Y * right.Y +
            left.Z * right.Z +
            left.W * right.W;
    }

    public static float Distance(
        Vector4 left,
        Vector4 right)
    {
        return (right - left).Length();
    }

    public static Vector4 Lerp(
        Vector4 start,
        Vector4 end,
        float amount)
    {
        return start + (end - start) * amount;
    }

    public static Vector4 operator +(
        Vector4 left,
        Vector4 right)
    {
        return new Vector4(
            left.X + right.X,
            left.Y + right.Y,
            left.Z + right.Z,
            left.W + right.W);
    }

    public static Vector4 operator -(
        Vector4 left,
        Vector4 right)
    {
        return new Vector4(
            left.X - right.X,
            left.Y - right.Y,
            left.Z - right.Z,
            left.W - right.W);
    }

    public static Vector4 operator -(
        Vector4 value)
    {
        return new Vector4(
            -value.X,
            -value.Y,
            -value.Z,
            -value.W);
    }

    public static Vector4 operator *(
        Vector4 value,
        float scalar)
    {
        return new Vector4(
            value.X * scalar,
            value.Y * scalar,
            value.Z * scalar,
            value.W * scalar);
    }

    public static Vector4 operator *(
        float scalar,
        Vector4 value)
    {
        return value * scalar;
    }

    public static Vector4 operator /(
        Vector4 value,
        float scalar)
    {
        if (scalar == 0.0f)
            throw new DivideByZeroException();

        return new Vector4(
            value.X / scalar,
            value.Y / scalar,
            value.Z / scalar,
            value.W / scalar);
    }
}