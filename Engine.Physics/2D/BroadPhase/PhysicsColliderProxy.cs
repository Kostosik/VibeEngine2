using Engine.Core.Math;
using Engine.ECS.Entities;
using Engine.Physics.Components;

namespace Engine.Physics.BroadPhase;

public readonly record struct PhysicsColliderProxy(
    EntityId Entity,
    FixedBounds2 Bounds,
    Collider2D Collider)
{
    public Fixed32 WorldSine { get; init; }

    public Fixed32 WorldCosine { get; init; }

    public bool HasCachedRotation { get; init; }

    public FixedVector2 WorldPosition { get; init; }

    public Fixed32 WorldRotation { get; init; }

    public FixedVector2 GetWorldPolygonVertex(
        int index)
    {
        var vertex =
            Collider.Shape.Polygon.GetVertex(index);

        if (HasCachedRotation)
        {
            return Rotate(
                vertex,
                WorldSine,
                WorldCosine) +
                WorldPosition;
        }

        return Rotate(
            vertex,
            WorldRotation) +
            WorldPosition;
    }

    private static FixedVector2 Rotate(
        FixedVector2 value,
        Fixed32 angle)
    {
        return Rotate(
            value,
            Fixed32.Sin(angle),
            Fixed32.Cos(angle));
    }

    private static FixedVector2 Rotate(
        FixedVector2 value,
        Fixed32 sine,
        Fixed32 cosine)
    {
        return new FixedVector2(
            value.X * cosine -
            value.Y * sine,

            value.X * sine +
            value.Y * cosine);
    }
}