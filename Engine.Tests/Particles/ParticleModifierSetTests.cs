using Engine.Core.Math;
using Engine.Particles;
using Engine.Particles.Modifiers;
using Engine.Particles.Particles;

namespace Engine.Tests.Particles;

public sealed class ParticleModifierSetTests
{
    [Fact]
    public void AppliesModifiersInOrder()
    {
        var modifiers = new ParticleModifierSet();

        modifiers.Add(
            new DelegateParticleModifier(
                static (ref Particle particle, float deltaSeconds) =>
                {
                    particle.Size += 1.0f;
                }));

        modifiers.Add(
            new DelegateParticleModifier(
                static (ref Particle particle, float deltaSeconds) =>
                {
                    particle.Size *= 2.0f;
                }));

        var particle = new Particle(
            Vector3.Zero,
            Vector3.Zero,
            5.0f);

        modifiers.Apply(
            ref particle,
            0.1f);

        Assert.Equal(4.0f, particle.Size);
    }

    [Fact]
    public void ModifierCanChangeParticleState()
    {
        var system = new ParticleSystem(1);

        system.Modifiers.Add(
            new DelegateParticleModifier(
                static (ref Particle particle, float deltaSeconds) =>
                {
                    particle.Velocity = new Vector3(
                        10.0f,
                        particle.Velocity.Y,
                        particle.Velocity.Z);
                }));

        system.Spawn(
            new Particle(
                Vector3.Zero,
                Vector3.Zero,
                5.0f));

        system.Update(1.0f);

        Assert.Equal(
            new Vector3(10.0f, 0.0f, 0.0f),
            system.Particles[0].Velocity);
    }

    [Fact]
    public void RemoveRemovesModifier()
    {
        var modifiers = new ParticleModifierSet();

        var modifier =
            new DelegateParticleModifier(
                static (ref Particle particle, float deltaSeconds) => { });

        modifiers.Add(modifier);

        Assert.True(modifiers.Remove(modifier));
        Assert.Equal(0, modifiers.Count);
    }
}