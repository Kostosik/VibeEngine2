using Engine.Camera;
using Engine.Content;
using Engine.Core.Assets;
using Engine.Core.Determinism;
using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Graphics;
using Engine.Graphics.Rendering;
using Engine.Graphics2D.Particles;
using Engine.Particles;
using Engine.Particles.Particles;
using Engine.Particles.Emission;
using Engine.Particles.Initialization;

namespace Engine.Runtime.Particles;

public sealed class ParticleRuntime : IDisposable
{
    private readonly World _ecsWorld;
    private readonly ParticleRenderer _renderer;
    private readonly List<ParticleEmitterInstance> _emitters = [];

    private readonly IContentManager _content;

    private readonly Dictionary<EntityId, ComponentEmitterBinding>
        _componentEmitters = new();

    private bool _disposed;

    public ParticleRuntime(
        World ecsWorld,
        IGraphicsDevice graphics,
        IContentManager content,
        Camera2D camera)
    {
        ArgumentNullException.ThrowIfNull(content);
        
        ArgumentNullException.ThrowIfNull(ecsWorld);
        _content = content;
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
        SynchronizeComponentEmitters();
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
        RemoveComponentEmitter(entity);
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

        foreach (var binding in _componentEmitters.Values)
        {
            _content.Unload<ParticleEffectDefinition>(
                binding.EffectPath);
        }

        _componentEmitters.Clear();

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

    private void SynchronizeComponentEmitters()
    {
        var entitiesWithComponent = new HashSet<EntityId>();

        var query = _ecsWorld
            .Query<ParticleEffectComponent>()
            .GetEnumerator();

        while (query.MoveNext())
        {
            var item = query.Current;
            var entity = item.Entity;
            var component = item.Component;

            entitiesWithComponent.Add(entity);

            if (component.InitialBurstCount < 0)
            {
                throw new InvalidOperationException(
                    $"Particle effect on entity '{entity}' has a negative initial burst count.");
            }

            // Пустой путь означает, что ресурс ещё не назначен.
            if (string.IsNullOrWhiteSpace(component.EffectPath))
            {
                RemoveComponentEmitter(entity);
                continue;
            }

            if (!_ecsWorld.Has<WorldTransform2D>(entity))
            {
                RemoveComponentEmitter(entity);

                throw new InvalidOperationException(
                    $"Entity '{entity}' has ParticleEffectComponent " +
                    "but lacks WorldTransform2D.");
            }

            var effectPath = new AssetPath(component.EffectPath);

            if (_componentEmitters.TryGetValue(
                    entity,
                    out var existing) &&
                existing.EffectPath == effectPath &&
                existing.InitialBurstCount == component.InitialBurstCount)
            {
                continue;
            }

            // Если настройки изменились, заменяем старый экземпляр.
            RemoveComponentEmitter(entity);

            var definition =
                _content.Load<ParticleEffectDefinition>(effectPath);

            ParticleEmitterInstance? instance = null;

            try
            {
                instance = CreateEffectEmitter(
                    entity,
                    definition,
                    CreateParticleSeed(entity));

                if (component.InitialBurstCount > 0)
                {
                    instance.Burst(component.InitialBurstCount);
                }

                _componentEmitters.Add(
                    entity,
                    new ComponentEmitterBinding(
                        effectPath,
                        component.InitialBurstCount,
                        instance));
            }
            catch
            {
                if (instance is not null)
                {
                    RemoveEmitter(instance);
                }

                _content.Unload<ParticleEffectDefinition>(effectPath);
                throw;
            }
        }

        // Удаляем эмиттеры, если компонент сняли с Entity.
        foreach (var entity in _componentEmitters.Keys.ToArray())
        {
            if (!entitiesWithComponent.Contains(entity))
            {
                RemoveComponentEmitter(entity);
            }
        }
    }

    private ParticleEmitterInstance CreateEffectEmitter(
    EntityId sourceEntity,
    ParticleEffectDefinition definition,
    ulong seed)
    {
        EnsureNotDisposed();

        ArgumentNullException.ThrowIfNull(definition);

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

        var simulation = new ParticleEffectSimulation(
            definition,
            seed);

        var instance = new ParticleEmitterInstance(
            _ecsWorld,
            sourceEntity,
            definition.TexturePath,
            definition.Layer,
            simulation);

        _emitters.Add(instance);

        return instance;
    }

    private void RemoveComponentEmitter(EntityId entity)
    {
        if (!_componentEmitters.Remove(entity, out var binding))
        {
            return;
        }

        RemoveEmitter(binding.Instance);

        _content.Unload<ParticleEffectDefinition>(
            binding.EffectPath);
    }

    private static ParticleFactoryDelegate CreateParticleFactory(
        ParticleEffectDefinition definition)
    {
        return (ref DeterministicRandom random) =>
        {
            var lifetime = random.NextFloat(
                definition.LifetimeSeconds.Min,
                definition.LifetimeSeconds.Max);

            var size = random.NextFloat(
                definition.Size.Min,
                definition.Size.Max);

            var velocityX = random.NextFloat(
                definition.Velocity.Min.X,
                definition.Velocity.Max.X);

            var velocityY = random.NextFloat(
                definition.Velocity.Min.Y,
                definition.Velocity.Max.Y);

            return new Particle(
                Vector3.Zero,
                new Vector3(velocityX, velocityY, 0.0f),
                lifetime,
                size);
        };
    }

    private static ulong CreateParticleSeed(EntityId entity)
    {
        return ((ulong)entity.Index << 32) | entity.Generation;
    }

    private sealed record ComponentEmitterBinding(
        AssetPath EffectPath,
        int InitialBurstCount,
        ParticleEmitterInstance Instance);
}