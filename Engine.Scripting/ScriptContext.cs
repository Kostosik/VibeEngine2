using Engine.ECS;
using Engine.ECS.Entities;

namespace Engine.Scripting;

public readonly struct ScriptContext
{
    private readonly World _world;

    internal ScriptContext(
        World world,
        EntityId entity,
        string configurationJson)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(configurationJson);

        _world = world;
        Entity = entity;
        ConfigurationJson = configurationJson;
    }
    public string ConfigurationJson { get; }
    public EntityId Entity { get; }

    public bool IsAlive =>
        _world.Exists(Entity);

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
}