using Engine.Core.Math;
using Engine.Particles;
using Engine.Particles.Particles;

namespace Engine.Tests.Particles;

public sealed class ParticleEffectSimulationTests
{
    [Fact]
    public void Burst_CreatesParticlesAtEmitterPosition()
    {
        var simulation = new ParticleEffectSimulation(
            CreateDefinition());

        var emitterPosition = new Vector2(10.0f, 20.0f);

        var spawned = simulation.Burst(
            3,
            emitterPosition);

        Assert.Equal(3, spawned);
        Assert.Equal(3, simulation.ParticleSystem.Count);

        foreach (var particle in simulation.ParticleSystem.Particles)
        {
            Assert.Equal(
                new Vector3(10.0f, 20.0f, 0.0f),
                particle.Position);
        }
    }

    [Fact]
    public void Update_EmitsParticlesAccordingToRate()
    {
        var simulation = new ParticleEffectSimulation(
            CreateDefinition(emissionRate: 10.0f));

        var spawned = simulation.Update(
            0.3f,
            Vector2.Zero);

        Assert.Equal(3, spawned);
        Assert.Equal(3, simulation.ParticleSystem.Count);
    }

    [Fact]
    public void Update_UsesConfiguredParticleRanges()
    {
        var simulation = new ParticleEffectSimulation(
            CreateDefinition());

        simulation.Burst(1, Vector2.Zero);

        var particle = Assert.Single(
            simulation.ParticleSystem.Particles.ToArray());

        Assert.InRange(particle.LifetimeSeconds, 1.0f, 2.0f);
        Assert.InRange(particle.Size, 4.0f, 8.0f);
        Assert.InRange(particle.Velocity.X, -5.0f, 5.0f);
        Assert.InRange(particle.Velocity.Y, 2.0f, 6.0f);
    }

    [Fact]
    public void SameSeed_ProducesSameParticles()
    {
        var first = new ParticleEffectSimulation(
            CreateDefinition(),
            seed: 123);

        var second = new ParticleEffectSimulation(
            CreateDefinition(),
            seed: 123);

        first.Burst(8, new Vector2(4.0f, 7.0f));
        second.Burst(8, new Vector2(4.0f, 7.0f));

        var firstParticles =
            first.ParticleSystem.Particles.ToArray();

        var secondParticles =
            second.ParticleSystem.Particles.ToArray();

        Assert.Equal(firstParticles.Length, secondParticles.Length);

        for (var i = 0; i < firstParticles.Length; i++)
        {
            Assert.Equal(
                firstParticles[i].Position,
                secondParticles[i].Position);

            Assert.Equal(
                firstParticles[i].Velocity,
                secondParticles[i].Velocity);

            Assert.Equal(
                firstParticles[i].LifetimeSeconds,
                secondParticles[i].LifetimeSeconds);

            Assert.Equal(
                firstParticles[i].Size,
                secondParticles[i].Size);
        }
    }

    [Fact]
    public void Burst_PointShape_SpawnsAtEmitterPosition()
    {
        var simulation = CreateSimulationWithShape(
            new PointParticleEmissionShapeDefinition());

        var emitterPosition = new Vector2(10.0f, 20.0f);

        var spawned = simulation.Burst(
            1,
            emitterPosition);

        Assert.Equal(1, spawned);
        Assert.Equal(1, simulation.ParticleSystem.Count);

        var particle =
            simulation.ParticleSystem.Particles[0];

        Assert.Equal(
            new Vector3(10.0f, 20.0f, 0.0f),
            particle.Position);
    }

