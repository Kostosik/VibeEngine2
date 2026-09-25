using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.ECS.Entities;

namespace Game.Sandbox.Gameplay;

public sealed class InteractionResolver
{
    private readonly Engine.ECS.World _world;
    private readonly IReadOnlyList<EntityId> _targets;

    public InteractionResolver(
        Engine.ECS.World world,
        IReadOnlyList<EntityId> targets)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        ArgumentNullException.ThrowIfNull(
            targets);

        _world = world;
        _targets = targets;
    }

    public EntityId? FindTarget(
        EntityId player)
    {
        if (!_world.Exists(player) ||
            !_world.Has<Transform2D>(player))
        {
            return null;
        }

        var playerPosition =
            _world.Get<Transform2D>(
                player).Position;

        EntityId? bestTarget = null;
        Fixed32? bestDistance = null;

        foreach (var target in _targets)
        {
            if (!_world.Exists(target) ||
                !_world.Has<Transform2D>(target) ||
                !_world.Has<InteractionTarget>(target))
            {
                continue;
            }

            ref var interaction =
                ref _world.Get<InteractionTarget>(
                    target);

            if (interaction.IsActivated)
            {
                continue;
            }

            var targetPosition =
                _world.Get<Transform2D>(
                    target).Position;

            var delta =
                targetPosition -
                playerPosition;

            var distanceSquared =
                delta.LengthSquared();

            var radiusSquared =
                interaction.Radius *
                interaction.Radius;

            if (distanceSquared >
                radiusSquared)
            {
                continue;
            }

            if (bestTarget is null ||
                distanceSquared <
                bestDistance)
            {
                bestTarget =
                    target;

                bestDistance =
                    distanceSquared;
            }
        }

        return bestTarget;
    }
}