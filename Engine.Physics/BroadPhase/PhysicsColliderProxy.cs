using Engine.Core.Math;
using Engine.ECS.Entities;
using Engine.Physics.Components;
using Engine.Physics.Shapes;

namespace Engine.Physics.BroadPhase;

public readonly record struct PhysicsColliderProxy(
    EntityId Entity,
    FixedBounds2 Bounds,
    Collider2D Collider)
{
    public FixedVector2 WorldPosition { get; init; }

    public Fixed32 WorldRotation { get; init; }

    public FixedVector2 GetWorldPolygonVertex(
        int index)
    {
        var vertex =
            Collider.Shape
                .Polygon
                .GetVertex(index);

        return Rotate(
            vertex,
            WorldRotation) +
            WorldPosition;
    }

    private static FixedVector2 Rotate(
        FixedVector2 value,
        Fixed32 angle)
    {
        var cosine =
            Fixed32.Cos(angle);

        var sine =
            Fixed32.Sin(angle);

        return new FixedVector2(
            value.X * cosine -
            value.Y * sine,

            value.X * sine +
            value.Y * cosine);
    }
}