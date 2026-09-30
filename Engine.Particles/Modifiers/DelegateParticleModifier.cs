using Engine.Particles.Particles;

namespace Engine.Particles.Modifiers;

public delegate void ParticleModifierDelegate(
    ref Particle particle,
    float deltaSeconds);

public sealed class DelegateParticleModifier : IParticleModifier
{
    private readonly ParticleModifierDelegate _modifier;

    public DelegateParticleModifier(
        ParticleModifierDelegate modifier)
    {
        ArgumentNullException.ThrowIfNull(modifier);

        _modifier = modifier;
    }

    public void Modify(
        ref Particle particle,
        float deltaSeconds)
    {
        _modifier(
            ref particle,
            deltaSeconds);
    }
}