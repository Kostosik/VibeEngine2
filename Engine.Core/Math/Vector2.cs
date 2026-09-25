namespace Engine.Core.Math;

public readonly record struct Vector2(
    float X,
    float Y)
{
    public static Vector2 Zero =>
        new(0.0f, 0.0f);

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
        {
            throw new DivideByZeroException();
        }

        return new Vector2(
            value.X / scalar,
            value.Y / scalar);
    }
}