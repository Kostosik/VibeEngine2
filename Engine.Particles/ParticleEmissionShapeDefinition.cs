using Engine.Core.Math;

namespace Engine.Particles;

public abstract record ParticleEmissionShapeDefinition;

public sealed record PointParticleEmissionShapeDefinition
    : ParticleEmissionShapeDefinition;

public sealed record CircleParticleEmissionShapeDefinition
    : ParticleEmissionShapeDefinition
{
    public CircleParticleEmissionShapeDefinition(float radius)
    {
        if (!float.IsFinite(radius) || radius < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(radius),
                "Circle radius must be finite and non-negative.");
        }

        Radius = radius;
    }

    public float Radius { get; }
}

public sealed record RectangleParticleEmissionShapeDefinition
    : ParticleEmissionShapeDefinition
{
    public RectangleParticleEmissionShapeDefinition(Vector2 size)
    {
        if (!float.IsFinite(size.X) ||
            !float.IsFinite(size.Y) ||
            size.X < 0.0f ||
            size.Y < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                "Rectangle size must be finite and non-negative.");
        }

        Size = size;
    }

    public Vector2 Size { get; }
}