using Engine.Core.Commands;

namespace Game.Sandbox.Gameplay;

public sealed class InteractCommandHandler :
    ICommandHandler<InteractCommand>
{
    private readonly Engine.ECS.World _world;

    public InteractCommandHandler(
        Engine.ECS.World world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        _world = world;
    }

    public void Handle(
        InteractCommand command)
    {
        if (!_world.Exists(command.Player) ||
            !_world.Exists(command.Target))
        {
            return;
        }

        if (!_world.Has<InteractionTarget>(
                command.Target))
        {
            return;
        }

        ref var target =
            ref _world.Get<InteractionTarget>(
                command.Target);

        target.IsActivated =
            !target.IsActivated;
    }
}