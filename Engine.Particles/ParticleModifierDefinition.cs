using Engine.Core.Math;

namespace Engine.Particles;

public abstract record ParticleModifierDefinition;

public sealed record AccelerationModifierDefinition
    : ParticleModifierDefinition
{
    public AccelerationModifierDefinition(Vector2 acceleration)
    {
        if (!float.IsFinite(acceleration.X) ||
            !float.IsFinite(acceleration.Y))
        {
            throw new ArgumentOutOfRangeException(
                nameof(acceleration),
                "Acceleration must contain finite values.");
        }

        Acceleration = acceleration;
    }

    public Vector2 Acceleration { get; }
}

public sealed record DragModifierDefinition
    : ParticleModifierDefinition
{
    public DragModifierDefinition(float coefficient)
    {
        if (!float.IsFinite(coefficient) || coefficient < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(coefficient),
                "Drag coefficient must be finite and non-negative.");
        }

        Coefficient = coefficient;
    }

    public float Coefficient { get; }
}

public sealed record SizeOverLifetimeModifierDefinition
    : ParticleModifierDefinition
{
    public SizeOverLifetimeModifierDefinition(
        float startMultiplier,
        float endMultiplier)
    {
        if (!float.IsFinite(startMultiplier) ||
            startMultiplier < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startMultiplier));
        }

        if (!float.IsFinite(endMultiplier) ||
            endMultiplier < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endMultiplier));
        }

        StartMultiplier = startMultiplier;
        EndMultiplier = endMultiplier;
    }

    public float StartMultiplier { get; }

    public float EndMultiplier { get; }
}