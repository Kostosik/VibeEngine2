using Engine.Camera;
using Engine.Content;
using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Graphics;
using Engine.Graphics.Rendering;
using Engine.Graphics2D.Particles;
using Engine.Particles.Emission;
using Engine.Particles.Initialization;

namespace Engine.Runtime.Particles;

public sealed class ParticleRuntime : IDisposable
{
    private readonly World _ecsWorld;
    private readonly ParticleRenderer _renderer;
    private readonly List<ParticleEmitterInstance> _emitters = [];

    private bool _disposed;

    public ParticleRuntime(
        World ecsWorld,
        IGraphicsDevice graphics,
        IContentManager content,
        Camera2D camera)
    {
        ArgumentNullException.ThrowIfNull(ecsWorld);

        _ecsWorld = ecsWorld;

        _renderer = new ParticleRenderer(
            graphics,
            content,
            camera);

        _ecsWorld.EntityDestroyed += OnEntityDestroyed;
    }

    public int EmitterCount => _emitters.Count;

    public ParticleEmitterInstance CreateEmitter(
        EntityId sourceEntity,
        string texturePath,
        ParticleEmitter emitter,
        int maxParticles,
        ParticleFactoryDelegate factory,
        ulong seed = 1,
        ParticleInitializerSet? initializers = null,
        int layer = RenderLayers.World)
    {
        EnsureNotDisposed();

        ArgumentException.ThrowIfNullOrWhiteSpace(
            texturePath);

        ArgumentNullException.ThrowIfNull(emitter);
        ArgumentNullException.ThrowIfNull(factory);

        if (!_ecsWorld.Exists(sourceEntity))
        {
            throw new ArgumentException(
                "Source entity does not exist.",
                nameof(sourceEntity));
        }

        if (!_ecsWorld.Has<WorldTransform2D>(sourceEntity))
        {
            throw new ArgumentException(
                "Source entity requires WorldTransform2D.",
                nameof(sourceEntity));
        }

        var instance = new ParticleEmitterInstance(
            _ecsWorld,
            sourceEntity,
            texturePath,
            layer,
            emitter,
            maxParticles,
            factory,
            seed,
            initializers);

        _emitters.Add(instance);

        return instance;
    }

    public bool RemoveEmitter(
        ParticleEmitterInstance instance)
    {
        EnsureNotDisposed();

        ArgumentNullException.ThrowIfNull(instance);

        if (!_emitters.Remove(instance))
        {
            return false;
        }

        instance.Deactivate();

        return true;
    }

    public void Update(float deltaSeconds)
    {
        EnsureNotDisposed();

        if (!float.IsFinite(deltaSeconds) ||
            deltaSeconds < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deltaSeconds));
        }

        for (var i = _emitters.Count - 1; i >= 0; i--)
        {
            if (_emitters[i].Update(deltaSeconds))
            {
                continue;
            }

            _emitters[i].Deactivate();
            _emitters.RemoveAt(i);
        }
    }

    public void Render()
    {
        EnsureNotDisposed();

        foreach (var emitter in _emitters)
        {
            emitter.Render(_renderer);
        }
    }

    private void OnEntityDestroyed(EntityId entity)
    {
        for (var i = _emitters.Count - 1; i >= 0; i--)
        {
            if (_emitters[i].SourceEntity != entity)
            {
                continue;
            }

            _emitters[i].Deactivate();
            _emitters.RemoveAt(i);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _ecsWorld.EntityDestroyed -= OnEntityDestroyed;

        foreach (var emitter in _emitters)
        {
            emitter.Deactivate();
        }

        _emitters.Clear();

        _renderer.Dispose();

        _disposed = true;
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}