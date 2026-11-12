using Engine.Core.Math;

namespace Engine.Graphics3D.Lighting;

public readonly record struct DirectionalLight3D
{
    public Vector3 DirectionToLight { get; }
    public Vector3 Color { get; }
    public float Intensity { get; }

    public static DirectionalLight3D Default { get; } = new(
        new Vector3(-0.4f, 0.7f, 0.5f),
        Vector3.One,
        1.0f);

    public DirectionalLight3D(
        Vector3 directionToLight,
        Vector3 color,
        float intensity = 1.0f)
    {
        var lengthSquared = directionToLight.LengthSquared();

        if (!float.IsFinite(directionToLight.X) ||
            !float.IsFinite(directionToLight.Y) ||
            !float.IsFinite(directionToLight.Z) ||
            !float.IsFinite(lengthSquared) ||
            lengthSquared <= 1e-12f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(directionToLight));
        }

        if (!float.IsFinite(color.X) ||
            !float.IsFinite(color.Y) ||
            !float.IsFinite(color.Z) ||
            color.X < 0f || color.X > 1f ||
            color.Y < 0f || color.Y > 1f ||
            color.Z < 0f || color.Z > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(color));
        }

        if (!float.IsFinite(intensity) || intensity < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(intensity));
        }

        DirectionToLight = directionToLight.Normalize();
        Color = color;
        Intensity = intensity;
    }
}