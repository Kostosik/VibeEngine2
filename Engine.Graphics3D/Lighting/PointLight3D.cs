using Engine.Core.Math;

namespace Engine.Graphics3D.Lighting;

public readonly record struct PointLight3D
{
    public Vector3 Position { get; }
    public Vector3 Color { get; }
    public float Intensity { get; }
    public float Range { get; }

    public static PointLight3D Disabled { get; } = new(
        Vector3.Zero,
        Vector3.One,
        intensity: 0f,
        range: 1f);

    public PointLight3D(
        Vector3 position,
        Vector3 color,
        float intensity = 1f,
        float range = 8f)
    {
        if (!float.IsFinite(position.X) ||
            !float.IsFinite(position.Y) ||
            !float.IsFinite(position.Z))
        {
            throw new ArgumentOutOfRangeException(nameof(position));
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
            throw new ArgumentOutOfRangeException(nameof(intensity));

        if (!float.IsFinite(range) || range <= 0f)
            throw new ArgumentOutOfRangeException(nameof(range));

        Position = position;
        Color = color;
        Intensity = intensity;
        Range = range;
    }
}