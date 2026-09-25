using Engine.ECS.Entities;

namespace Engine.ECS.Commands;

public sealed class CommandBuffer
{
    private readonly World _world;

    private readonly List<ICommand> _commands = new();

    internal CommandBuffer(
        World world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        _world = world;
    }

    public int Count =>
        _commands.Count;

    public EntityId CreateEntity()
    {
        var entity =
            _world.ReserveEntity();

        _commands.Add(
            new CreateEntityCommand(
                entity));

        return entity;
    }

    public void Add<T>(
        EntityId entity,
        T component)
        where T : struct
    {
        _commands.Add(
            new AddComponentCommand<T>(
                entity,
                component));
    }

    public void Remove<T>(
        EntityId entity)
        where T : struct
    {
        _commands.Add(
            new RemoveComponentCommand<T>(
                entity));
    }

    public void Destroy(
        EntityId entity)
    {
        _commands.Add(
            new DestroyEntityCommand(
                entity));
    }

    internal void Apply(
        World world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        foreach (var command in _commands)
        {
            command.Apply(world);
        }

        _commands.Clear();
    }

    internal void Clear()
    {
        _commands.Clear();
    }
}