using Engine.Core.Math;

namespace Engine.Transform;

public static class FixedTransformOperations3D
{
    public static FixedTransform3D Create(
        FixedVector3 position,
        FixedQuaternion rotation,
        FixedVector3 scale)
    {
        return new FixedTransform3D(position)
        {
            Rotation = rotation.Normalize(),
            Scale = scale
        };
    }

    public static FixedTransform3D Translate(
        FixedTransform3D transform,
        FixedVector3 offset)
    {
        transform.Position += offset;
        return transform;
    }

    public static FixedTransform3D Rotate(
        FixedTransform3D transform,
        FixedQuaternion rotation)
    {
        transform.Rotation =
            (transform.Rotation * rotation).Normalize();

        return transform;
    }

    public static FixedTransform3D ScaleBy(
        FixedTransform3D transform,
        FixedVector3 scale)
    {
        transform.Scale = Multiply(transform.Scale, scale);
        return transform;
    }

    public static FixedTransform3D Combine(
        FixedTransform3D parent,
        FixedTransform3D local)
    {
        var scaledPosition = Multiply(
            local.Position,
            parent.Scale);

        return new FixedTransform3D(
            parent.Position +
            parent.Rotation.Transform(scaledPosition))
        {
            Rotation = (
                parent.Rotation * local.Rotation
            ).Normalize(),

            Scale = Multiply(parent.Scale, local.Scale)
        };
    }

    public static FixedVector3 TransformPoint(
        FixedTransform3D transform,
        FixedVector3 point)
    {
        var scaledPoint = Multiply(point, transform.Scale);

        return transform.Position +
               transform.Rotation.Transform(scaledPoint);
    }

    public static FixedVector3 TransformDirection(
        FixedTransform3D transform,
        FixedVector3 direction)
    {
        return transform.Rotation.Transform(direction);
    }

    private static FixedVector3 Multiply(
        FixedVector3 left,
        FixedVector3 right)
    {
        return new FixedVector3(
            left.X * right.X,
            left.Y * right.Y,
            left.Z * right.Z);
    }
}