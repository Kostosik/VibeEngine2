using Engine.Core.Math;

namespace Engine.Transform;

public static class TransformOperations3D
{
    public static Transform3D Create(
        Vector3 position,
        Quaternion rotation,
        Vector3 scale)
    {
        return new Transform3D(position)
        {
            Rotation = rotation.Normalize(),
            Scale = scale
        };
    }

    public static Transform3D Translate(
        in Transform3D transform,
        Vector3 offset)
    {
        var result = transform;
        result.Position += offset;
        return result;
    }

    public static Transform3D Rotate(
        in Transform3D transform,
        Quaternion rotation)
    {
        var result = transform;
        result.Rotation =
            (result.Rotation * rotation).Normalize();

        return result;
    }

    public static Transform3D ScaleBy(
        in Transform3D transform,
        Vector3 scale)
    {
        var result = transform;

        result.Scale = MultiplyComponents(
            result.Scale,
            scale);

        return result;
    }

    public static Transform3D Combine(
        in Transform3D parent,
        in Transform3D local)
    {
        var scaledPosition = MultiplyComponents(
            local.Position,
            parent.Scale);

        var rotatedPosition =
            parent.Rotation.Transform(scaledPosition);

        return new Transform3D(
            parent.Position + rotatedPosition)
        {
            Rotation =
                (parent.Rotation * local.Rotation).Normalize(),

            Scale =
                MultiplyComponents(
                    parent.Scale,
                    local.Scale)
        };
    }

    public static Vector3 TransformPoint(
        in Transform3D transform,
        Vector3 point)
    {
        var scaled = MultiplyComponents(
            point,
            transform.Scale);

        return transform.Position +
               transform.Rotation.Transform(scaled);
    }

    public static Vector3 TransformDirection(
        in Transform3D transform,
        Vector3 direction)
    {
        return transform.Rotation.Transform(direction);
    }

    private static Vector3 MultiplyComponents(
        Vector3 left,
        Vector3 right)
    {
        return new Vector3(
            left.X * right.X,
            left.Y * right.Y,
            left.Z * right.Z);
    }
}