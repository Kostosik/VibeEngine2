using Engine.ECS.Entities;

namespace Engine.ECS.Commands;

internal sealed class RemoveComponentCommand<T> : ICommand
    where T : struct
{
    private readonly EntityId _entity;

    public RemoveComponentCommand(
        EntityId entity)
    {
        _entity = entity;
    }

    public void Apply(
        World world)
    {
        world.Remove<T>(
            _entity);
    }
}