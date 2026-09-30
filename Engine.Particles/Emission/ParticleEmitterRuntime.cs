using Engine.Core.Determinism;
using Engine.Particles.Initialization;
using Engine.Particles.Particles;

namespace Engine.Particles.Emission;

public sealed class ParticleEmitterRuntime
{
    private readonly ParticleEmitter _emitter;
    private readonly ParticleInitializerSet _initializers;

    public ParticleEmitterRuntime(
        ParticleEmitter emitter,
        ParticleInitializerSet? initializers = null)
    {
        ArgumentNullException.ThrowIfNull(emitter);

        _emitter = emitter;
        _initializers = initializers ?? new ParticleInitializerSet();
    }

    public ParticleEmitter Emitter => _emitter;

    public ParticleInitializerSet Initializers =>
        _initializers;

    public int Update(
        ParticleSystem particleSystem,
        float deltaSeconds,
        ref DeterministicRandom random,
        ParticleFactoryDelegate factory)
    {
        ArgumentNullException.ThrowIfNull(particleSystem);
        ArgumentNullException.ThrowIfNull(factory);

        var requested = _emitter.Update(deltaSeconds);

        return Emit(
            particleSystem,
            requested,
            ref random,
            factory);
    }

    public int Burst(
        ParticleSystem particleSystem,
        int count,
        ref DeterministicRandom random,
        ParticleFactoryDelegate factory)
    {
        ArgumentNullException.ThrowIfNull(particleSystem);
        ArgumentNullException.ThrowIfNull(factory);

        var requested = _emitter.Burst(count);

        return Emit(
            particleSystem,
            requested,
            ref random,
            factory);
    }

    public void Reset()
    {
        _emitter.Reset();
    }

    private int Emit(
        ParticleSystem particleSystem,
        int requested,
        ref DeterministicRandom random,
        ParticleFactoryDelegate factory)
    {
        var spawned = 0;

        for (var i = 0; i < requested; i++)
        {
            var particle = factory(ref random);

            _initializers.Apply(
                ref particle,
                ref random);

            if (!particleSystem.Spawn(particle))
                break;

            spawned++;
        }

        return spawned;
    }
}