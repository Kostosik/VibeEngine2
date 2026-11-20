using Engine.Core.Math;

namespace Engine.Graphics2D.Lighting;

/// <summary>
/// Describes a 2D point light source.
/// Color values are expected in linear RGB space.
/// </summary>
public readonly record struct Light2D : ILightingContribution2D
{
    public LightingChannel2D Channel =>
    LightingChannel2D.Direct;
    public Vector2 Position { get; }

    public Vector3 Color { get; }

    public float Radius { get; }

    public float Intensity { get; }

    public bool IsEnabled => Intensity > 0.0f && Radius > 0.0f;

    public Light2D(
        Vector2 position,
        Vector3 color,
        float radius,
        float intensity = 1.0f)
    {
        if (!float.IsFinite(position.X) ||
            !float.IsFinite(position.Y))
        {
            throw new ArgumentOutOfRangeException(
                nameof(position));
        }

        ValidateColor(color, nameof(color));

        if (!float.IsFinite(radius) || radius <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(radius));
        }

        if (!float.IsFinite(intensity) || intensity < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intensity));
        }

        Position = position;
        Color = color;
        Radius = radius;
        Intensity = intensity;
    }

    private static void ValidateColor(
        Vector3 color,
        string parameterName)
    {
        if (!float.IsFinite(color.X) ||
            !float.IsFinite(color.Y) ||
            !float.IsFinite(color.Z) ||
            color.X < 0.0f || color.X > 1.0f ||
            color.Y < 0.0f || color.Y > 1.0f ||
            color.Z < 0.0f || color.Z > 1.0f)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Light color must contain finite linear RGB values between 0 and 1.");
        }
    }
}