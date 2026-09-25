using Engine.ECS.Entities;

namespace Engine.ECS.Commands;

internal sealed class DestroyEntityCommand : ICommand
{
    private readonly EntityId _entity;

    public DestroyEntityCommand(
        EntityId entity)
    {
        _entity = entity;
    }

    public void Apply(
        World world)
    {
        world.DestroyEntity(
            _entity);
    }
}