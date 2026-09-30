using Engine.Core.Determinism;
using Engine.Core.Math;
using Engine.Particles.Emission;
using Engine.Particles.Initialization;
using Engine.Particles.Particles;

namespace Engine.Tests.Particles;

public sealed class ParticleEmitterRuntimeTests
{
    [Fact]
    public void UpdateCreatesAndInitializesParticles()
    {
        var emitter = new ParticleEmitter(10.0f);

        var initializers = new ParticleInitializerSet();

        initializers.Add(
            new DelegateParticleInitializer(
                static (
                    ref Particle particle,
                    ref DeterministicRandom random) =>
                {
                    particle.Velocity =
                        new Vector3(1.0f, 2.0f, 3.0f);
                }));

        var runtime = new ParticleEmitterRuntime(
            emitter,
            initializers);

        var particleSystem = new ParticleSystem(10);
        var random = new DeterministicRandom(123);

        var spawned = runtime.Update(
            particleSystem,
            0.5f,
            ref random,
            static (ref DeterministicRandom _) =>
                new Particle(
                    Vector3.Zero,
                    Vector3.Zero,
                    5.0f));

        Assert.Equal(5, spawned);
        Assert.Equal(5, particleSystem.Count);

        foreach (var particle in particleSystem.Particles)
        {
            Assert.Equal(
                new Vector3(1.0f, 2.0f, 3.0f),
                particle.Velocity);
        }
    }

    [Fact]
    public void BurstCreatesRequestedParticles()
    {
        var runtime = new ParticleEmitterRuntime(
            new ParticleEmitter());

        var particleSystem = new ParticleSystem(10);
        var random = new DeterministicRandom(123);

        var spawned = runtime.Burst(
            particleSystem,
            4,
            ref random,
            static (ref DeterministicRandom _) =>
                new Particle(
                    Vector3.Zero,
                    Vector3.Zero,
                    5.0f));

        Assert.Equal(4, spawned);
        Assert.Equal(4, particleSystem.Count);
    }

    [Fact]
    public void RuntimeStopsWhenParticleSystemIsFull()
    {
        var runtime = new ParticleEmitterRuntime(
            new ParticleEmitter());

        var particleSystem = new ParticleSystem(2);
        var random = new DeterministicRandom(123);

        var spawned = runtime.Burst(
            particleSystem,
            5,
            ref random,
            static (ref DeterministicRandom _) =>
                new Particle(
                    Vector3.Zero,
                    Vector3.Zero,
                    5.0f));

        Assert.Equal(2, spawned);
        Assert.Equal(2, particleSystem.Count);
    }

    [Fact]
    public void ResetResetsEmitterState()
    {
        var runtime = new ParticleEmitterRuntime(
            new ParticleEmitter(10.0f));

        var particleSystem = new ParticleSystem(10);
        var random = new DeterministicRandom(123);

        runtime.Update(
            particleSystem,
            0.05f,
            ref random,
            static (ref DeterministicRandom _) =>
                new Particle(
                    Vector3.Zero,
                    Vector3.Zero,
                    5.0f));

        runtime.Reset();

        var spawned = runtime.Update(
            particleSystem,
            0.05f,
            ref random,
            static (ref DeterministicRandom _) =>
                new Particle(
                    Vector3.Zero,
                    Vector3.Zero,
                    5.0f));

        Assert.Equal(0, spawned);
    }
}