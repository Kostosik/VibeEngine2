using Engine.Core.Math;

namespace Engine.Transform;

public static class TransformOperations2D
{
    public static Transform2D Combine(
        in Transform2D parent,
        in Transform2D local)
    {
        var scaledLocalPosition =
            MultiplyComponents(
                local.Position,
                parent.Scale);

        var rotatedLocalPosition =
            Rotate(
                scaledLocalPosition,
                parent.Rotation);

        return new Transform2D(
            parent.Position +
            rotatedLocalPosition)
        {
            Rotation =
                parent.Rotation +
                local.Rotation,

            Scale =
                MultiplyComponents(
                    parent.Scale,
                    local.Scale)
        };
    }

    public static Vector2 TransformPoint(
        in Transform2D transform,
        Vector2 point)
    {
        var scaled =
            MultiplyComponents(
                point,
                transform.Scale);

        return transform.Position +
               Rotate(
                   scaled,
                   transform.Rotation);
    }

    private static Vector2 MultiplyComponents(
        Vector2 left,
        Vector2 right)
    {
        return new Vector2(
            left.X * right.X,
            left.Y * right.Y);
    }

    private static Vector2 Rotate(
        Vector2 value,
        float angle)
    {
        var cosine =
            MathF.Cos(angle);

        var sine =
            MathF.Sin(angle);

        return new Vector2(
            value.X * cosine -
            value.Y * sine,

            value.X * sine +
            value.Y * cosine);
    }
}