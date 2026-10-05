using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.Physics.Components;
using Engine.Physics.Shapes;

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

            if (!_world.Has<WorldTransform2D>(
                    entity))
            {
                continue;
            }

            ref var transform =
                ref _world.Get<WorldTransform2D>(
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
                    DrawCollider(
                        drawList,
                        collider,
                        transform);
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

    private void DrawCollider(
    DebugDrawList drawList,
    Collider2D collider,
    WorldTransform2D transform)
    {
        var shape =
            collider.Shape;

        var position =
            collider.GetWorldPosition(
                transform.Position,
                transform.Rotation);

        switch (shape.Type)
        {
            case PhysicsShapeType.Aabb:
                {
                    var bounds =
                        shape.Aabb.GetBounds(
                            position);

                    drawList.DrawRectangle(
                        new DebugRectangle(
                            bounds,
                            DebugColor.Yellow));

                    break;
                }

            case PhysicsShapeType.Circle:
                {
                    drawList.DrawCircle(
                        new DebugCircle(
                            position,
                            shape.Circle.Radius,
                            DebugColor.Blue));

                    break;
                }

            case PhysicsShapeType.Polygon:
                {
                    var polygon =
                        shape.Polygon;

                    for (var i = 0;
                         i < polygon.VertexCount;
                         i++)
                    {
                        var next =
                            (i + 1) %
                            polygon.VertexCount;

                        var start =
                            TransformVertex(
                                polygon.GetVertex(i),
                                position,
                                transform.Rotation);

                        var end =
                            TransformVertex(
                                polygon.GetVertex(next),
                                position,
                                transform.Rotation);

                        drawList.DrawLine(
                            new DebugLine(
                                start,
                                end,
                                DebugColor.Red));
                    }

                    break;
                }

            default:
                throw new InvalidOperationException(
                    $"Unsupported physics shape type '{shape.Type}'.");
        }
    }

    private static FixedVector2 TransformVertex(
    FixedVector2 vertex,
    FixedVector2 position,
    Fixed32 rotation)
    {
        var cosine =
            Fixed32.Cos(rotation);

        var sine =
            Fixed32.Sin(rotation);

        return
            new FixedVector2(
                vertex.X * cosine -
                vertex.Y * sine,

                vertex.X * sine +
                vertex.Y * cosine) +
            position;
    }
}