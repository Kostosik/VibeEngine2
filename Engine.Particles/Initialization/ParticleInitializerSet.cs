using Engine.Core.Determinism;
using Engine.Particles.Particles;

namespace Engine.Particles.Initialization;

public sealed class ParticleInitializerSet
{
    private readonly List<IParticleInitializer> _initializers = [];

    public int Count => _initializers.Count;

    public void Add(IParticleInitializer initializer)
    {
        ArgumentNullException.ThrowIfNull(initializer);

        _initializers.Add(initializer);
    }

    public bool Remove(IParticleInitializer initializer)
    {
        ArgumentNullException.ThrowIfNull(initializer);

        return _initializers.Remove(initializer);
    }

    public void Clear()
    {
        _initializers.Clear();
    }

    public void Apply(
        ref Particle particle,
        ref DeterministicRandom random)
    {
        foreach (var initializer in _initializers)
        {
            initializer.Initialize(
                ref particle,
                ref random);
        }
    }
}