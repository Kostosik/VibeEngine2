using Engine.Core.Determinism;
using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Graphics2D.Particles;
using Engine.Particles.Emission;
using Engine.Particles.Initialization;
using Engine.Particles.Particles;

namespace Engine.Runtime.Particles;

public sealed class ParticleEmitterInstance
{
    private readonly World _ecsWorld;
    private readonly ParticleFactoryDelegate _factory;
    private readonly ParticleEmitterRuntime _emitterRuntime;

    private DeterministicRandom _random;
    private Vector3 _sourcePosition;
    private bool _active = true;

    internal ParticleEmitterInstance(
        World ecsWorld,
        EntityId sourceEntity,
        string texturePath,
        int layer,
        ParticleEmitter emitter,
        int maxParticles,
        ParticleFactoryDelegate factory,
        ulong seed,
        ParticleInitializerSet? initializers)
    {
        ArgumentNullException.ThrowIfNull(ecsWorld);
        ArgumentNullException.ThrowIfNull(factory);

        _ecsWorld = ecsWorld;
        SourceEntity = sourceEntity;
        TexturePath = texturePath;
        Layer = layer;
        _factory = factory;
        _random = new DeterministicRandom(seed);

        ParticleSystem = new ParticleSystem(maxParticles);

        _emitterRuntime = new ParticleEmitterRuntime(
            emitter,
            initializers);

        SyncSourcePositionOrThrow();
    }

    public EntityId SourceEntity { get; }

    public string TexturePath { get; }

    public int Layer { get; }

    public ParticleEmitter Emitter =>
        _emitterRuntime.Emitter;

    public ParticleSystem ParticleSystem { get; }

    public bool IsActive => _active;

    public int Burst(int count)
    {
        SyncSourcePositionOrThrow();

        return _emitterRuntime.Burst(
            ParticleSystem,
            count,
            ref _random,
            CreateParticle);
    }

    internal bool Update(float deltaSeconds)
    {
        if (!TrySyncSourcePosition())
        {
            return false;
        }

        ParticleSystem.Update(deltaSeconds);

        _emitterRuntime.Update(
            ParticleSystem,
            deltaSeconds,
            ref _random,
            CreateParticle);

        return true;
    }

    internal void Render(ParticleRenderer renderer)
    {
        if (!_active || ParticleSystem.Count == 0)
        {
            return;
        }

        renderer.Render(
            ParticleSystem,
            TexturePath,
            Layer);
    }

    internal void Deactivate()
    {
        if (!_active)
        {
            return;
        }

        _active = false;
        ParticleSystem.Clear();
    }

    private Particle CreateParticle(
        ref DeterministicRandom random)
    {
        var particle = _factory(ref random);

        // Позиция фабрики трактуется как локальное
        // смещение относительно исходной сущности.
        particle.Position = new Vector3(
            _sourcePosition.X + particle.Position.X,
            _sourcePosition.Y + particle.Position.Y,
            _sourcePosition.Z + particle.Position.Z);

        return particle;
    }

    private void SyncSourcePositionOrThrow()
    {
        if (!TrySyncSourcePosition())
        {
            throw new InvalidOperationException(
                $"Particle emitter source entity '{SourceEntity}' " +
                "no longer exists or has no WorldTransform2D.");
        }
    }

    private bool TrySyncSourcePosition()
    {
        if (!_active ||
            !_ecsWorld.Exists(SourceEntity) ||
            !_ecsWorld.Has<WorldTransform2D>(SourceEntity))
        {
            return false;
        }

        ref var transform =
            ref _ecsWorld.Get<WorldTransform2D>(
                SourceEntity);

        var position = transform.Position;

        _sourcePosition = new Vector3(
            position.X.ToFloat(),
            position.Y.ToFloat(),
            0.0f);

        return true;
    }
}