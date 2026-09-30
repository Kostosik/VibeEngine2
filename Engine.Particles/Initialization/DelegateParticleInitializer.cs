using Engine.Core.Determinism;
using Engine.Particles.Particles;

namespace Engine.Particles.Initialization;

public sealed class DelegateParticleInitializer : IParticleInitializer
{
    private readonly ParticleInitializerDelegate _initializer;

    public DelegateParticleInitializer(
        ParticleInitializerDelegate initializer)
    {
        ArgumentNullException.ThrowIfNull(initializer);

        _initializer = initializer;
    }

    public void Initialize(
        ref Particle particle,
        ref DeterministicRandom random)
    {
        _initializer(ref particle, ref random);
    }
}

public delegate void ParticleInitializerDelegate(
    ref Particle particle,
    ref DeterministicRandom random);