    [Fact]
    public void Burst_CircleShape_SpawnsParticlesInsideCircle()
    {
        const float radius = 5.0f;
        const int particleCount = 16;

        var simulation = CreateSimulationWithShape(
            new CircleParticleEmissionShapeDefinition(radius));

        var emitterPosition = new Vector2(10.0f, 20.0f);

        var spawned = simulation.Burst(
            particleCount,
            emitterPosition);

        Assert.Equal(particleCount, spawned);

        var foundOffsetFromCenter = false;

        foreach (var particle in simulation.ParticleSystem.Particles)
        {
            var dx = particle.Position.X - emitterPosition.X;
            var dy = particle.Position.Y - emitterPosition.Y;

            var distanceSquared = dx * dx + dy * dy;

            Assert.True(
                distanceSquared <= radius * radius + 0.0001f,
                $"Particle is outside the circle. " +
                $"Distance squared: {distanceSquared}");

            if (distanceSquared > 0.0001f)
            {
                foundOffsetFromCenter = true;
            }
        }

        Assert.True(
            foundOffsetFromCenter,
            "Particles should be distributed around the center.");
    }

    [Fact]
    public void Burst_RectangleShape_SpawnsParticlesInsideRectangle()
    {
        const int particleCount = 16;

        var size = new Vector2(8.0f, 4.0f);

        var simulation = CreateSimulationWithShape(
            new RectangleParticleEmissionShapeDefinition(size));

        var emitterPosition = new Vector2(10.0f, 20.0f);

        var spawned = simulation.Burst(
            particleCount,
            emitterPosition);

        Assert.Equal(particleCount, spawned);

        var foundOffsetFromCenter = false;

        foreach (var particle in simulation.ParticleSystem.Particles)
        {
            var dx = particle.Position.X - emitterPosition.X;
            var dy = particle.Position.Y - emitterPosition.Y;

            Assert.InRange(
                dx,
                -size.X / 2.0f,
                size.X / 2.0f);

            Assert.InRange(
                dy,
                -size.Y / 2.0f,
                size.Y / 2.0f);

            if (MathF.Abs(dx) > 0.0001f ||
                MathF.Abs(dy) > 0.0001f)
            {
                foundOffsetFromCenter = true;
            }
        }

        Assert.True(
            foundOffsetFromCenter,
            "Particles should be distributed around the center.");
    }

    [Fact]
    public void Update_AccelerationModifier_ChangesVelocityOverTime()
    {
        var simulation = CreateSimulationWithModifiers(
            new ParticleModifierDefinition[]
            {
            new AccelerationModifierDefinition(
                new Vector2(2.0f, -1.0f))
            },
            new ParticleVector2Range(
                Vector2.Zero,
                Vector2.Zero),
            new ParticleFloatRange(10.0f, 10.0f),
            new ParticleFloatRange(1.0f, 1.0f));

        Assert.Equal(1, simulation.Burst(1, Vector2.Zero));

        simulation.Update(0.5f, Vector2.Zero);

        var particle = simulation.ParticleSystem.Particles[0];

        Assert.Equal(
            new Vector3(1.0f, -0.5f, 0.0f),
            particle.Velocity);
    }

    [Fact]
    public void Update_DragModifier_ReducesVelocity()
    {
        var simulation = CreateSimulationWithModifiers(
            new ParticleModifierDefinition[]
            {
            new DragModifierDefinition(2.0f)
            },
            new ParticleVector2Range(
                new Vector2(10.0f, 0.0f),
                new Vector2(10.0f, 0.0f)),
            new ParticleFloatRange(10.0f, 10.0f),
            new ParticleFloatRange(1.0f, 1.0f));

        Assert.Equal(1, simulation.Burst(1, Vector2.Zero));

        simulation.Update(0.5f, Vector2.Zero);

        var particle = simulation.ParticleSystem.Particles[0];

        // Drag uses exponential decay:
        // velocity *= exp(-coefficient * deltaSeconds).
        var expectedVelocity = 10.0f * MathF.Exp(-1.0f);

        Assert.InRange(
            particle.Velocity.X,
            expectedVelocity - 0.0001f,
            expectedVelocity + 0.0001f);

        Assert.Equal(0.0f, particle.Velocity.Y);
    }

