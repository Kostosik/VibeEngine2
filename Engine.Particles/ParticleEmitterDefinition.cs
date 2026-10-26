namespace Engine.Particles;

public sealed class ParticleEmitterDefinition
{
    public ParticleEmitterDefinition(
        float ratePerSecond,
        int maxParticles,
        ParticleEmissionShapeDefinition shape)
    {
        if (!float.IsFinite(ratePerSecond) ||
            ratePerSecond < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ratePerSecond));
        }

        if (maxParticles <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxParticles));
        }

        ArgumentNullException.ThrowIfNull(shape);

        RatePerSecond = ratePerSecond;
        MaxParticles = maxParticles;
        Shape = shape;
    }

    public float RatePerSecond { get; }

    public int MaxParticles { get; }

    public ParticleEmissionShapeDefinition Shape { get; }
}