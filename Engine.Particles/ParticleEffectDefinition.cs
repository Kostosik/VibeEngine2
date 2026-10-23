using Engine.Core.Assets;
using Engine.Core.Math;

namespace Engine.Particles;

public sealed class ParticleEffectDefinition
{
    public const int CurrentFormatVersion = 1;

    public ParticleEffectDefinition(
        int formatVersion,
        string texturePath,
        float emissionRatePerSecond,
        int maxParticles,
        int layer,
        ParticleFloatRange lifetimeSeconds,
        ParticleFloatRange size,
        ParticleVector2Range velocity)
    {
        if (formatVersion != CurrentFormatVersion)
        {
            throw new ArgumentOutOfRangeException(
                nameof(formatVersion),
                $"Unsupported particle effect format version " +
                $"'{formatVersion}'.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            texturePath);

        TexturePath = new AssetPath(texturePath).Value;

        if (string.IsNullOrWhiteSpace(TexturePath))
        {
            throw new ArgumentException(
                "Texture path must identify an asset.",
                nameof(texturePath));
        }

        if (!float.IsFinite(emissionRatePerSecond) ||
            emissionRatePerSecond < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(emissionRatePerSecond));
        }

        if (maxParticles <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxParticles));
        }

        ValidateLifetimeRange(lifetimeSeconds);
        ValidateSizeRange(size);
        ValidateVelocityRange(velocity);

        FormatVersion = formatVersion;
        EmissionRatePerSecond = emissionRatePerSecond;
        MaxParticles = maxParticles;
        Layer = layer;
        LifetimeSeconds = lifetimeSeconds;
        Size = size;
        Velocity = velocity;
    }

    public int FormatVersion { get; }

    public string TexturePath { get; }

    public float EmissionRatePerSecond { get; }

    public int MaxParticles { get; }

    public int Layer { get; }

    public ParticleFloatRange LifetimeSeconds { get; }

    public ParticleFloatRange Size { get; }

    public ParticleVector2Range Velocity { get; }

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
                "Lifetime values must be finite, positive, " +
                "and ordered from minimum to maximum.");
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
                "Size values must be finite, non-negative, " +
                "and ordered from minimum to maximum.");
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
                "Velocity values must be finite and ordered " +
                "from minimum to maximum on both axes.");
        }
    }
}

public readonly record struct ParticleFloatRange(
    float Min,
    float Max);

public readonly record struct ParticleVector2Range(
    Vector2 Min,
    Vector2 Max);