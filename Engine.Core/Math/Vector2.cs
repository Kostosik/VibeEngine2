
namespace Engine.Core.Math;

public readonly record struct Vector2(
    float X,
    float Y)
{
    public static Vector2 Zero =>
        new(0.0f, 0.0f);

    public static Vector2 One =>
        new(1.0f, 1.0f);

    public static Vector2 UnitX =>
        new(1.0f, 0.0f);

    public static Vector2 UnitY =>
        new(0.0f, 1.0f);

    public bool IsFinite =>
        float.IsFinite(X) &&
        float.IsFinite(Y);

    public float LengthSquared()
    {
        return X * X + Y * Y;
    }

    public float Length()
    {
        return MathF.Sqrt(LengthSquared());
    }

    public Vector2 Normalize()
    {
        var length = Length();

        if (length <= 0.0f)
            return Zero;

        return this / length;
    }

    public static float Dot(
        Vector2 left,
        Vector2 right)
    {
        return left.X * right.X +
               left.Y * right.Y;
    }

    /// <summary>
    /// Returns the signed scalar 2D cross product.
    /// Positive value means counter-clockwise orientation.
    /// </summary>
    public static float Cross(
        Vector2 left,
        Vector2 right)
    {
        return left.X * right.Y -
               left.Y * right.X;
    }

    public static float DistanceSquared(
        Vector2 left,
        Vector2 right)
    {
        return (right - left).LengthSquared();
    }

    public static float Distance(
        Vector2 left,
        Vector2 right)
    {
        return MathF.Sqrt(
            DistanceSquared(left, right));
    }

    /// <summary>
    /// Linear interpolation.
    /// Amount is not clamped to [0, 1].
    /// </summary>
    public static Vector2 Lerp(
        Vector2 start,
        Vector2 end,
        float amount)
    {
        return start + (end - start) * amount;
    }

    public static Vector2 Min(
        Vector2 left,
        Vector2 right)
    {
        return new Vector2(
            MathF.Min(left.X, right.X),
            MathF.Min(left.Y, right.Y));
    }

    public static Vector2 Max(
        Vector2 left,
        Vector2 right)
    {
        return new Vector2(
            MathF.Max(left.X, right.X),
            MathF.Max(left.Y, right.Y));
    }

    public static Vector2 Clamp(
        Vector2 value,
        Vector2 min,
        Vector2 max)
    {
        if (min.X > max.X || min.Y > max.Y)
        {
            throw new ArgumentException(
                "Minimum components cannot exceed maximum components.");
        }

        return new Vector2(
            System.Math.Clamp(value.X, min.X, max.X),
            System.Math.Clamp(value.Y, min.Y, max.Y));
    }

    public static Vector2 MultiplyComponents(
        Vector2 left,
        Vector2 right)
    {
        return new Vector2(
            left.X * right.X,
            left.Y * right.Y);
    }

    public static Vector2 DivideComponents(
        Vector2 left,
        Vector2 right)
    {
        if (right.X == 0.0f || right.Y == 0.0f)
            throw new DivideByZeroException();

        return new Vector2(
            left.X / right.X,
            left.Y / right.Y);
    }

    /// <summary>
    /// Returns a vector rotated 90 degrees counter-clockwise.
    /// </summary>
    public Vector2 Perpendicular()
    {
        return new Vector2(-Y, X);
    }

    /// <summary>
    /// Rotates the vector counter-clockwise.
    /// Angle is specified in radians.
    /// </summary>
    public Vector2 Rotated(float radians)
    {
        var cosine = MathF.Cos(radians);
        var sine = MathF.Sin(radians);

        return new Vector2(
            X * cosine - Y * sine,
            X * sine + Y * cosine);
    }

    public static Vector2 operator +(
        Vector2 left,
        Vector2 right)
    {
        return new Vector2(
            left.X + right.X,
            left.Y + right.Y);
    }

    public static Vector2 operator -(
        Vector2 left,
        Vector2 right)
    {
        return new Vector2(
            left.X - right.X,
            left.Y - right.Y);
    }

    public static Vector2 operator -(
        Vector2 value)
    {
        return new Vector2(
            -value.X,
            -value.Y);
    }

    public static Vector2 operator *(
        Vector2 value,
        float scalar)
    {
        return new Vector2(
            value.X * scalar,
            value.Y * scalar);
    }

    public static Vector2 operator *(
        float scalar,
        Vector2 value)
    {
        return value * scalar;
    }

    public static Vector2 operator /(
        Vector2 value,
        float scalar)
    {
        if (scalar == 0.0f)
            throw new DivideByZeroException();

        return new Vector2(
            value.X / scalar,
            value.Y / scalar);
    }

    public static Vector2 operator *(
    Vector2 left,
    Vector2 right)
    {
        return MultiplyComponents(left, right);
    }
}
