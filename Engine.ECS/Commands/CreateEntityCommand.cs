using Engine.ECS.Entities;

namespace Engine.ECS.Commands;

internal sealed class CreateEntityCommand : ICommand
{
    private readonly EntityId _entity;

    public CreateEntityCommand(
        EntityId entity)
    {
        _entity = entity;
    }

    public void Apply(
        World world)
    {
        world.ActivateEntity(
            _entity);
    }
}