using Engine.Core.Math;
using Engine.Particles.Particles;

namespace Engine.Tests.Particles;

public sealed class ParticleSystemTests
{
    [Fact]
    public void SpawnsParticle()
    {
        var system = new ParticleSystem(4);

        var particle = new Particle(
            new Vector3(1.0f, 2.0f, 3.0f),
            new Vector3(4.0f, 5.0f, 6.0f),
            2.0f);

        Assert.True(system.Spawn(particle));

        Assert.Equal(1, system.Count);
        Assert.Equal(
            particle,
            system.Particles[0]);
    }

    [Fact]
    public void SpawnFailsWhenCapacityIsReached()
    {
        var system = new ParticleSystem(1);

        var particle = new Particle(
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 0.0f),
            1.0f);

        Assert.True(system.Spawn(particle));
        Assert.False(system.Spawn(particle));
        Assert.Equal(1, system.Count);
    }

    [Fact]
    public void UpdateIntegratesPositionAndAge()
    {
        var system = new ParticleSystem(1);

        system.Spawn(new Particle(
            new Vector3(1.0f, 2.0f, 3.0f),
            new Vector3(2.0f, 4.0f, 6.0f),
            5.0f));

        system.Update(0.5f);

        var particle = system.Particles[0];

        Assert.Equal(
            new Vector3(2.0f, 4.0f, 6.0f),
            particle.Position);

        Assert.Equal(0.5f, particle.AgeSeconds);
    }

    [Fact]
    public void RemovesExpiredParticles()
    {
        var system = new ParticleSystem(2);

        system.Spawn(new Particle(
            Vector3.Zero,
            Vector3.Zero,
            1.0f));

        system.Update(1.0f);

        Assert.Equal(0, system.Particles.Length);
        Assert.Equal(0, system.Count);
    }

    [Fact]
    public void UpdateRemovesDeadParticlesWithoutLeavingGaps()
    {
        var system = new ParticleSystem(3);

        system.Spawn(new Particle(
            new Vector3(1.0f, 0.0f, 0.0f),
            Vector3.Zero,
            1.0f));

        system.Spawn(new Particle(
            new Vector3(2.0f, 0.0f, 0.0f),
            Vector3.Zero,
            5.0f));

        system.Update(1.0f);

        Assert.Equal(1, system.Count);
        Assert.Equal(
            new Vector3(2.0f, 0.0f, 0.0f),
            system.Particles[0].Position);
    }

    [Fact]
    public void ClearRemovesAllParticles()
    {
        var system = new ParticleSystem(4);

        system.Spawn(new Particle(
            Vector3.Zero,
            Vector3.Zero,
            1.0f));

        system.Clear();

        Assert.Equal(0, system.Count);
        Assert.Equal(0, system.Particles.Length);
    }
}