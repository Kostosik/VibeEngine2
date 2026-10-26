using Engine.Core.Assets;
using Engine.Core.Math;

namespace Engine.Particles;

public sealed class ParticleEffectDefinition
{
    public const int CurrentFormatVersion = 2;

    public ParticleEffectDefinition(
        int formatVersion,
        string texturePath,
        int layer,
        ParticleEmitterDefinition emitter,
        ParticleSpawnDefinition particle)
    {
        if (formatVersion != CurrentFormatVersion)
        {
            throw new ArgumentOutOfRangeException(
                nameof(formatVersion),
                $"Unsupported particle effect format version " +
                $"'{formatVersion}'.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(texturePath);
        ArgumentNullException.ThrowIfNull(emitter);
        ArgumentNullException.ThrowIfNull(particle);

        TexturePath = new AssetPath(texturePath).Value;

        if (string.IsNullOrWhiteSpace(TexturePath))
        {
            throw new ArgumentException(
                "Texture path must identify an asset.",
                nameof(texturePath));
        }

        FormatVersion = formatVersion;
        Layer = layer;
        Emitter = emitter;
        Particle = particle;
    }

    // Временный переходный конструктор для существующего runtime
    // и редактора. Удалим его после перехода на новую модель.
    public ParticleEffectDefinition(
        int formatVersion,
        string texturePath,
        float emissionRatePerSecond,
        int maxParticles,
        int layer,
        ParticleFloatRange lifetimeSeconds,
        ParticleFloatRange size,
        ParticleVector2Range velocity)
        : this(
            formatVersion,
            texturePath,
            layer,
            new ParticleEmitterDefinition(
                emissionRatePerSecond,
                maxParticles,
                new PointParticleEmissionShapeDefinition()),
            new ParticleSpawnDefinition(
                lifetimeSeconds,
                size,
                velocity))
    {
    }

    public int FormatVersion { get; }

    public string TexturePath { get; }

    public int Layer { get; }

    public ParticleEmitterDefinition Emitter { get; }

    public ParticleSpawnDefinition Particle { get; }

    // Переходные свойства: текущий runtime пока их использует.
    public float EmissionRatePerSecond =>
        Emitter.RatePerSecond;

    public int MaxParticles =>
        Emitter.MaxParticles;

    public ParticleFloatRange LifetimeSeconds =>
        Particle.LifetimeSeconds;

    public ParticleFloatRange Size =>
        Particle.Size;

    public ParticleVector2Range Velocity =>
        Particle.Velocity;
}

public readonly record struct ParticleFloatRange(
    float Min,
    float Max);

public readonly record struct ParticleVector2Range(
    Vector2 Min,
    Vector2 Max);