    [Fact]
    public void Update_SizeOverLifetimeModifier_ChangesParticleSize()
    {
        var simulation = CreateSimulationWithModifiers(
            new ParticleModifierDefinition[]
            {
            new SizeOverLifetimeModifierDefinition(
                1.0f,
                3.0f)
            },
            new ParticleVector2Range(
                Vector2.Zero,
                Vector2.Zero),
            new ParticleFloatRange(2.0f, 2.0f),
            new ParticleFloatRange(4.0f, 4.0f));

        Assert.Equal(1, simulation.Burst(1, Vector2.Zero));

        simulation.Update(0.5f, Vector2.Zero);

        var particle = simulation.ParticleSystem.Particles[0];

        // At 25% of its lifetime:
        // multiplier = 1 + (3 - 1) * 0.25 = 1.5
        // size = 4 * 1.5 = 6
        Assert.Equal(6.0f, particle.Size, precision: 4);
    }

    [Fact]
    public void Reset_ClearsParticlesAndRestartsRandomSequence()
    {
        var simulation = new ParticleEffectSimulation(
            CreateDefinition(),
            seed: 123);

        simulation.Burst(1, Vector2.Zero);

        var firstParticle =
            simulation.ParticleSystem.Particles[0];

        simulation.Reset();

        Assert.Equal(0, simulation.ParticleSystem.Count);

        simulation.Burst(1, Vector2.Zero);

        var resetParticle =
            simulation.ParticleSystem.Particles[0];

        Assert.Equal(firstParticle.Velocity, resetParticle.Velocity);
        Assert.Equal(
            firstParticle.LifetimeSeconds,
            resetParticle.LifetimeSeconds);
        Assert.Equal(firstParticle.Size, resetParticle.Size);
    }

    private static ParticleEffectSimulation CreateSimulationWithModifiers(
    IReadOnlyList<ParticleModifierDefinition> modifiers,
    ParticleVector2Range velocity,
    ParticleFloatRange lifetime,
    ParticleFloatRange size)
    {
        var definition = new ParticleEffectDefinition(
            ParticleEffectDefinition.CurrentFormatVersion,
            "Textures/particle.png",
            0,
            new ParticleEmitterDefinition(
                0.0f,
                32,
                new PointParticleEmissionShapeDefinition()),
            new ParticleSpawnDefinition(
                lifetime,
                size,
                velocity,
                modifiers));

        return new ParticleEffectSimulation(
            definition,
            seed: 123);
    }

    private static ParticleEffectSimulation CreateSimulationWithShape(
    ParticleEmissionShapeDefinition shape)
    {
        var definition = new ParticleEffectDefinition(
            ParticleEffectDefinition.CurrentFormatVersion,
            "Textures/particle.png",
            0,
            new ParticleEmitterDefinition(
                0.0f,
                32,
                shape),
            new ParticleSpawnDefinition(
                new ParticleFloatRange(1.0f, 1.0f),
                new ParticleFloatRange(1.0f, 1.0f),
                new ParticleVector2Range(
                    Vector2.Zero,
                    Vector2.Zero),
                Array.Empty<ParticleModifierDefinition>()));

        return new ParticleEffectSimulation(
            definition,
            seed: 123);
    }

    private static ParticleEffectDefinition CreateDefinition(
        float emissionRate = 0.0f)
    {
        return new ParticleEffectDefinition(
            ParticleEffectDefinition.CurrentFormatVersion,
            "Textures/particle.png",
            layer: 0,
            emitter: new ParticleEmitterDefinition(
                ratePerSecond: emissionRate,
                maxParticles: 32,
                shape: new PointParticleEmissionShapeDefinition()),
            particle: new ParticleSpawnDefinition(
                new ParticleFloatRange(1.0f, 2.0f),
                new ParticleFloatRange(4.0f, 8.0f),
                new ParticleVector2Range(
                    new Vector2(-5.0f, 2.0f),
                    new Vector2(5.0f, 6.0f))));
    }
}