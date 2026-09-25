using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.Physics.Components;

namespace Engine.Tooling.DebugVisualization;

public sealed class PhysicsDebugVisualizer :
    IDebugVisualizationProvider
{
    public bool Enabled { get; set; } = false;

    private readonly Engine.ECS.World _world;

    public PhysicsDebugVisualizer(
        Engine.ECS.World world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        _world = world;
    }

    public bool ShowColliders { get; set; } = true;

    public bool ShowVelocity { get; set; } = true;

    public DebugColor ColliderColor { get; set; } =
        DebugColor.Yellow;

    public DebugColor VelocityColor { get; set; } =
        DebugColor.Blue;

    public void Draw(
        DebugDrawList drawList)
    {
        ArgumentNullException.ThrowIfNull(
            drawList);

        if (!Enabled)
        {
            return;
        }

        foreach (var item
                 in _world.Query<PhysicsBody2D>())
        {
            var entity =
                item.Entity;

            ref var body =
                ref item.Component;

            if (!_world.Has<Transform2D>(
                    entity))
            {
                continue;
            }

            ref var transform =
                ref _world.Get<Transform2D>(
                    entity);

            if (ShowColliders &&
                _world.Has<Collider2D>(
                    entity))
            {
                ref var collider =
                    ref _world.Get<Collider2D>(
                        entity);

                if (collider.Enabled)
                {
                    var bounds =
                        collider.GetWorldBounds(
                            transform.Position);

                    drawList.DrawRectangle(
                        new DebugRectangle(
                            bounds,
                            ColliderColor));
                }
            }

            if (ShowVelocity &&
                body.BodyType != PhysicsBodyType.Static)
            {
                var end =
                    transform.Position +
                    body.Velocity *
                    Fixed32.FromRatio(
                        1,
                        4);

                drawList.DrawLine(
                    new DebugLine(
                        transform.Position,
                        end,
                        VelocityColor));
            }
        }
    }
}