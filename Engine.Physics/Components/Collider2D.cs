using Engine.Core.Determinism;
using Engine.Core.Math;
using Engine.Physics.Materials;
using Engine.Physics.Shapes;

namespace Engine.Physics.Components;

public struct Collider2D :
    IDeterministicState
{
    public Collider2D(
        AabbShape2D shape)
    {
        Shape =
            PhysicsShape2D.FromAabb(
                shape);

        Offset = FixedVector2.Zero;
        Material = PhysicsMaterial2D.Default;
        IsTrigger = false;
        Enabled = true;
        CollisionLayer = 1u;
        CollisionMask = uint.MaxValue;
    }

    public Collider2D(
    PolygonShape2D shape)
    {
        Shape =
            PhysicsShape2D.FromPolygon(
                shape);

        Offset = FixedVector2.Zero;
        Material = PhysicsMaterial2D.Default;
        IsTrigger = false;
        Enabled = true;
        CollisionLayer = 1u;
        CollisionMask = uint.MaxValue;
    }

    public Collider2D(
        CircleShape2D shape)
    {
        Shape =
            PhysicsShape2D.FromCircle(
                shape);

        Offset = FixedVector2.Zero;
        Material = PhysicsMaterial2D.Default;
        IsTrigger = false;
        Enabled = true;
        CollisionLayer = 1u;
        CollisionMask = uint.MaxValue;
    }

    public PhysicsShape2D Shape { get; set; }

    public FixedVector2 Offset { get; set; }

    public PhysicsMaterial2D Material { get; set; }

    public bool IsTrigger { get; set; }

    public bool Enabled { get; set; }

    public uint CollisionLayer { get; set; }

    public uint CollisionMask { get; set; }

    public FixedBounds2 GetWorldBounds(
        FixedVector2 bodyPosition)
    {
        return Shape.GetBounds(
            bodyPosition + Offset);
    }

    public FixedVector2 GetWorldPosition(
    FixedVector2 bodyPosition,
    Fixed32 bodyRotation)
    {
        return bodyPosition +
               Rotate(
                   Offset,
                   bodyRotation);
    }

    public FixedBounds2 GetWorldBounds(
        FixedVector2 bodyPosition,
        Fixed32 bodyRotation)
    {
        var position =
            GetWorldPosition(
                bodyPosition,
                bodyRotation);

        return Shape.Type switch
        {
            PhysicsShapeType.Aabb =>
                Shape.Aabb.GetBounds(
                    position),

            PhysicsShapeType.Circle =>
                Shape.Circle.GetBounds(
                    position),

            PhysicsShapeType.Polygon =>
                GetPolygonWorldBounds(
                    position,
                    bodyRotation),

            _ =>
                throw new InvalidOperationException(
                    $"Unsupported physics shape type '{Shape.Type}'.")
        };
    }

    private FixedBounds2 GetPolygonWorldBounds(
        FixedVector2 position,
        Fixed32 rotation)
    {
        var polygon =
            Shape.Polygon;

        var first =
            Rotate(
                polygon.GetVertex(0),
                rotation) +
            position;

        var minimum =
            first;

        var maximum =
            first;

        for (var i = 1;
             i < polygon.VertexCount;
             i++)
        {
            var vertex =
                Rotate(
                    polygon.GetVertex(i),
                    rotation) +
                position;

            minimum =
                new FixedVector2(
                    Fixed32.Min(
                        minimum.X,
                        vertex.X),
                    Fixed32.Min(
                        minimum.Y,
                        vertex.Y));

            maximum =
                new FixedVector2(
                    Fixed32.Max(
                        maximum.X,
                        vertex.X),
                    Fixed32.Max(
                        maximum.Y,
                        vertex.Y));
        }

        return new FixedBounds2(
            minimum,
            maximum);
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
    public bool CanCollideWith(
        Collider2D other)
    {
        return
            (CollisionMask & other.CollisionLayer) != 0 &&
            (other.CollisionMask & CollisionLayer) != 0;
    }

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        Shape.AddToHash(
            ref hasher);

        hasher.AddFixedVector2(
            Offset);

        Material.AddToHash(
            ref hasher);

        hasher.AddBool(
            IsTrigger);

        hasher.AddBool(
            Enabled);

        hasher.AddUInt32(
            CollisionLayer);

        hasher.AddUInt32(
            CollisionMask);
    }
}