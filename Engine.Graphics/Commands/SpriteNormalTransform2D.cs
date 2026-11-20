using Engine.Core.Math;

namespace Engine.Graphics.Commands;

/// <summary>
/// Transforms tangent-space sprite normals
/// into lighting-buffer coordinates.
///
/// The matrix represents the inverse-transpose
/// of the sprite's 2D transformation.
/// </summary>
public readonly record struct SpriteNormalTransform2D(
    float M11,
    float M12,
    float M21,
    float M22)
{
    public static SpriteNormalTransform2D Identity =>
        new(1.0f, 0.0f, 0.0f, 1.0f);

    public static SpriteNormalTransform2D Create(
        Vector2 scale,
        float rotationDegrees,
        bool flipX = false,
        bool flipY = false)
    {
        if (!float.IsFinite(scale.X) ||
            !float.IsFinite(scale.Y) ||
            scale.X <= 0.0f ||
            scale.Y <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scale));
        }

        if (!float.IsFinite(rotationDegrees))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rotationDegrees));
        }

        // World-to-screen rendering uses Y-down coordinates.
        // Lighting buffers use Y-up coordinates.
        // Therefore the rotation sign must be inverted.

        var radians =
            -rotationDegrees * (MathF.PI / 180.0f);

        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);

        // Inverse scale, including reflection.
        var inverseScaleX =
            (flipX ? -1.0f : 1.0f) / scale.X;

        var inverseScaleY =
            (flipY ? -1.0f : 1.0f) / scale.Y;

        // R * inverse(S)
        return new SpriteNormalTransform2D(
            cos * inverseScaleX,
            -sin * inverseScaleY,
            sin * inverseScaleX,
            cos * inverseScaleY);
    }

    public Vector3 TransformNormal(Vector3 normal)
    {
        if (!float.IsFinite(normal.X) ||
            !float.IsFinite(normal.Y) ||
            !float.IsFinite(normal.Z))
        {
            throw new ArgumentOutOfRangeException(
                nameof(normal));
        }

        var transformed = new Vector3(
            M11 * normal.X + M12 * normal.Y,
            M21 * normal.X + M22 * normal.Y,
            normal.Z);

        var lengthSquared =
            transformed.LengthSquared();

        if (!float.IsFinite(lengthSquared) ||
            lengthSquared <= 0.0000001f)
        {
            throw new ArgumentException(
                "Normal transformation produced an invalid vector.",
                nameof(normal));
        }

        return transformed / MathF.Sqrt(lengthSquared);
    }
}