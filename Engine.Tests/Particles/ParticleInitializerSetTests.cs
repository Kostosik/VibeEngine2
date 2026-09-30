using Engine.Core.Determinism;
using Engine.Core.Math;
using Engine.Particles;
using Engine.Particles.Initialization;
using Engine.Particles.Particles;

namespace Engine.Tests.Particles;

public sealed class ParticleInitializerSetTests
{
    [Fact]
    public void AppliesInitializersInOrder()
    {
        var initializers = new ParticleInitializerSet();

        initializers.Add(
            new DelegateParticleInitializer(
                static (ref Particle particle, ref DeterministicRandom random) =>
                {
                    particle.Position = new Vector3(
                        1.0f,
                        2.0f,
                        3.0f);
                }));

        initializers.Add(
            new DelegateParticleInitializer(
                static (ref Particle particle, ref DeterministicRandom random) =>
                {
                    particle.Velocity = new Vector3(
                        4.0f,
                        5.0f,
                        6.0f);
                }));

        var particle = new Particle(
            Vector3.Zero,
            Vector3.Zero,
            1.0f);

        var random = new DeterministicRandom(123);

        initializers.Apply(
            ref particle,
            ref random);

        Assert.Equal(
            new Vector3(1.0f, 2.0f, 3.0f),
            particle.Position);

        Assert.Equal(
            new Vector3(4.0f, 5.0f, 6.0f),
            particle.Velocity);
    }

    [Fact]
    public void RemoveRemovesInitializer()
    {
        var initializers = new ParticleInitializerSet();

        var initializer =
            new DelegateParticleInitializer(
                static (ref Particle particle, ref DeterministicRandom random) =>
                {
                    particle.Size = 10.0f;
                });

        initializers.Add(initializer);

        Assert.True(initializers.Remove(initializer));
        Assert.Equal(0, initializers.Count);
    }

    [Fact]
    public void ClearRemovesAllInitializers()
    {
        var initializers = new ParticleInitializerSet();

        initializers.Add(
            new DelegateParticleInitializer(
                static (ref Particle particle, ref DeterministicRandom random) => { }));

        initializers.Clear();

        Assert.Equal(0, initializers.Count);
    }
}