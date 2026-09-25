using Engine.ECS.Entities;

namespace Engine.ECS.Commands;

internal sealed class AddComponentCommand<T> : ICommand
    where T : struct
{
    private readonly EntityId _entity;
    private readonly T _component;

    public AddComponentCommand(
        EntityId entity,
        T component)
    {
        _entity = entity;
        _component = component;
    }

    public void Apply(
        World world)
    {
        world.Add(
            _entity,
            _component);
    }
}