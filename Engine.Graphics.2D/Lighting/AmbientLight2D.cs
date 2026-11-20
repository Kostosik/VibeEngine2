using Engine.Core.Math;

namespace Engine.Graphics2D.Lighting;

/// <summary>
/// Describes uniform ambient lighting for a 2D scene.
/// Color values are expected in linear RGB space.
/// </summary>
public readonly record struct AmbientLight2D : ILightingContribution2D
{
    public LightingChannel2D Channel =>
    LightingChannel2D.Ambient;
    public Vector3 Color { get; }

    public float Intensity { get; }

    public bool IsEnabled => Intensity > 0.0f;

    public static AmbientLight2D Disabled { get; } =
        new(Vector3.Zero, 0.0f);

    public AmbientLight2D(
        Vector3 color,
        float intensity = 1.0f)
    {
        if (!float.IsFinite(color.X) ||
            !float.IsFinite(color.Y) ||
            !float.IsFinite(color.Z) ||
            color.X < 0.0f || color.X > 1.0f ||
            color.Y < 0.0f || color.Y > 1.0f ||
            color.Z < 0.0f || color.Z > 1.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(color),
                "Ambient color must contain finite linear RGB values between 0 and 1.");
        }

        if (!float.IsFinite(intensity) || intensity < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intensity));
        }

        Color = color;
        Intensity = intensity;
    }
}