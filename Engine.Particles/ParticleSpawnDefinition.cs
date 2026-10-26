using Engine.Core.Math;

namespace Engine.Particles;

public sealed class ParticleSpawnDefinition
{
    private readonly IReadOnlyList<ParticleModifierDefinition>
        _modifiers;

    public ParticleSpawnDefinition(
        ParticleFloatRange lifetimeSeconds,
        ParticleFloatRange size,
        ParticleVector2Range velocity,
        IEnumerable<ParticleModifierDefinition>? modifiers = null)
    {
        ValidateLifetimeRange(lifetimeSeconds);
        ValidateSizeRange(size);
        ValidateVelocityRange(velocity);

        var items = modifiers?.ToArray()
            ?? Array.Empty<ParticleModifierDefinition>();

        if (items.Any(static modifier => modifier is null))
        {
            throw new ArgumentException(
                "Modifier list cannot contain null entries.",
                nameof(modifiers));
        }

        LifetimeSeconds = lifetimeSeconds;
        Size = size;
        Velocity = velocity;
        _modifiers = Array.AsReadOnly(items);
    }

    public ParticleFloatRange LifetimeSeconds { get; }

    public ParticleFloatRange Size { get; }

    public ParticleVector2Range Velocity { get; }

    public IReadOnlyList<ParticleModifierDefinition> Modifiers =>
        _modifiers;

    private static void ValidateLifetimeRange(
        ParticleFloatRange range)
    {
        if (!float.IsFinite(range.Min) ||
            !float.IsFinite(range.Max) ||
            range.Min <= 0.0f ||
            range.Max < range.Min)
        {
            throw new ArgumentOutOfRangeException(
                nameof(range),
                "Lifetime must be finite, positive and ordered.");
        }
    }

    private static void ValidateSizeRange(
        ParticleFloatRange range)
    {
        if (!float.IsFinite(range.Min) ||
            !float.IsFinite(range.Max) ||
            range.Min < 0.0f ||
            range.Max < range.Min)
        {
            throw new ArgumentOutOfRangeException(
                nameof(range),
                "Size must be finite, non-negative and ordered.");
        }
    }

    private static void ValidateVelocityRange(
        ParticleVector2Range range)
    {
        if (!float.IsFinite(range.Min.X) ||
            !float.IsFinite(range.Min.Y) ||
            !float.IsFinite(range.Max.X) ||
            !float.IsFinite(range.Max.Y) ||
            range.Max.X < range.Min.X ||
            range.Max.Y < range.Min.Y)
        {
            throw new ArgumentOutOfRangeException(
                nameof(range),
                "Velocity ranges must be finite and ordered.");
        }
    }
}