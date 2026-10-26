using Engine.Core.Determinism;
using Engine.Core.Math;
using Engine.Particles.Emission;
using Engine.Particles.Modifiers;
using Engine.Particles.Particles;

namespace Engine.Particles;

/// <summary>
/// Общая симуляция ресурсного эффекта частиц.
/// Не зависит от ECS, графики или редактора.
/// </summary>
public sealed class ParticleEffectSimulation
{
    private readonly ulong _initialSeed;
    private readonly ParticleEffectDefinition _definition;
    private readonly ParticleEmitter _emitter;
    private readonly ParticleEmitterRuntime _emitterRuntime;

    private DeterministicRandom _random;

    public ParticleEffectSimulation(
        ParticleEffectDefinition definition,
        ulong seed = 1)
    {
        ArgumentNullException.ThrowIfNull(definition);

        _definition = definition;

        ParticleSystem = new ParticleSystem(
            definition.Emitter.MaxParticles);
        ConfigureModifiers(definition.Particle.Modifiers);
        _emitter = new ParticleEmitter(
            definition.Emitter.RatePerSecond);

        _emitterRuntime = new ParticleEmitterRuntime(
            _emitter);

        _initialSeed = seed;
        _random = new DeterministicRandom(_initialSeed);
    }

    public ParticleSystem ParticleSystem { get; }

    public ParticleEmitter Emitter => _emitter;

    public int Update(
        float deltaSeconds,
        Vector2 emitterPosition)
    {
        ValidatePosition(emitterPosition);

        ParticleSystem.Update(deltaSeconds);

        return _emitterRuntime.Update(
            ParticleSystem,
            deltaSeconds,
            ref _random,
            CreateFactory(emitterPosition));
    }

    public int Burst(
        int count,
        Vector2 emitterPosition)
    {
        ValidatePosition(emitterPosition);

        return _emitterRuntime.Burst(
            ParticleSystem,
            count,
            ref _random,
            CreateFactory(emitterPosition));
    }

    public void Reset()
    {
        ParticleSystem.Clear();
        _emitterRuntime.Reset();
        _random = new DeterministicRandom(_initialSeed);
    }

    private ParticleFactoryDelegate CreateFactory(
        Vector2 emitterPosition)
    {
        return (ref DeterministicRandom random) =>
        {
            var lifetime = random.NextFloat(
                _definition.Particle.LifetimeSeconds.Min,
                _definition.Particle.LifetimeSeconds.Max);

            var size = random.NextFloat(
                _definition.Particle.Size.Min,
                _definition.Particle.Size.Max);

            var velocityX = random.NextFloat(
                _definition.Particle.Velocity.Min.X,
                _definition.Particle.Velocity.Max.X);

            var velocityY = random.NextFloat(
                _definition.Particle.Velocity.Min.Y,
                _definition.Particle.Velocity.Max.Y);

            var spawnPosition = SampleSpawnPosition(
                ref random,
                emitterPosition);

            return new Particle(
                new Vector3(
                    spawnPosition.X,
                    spawnPosition.Y,
                    0.0f),
                new Vector3(velocityX, velocityY, 0.0f),
                lifetime,
                size);
        };
    }

    private void ConfigureModifiers(
    IReadOnlyList<ParticleModifierDefinition> definitions)
    {
        foreach (var definition in definitions)
        {
            switch (definition)
            {
                case AccelerationModifierDefinition acceleration:
                    ParticleSystem.Modifiers.Add(
                        new DelegateParticleModifier(
                            (ref Particle particle, float deltaSeconds) =>
                            {
                                particle.Velocity = new Vector3(
                                    particle.Velocity.X +
                                        acceleration.Acceleration.X * deltaSeconds,
                                    particle.Velocity.Y +
                                        acceleration.Acceleration.Y * deltaSeconds,
                                    particle.Velocity.Z);
                            }));
                    break;

                case DragModifierDefinition drag:
                    ParticleSystem.Modifiers.Add(
                        new DelegateParticleModifier(
                            (ref Particle particle, float deltaSeconds) =>
                            {
                                var damping = MathF.Exp(
                                    -drag.Coefficient * deltaSeconds);

                                particle.Velocity = new Vector3(
                                    particle.Velocity.X * damping,
                                    particle.Velocity.Y * damping,
                                    particle.Velocity.Z * damping);
                            }));
                    break;

                case SizeOverLifetimeModifierDefinition size:
                    ParticleSystem.Modifiers.Add(
                        new DelegateParticleModifier(
                            (ref Particle particle, float deltaSeconds) =>
                            {
                                var normalizedAge =
                                    particle.NormalizedAge;

                                var multiplier =
                                    size.StartMultiplier +
                                    (size.EndMultiplier -
                                     size.StartMultiplier) *
                                    normalizedAge;

                                particle.Size =
                                    particle.InitialSize * multiplier;
                            }));
                    break;

                default:
                    throw new NotSupportedException(
                        $"Unsupported particle modifier: " +
                        $"'{definition.GetType().Name}'.");
            }
        }
    }

    private Vector2 SampleSpawnPosition(
    ref DeterministicRandom random,
    Vector2 emitterPosition)
    {
        var offset = _definition.Emitter.Shape switch
        {
            PointParticleEmissionShapeDefinition =>
                Vector2.Zero,

            CircleParticleEmissionShapeDefinition circle =>
                SampleCircleOffset(
                    ref random,
                    circle.Radius),

            RectangleParticleEmissionShapeDefinition rectangle =>
                SampleRectangleOffset(
                    ref random,
                    rectangle.Size),

            var shape =>
                throw new NotSupportedException(
                    $"Unsupported particle emission shape: " +
                    $"'{shape.GetType().Name}'.")
        };

        return emitterPosition + offset;
    }

    private static Vector2 SampleCircleOffset(
        ref DeterministicRandom random,
        float radius)
    {
        if (radius == 0.0f)
            return Vector2.Zero;

        // Распределяем частицы по площади круга,
        // а не только по его окружности.
        var angle = random.NextFloat(
            0.0f,
            2.0f * MathF.PI);

        var distance =
            radius * MathF.Sqrt(random.NextFloat());

        return new Vector2(
            MathF.Cos(angle) * distance,
            MathF.Sin(angle) * distance);
    }

    private static Vector2 SampleRectangleOffset(
        ref DeterministicRandom random,
        Vector2 size)
    {
        return new Vector2(
            random.NextFloat(
                -size.X * 0.5f,
                size.X * 0.5f),
            random.NextFloat(
                -size.Y * 0.5f,
                size.Y * 0.5f));
    }

    private static void ValidatePosition(Vector2 position)
    {
        if (!float.IsFinite(position.X) ||
            !float.IsFinite(position.Y))
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                "Emitter position must contain finite values.");
        }
    }
}