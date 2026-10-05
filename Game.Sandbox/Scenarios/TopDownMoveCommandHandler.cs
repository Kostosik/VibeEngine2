using Engine.Core.Commands;
using Engine.Core.Math;
using Engine.ECS;
using Engine.Physics.Components;

namespace Game.Sandbox.Scenarios;

public sealed class TopDownMoveCommandHandler :
    ICommandHandler<TopDownMoveCommand>
{
    private readonly World _world;
    private readonly Fixed32 _speed;

    public TopDownMoveCommandHandler(
        World world,
        Fixed32 speed)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (speed <= Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(speed));
        }

        _world = world;
        _speed = speed;
    }

    public void Handle(
        TopDownMoveCommand command)
    {
        if (!_world.Exists(command.Entity) ||
            !_world.Has<PhysicsBody2D>(command.Entity))
        {
            return;
        }

        ref var body =
            ref _world.Get<PhysicsBody2D>(
                command.Entity);

        var direction =
            command.Direction;

        if (direction.LengthSquared() <=
            Fixed32.Zero)
        {
            body.Velocity =
                FixedVector2.Zero;

            return;
        }

        direction =
            direction.Normalize();

        body.WakeUp();

        body.Velocity =
            direction * _speed;
    }
}