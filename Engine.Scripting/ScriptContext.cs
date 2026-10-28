using Engine.Core.Events;
using Engine.ECS;
using Engine.ECS.Entities;
using System.Text.Json;

namespace Engine.Scripting;

public readonly struct ScriptContext
{
    private readonly World _world;
    private readonly ScriptEventSubscriptions _eventSubscriptions;
    internal ScriptContext(
        World world,
        EntityId entity,
        string configurationJson,
        ScriptEventSubscriptions eventSubscriptions)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(configurationJson);
        ArgumentNullException.ThrowIfNull(eventSubscriptions);

        _world = world;
        Entity = entity;
        ConfigurationJson = configurationJson;
        _eventSubscriptions = eventSubscriptions;
    }
    public string ConfigurationJson { get; }
    public EntityId Entity { get; }

    public bool Has<T>()
        where T : struct
    {
        return _world.Has<T>(Entity);
    }

    public ref T Get<T>()
        where T : struct
    {
        return ref _world.Get<T>(Entity);
    }

    public void Set<T>(
        T component)
        where T : struct
    {
        _world.Add(
            Entity,
            component);
    }

    public bool Remove<T>()
        where T : struct
    {
        return _world.Remove<T>(Entity);
    }

    public bool IsAlive(EntityId entity)
    {
        return _world.Exists(entity);
    }

    public bool Has<T>(EntityId entity)
        where T : struct
    {
        return _world.Has<T>(entity);
    }

    public ref T Get<T>(EntityId entity)
        where T : struct
    {
        return ref _world.Get<T>(entity);
    }

    public T DeserializeConfiguration<T>()
    {
        var configuration =
            JsonSerializer.Deserialize<T>(ConfigurationJson);

        if (configuration is null)
        {
            throw new InvalidOperationException(
                $"Script configuration for entity '{Entity}' " +
                $"could not be deserialized as '{typeof(T).Name}'.");
        }

        return configuration;
    }

    public EventSubscription Subscribe<TEvent>(
    Action<TEvent> handler)
    where TEvent : IEvent
    {
        return _eventSubscriptions.Subscribe(handler);
    }
}