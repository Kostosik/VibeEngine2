using Engine.Particles.Particles;

namespace Engine.Particles.Modifiers;

public sealed class ParticleModifierSet
{
    private readonly List<IParticleModifier> _modifiers = [];

    public int Count => _modifiers.Count;

    public void Add(IParticleModifier modifier)
    {
        ArgumentNullException.ThrowIfNull(modifier);

        _modifiers.Add(modifier);
    }

    public bool Remove(IParticleModifier modifier)
    {
        ArgumentNullException.ThrowIfNull(modifier);

        return _modifiers.Remove(modifier);
    }

    public void Clear()
    {
        _modifiers.Clear();
    }

    public void Apply(
        ref Particle particle,
        float deltaSeconds)
    {
        foreach (var modifier in _modifiers)
        {
            modifier.Modify(
                ref particle,
                deltaSeconds);
        }
    }
}