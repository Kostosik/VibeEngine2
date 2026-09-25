using Engine.Core.Commands;
using Engine.Core.Math;
using Engine.ECS;
using Engine.Physics.Components;

namespace Game.Sandbox.Gameplay;

public sealed class PlayerMovementCommandHandler :
    ICommandHandler<PlayerMoveCommand>
{
    private readonly Engine.ECS.World _world;
    private readonly Fixed32 _moveSpeed;

    public PlayerMovementCommandHandler(
        Engine.ECS.World world,
        Fixed32 moveSpeed)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (moveSpeed <= Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(moveSpeed));
        }

        _world = world;
        _moveSpeed = moveSpeed;
    }

    public void Handle(
        PlayerMoveCommand command)
    {
        if (!_world.Exists(command.Player))
        {
            return;
        }

        if (!_world.Has<PhysicsBody2D>(
                command.Player))
        {
            return;
        }

        ref var body =
            ref _world.Get<PhysicsBody2D>(
                command.Player);

        var direction =
            command.Direction;

        if (direction.LengthSquared() <= Fixed32.Zero)
        {
            body.Velocity =
                FixedVector2.Zero;

            return;
        }

        direction =
            direction.Normalize();

        body.Velocity =
    direction *
    _moveSpeed;
    }
}