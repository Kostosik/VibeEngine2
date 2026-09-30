using Engine.Core.Math;

namespace Engine.Transform;

public static class FixedTransformOperations2D
{
    public static FixedTransform2D Translate(
        in FixedTransform2D transform,
        FixedVector2 offset)
    {
        var result = transform;

        result.Position += offset;

        return result;
    }

    public static FixedTransform2D Rotate(
        in FixedTransform2D transform,
        Fixed32 angle)
    {
        var result = transform;

        result.Rotation += angle;

        return result;
    }

    public static FixedTransform2D Scale(
        in FixedTransform2D transform,
        FixedVector2 scale)
    {
        var result = transform;

        result.Scale = MultiplyComponents(
            result.Scale,
            scale);

        return result;
    }

    public static FixedTransform2D Create(
        FixedVector2 position,
        Fixed32 rotation,
        FixedVector2 scale)
    {
        return new FixedTransform2D(position)
        {
            Rotation = rotation,
            Scale = scale
        };
    }

    public static FixedTransform2D Combine(
        in FixedTransform2D parent,
        in FixedTransform2D local)
    {
        var scaledPosition =
            MultiplyComponents(
                local.Position,
                parent.Scale);

        var rotatedPosition =
            Rotate(
                scaledPosition,
                parent.Rotation);

        return new FixedTransform2D(
            parent.Position +
            rotatedPosition)
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

    public static FixedVector2 TransformPoint(
        in FixedTransform2D transform,
        FixedVector2 point)
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

    private static FixedVector2 MultiplyComponents(
        FixedVector2 left,
        FixedVector2 right)
    {
        return new FixedVector2(
            left.X * right.X,
            left.Y * right.Y);
    }

    private static FixedVector2 Rotate(
        FixedVector2 value,
        Fixed32 angle)
    {
        var cosine =
            Fixed32.Cos(angle);

        var sine =
            Fixed32.Sin(angle);

        return new FixedVector2(
            value.X * cosine -
            value.Y * sine,

            value.X * sine +
            value.Y * cosine);
    }
}