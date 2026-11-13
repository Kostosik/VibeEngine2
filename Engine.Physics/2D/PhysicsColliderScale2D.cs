using Engine.Core.Math;
using Engine.Physics.Components;
using Engine.Physics.Shapes;

namespace Engine.Physics;

/// <summary>
/// Creates a scaled copy of a collider for physics queries
/// and editor visualization without modifying the ECS component.
/// </summary>
public static class PhysicsColliderScale2D
{
    public static bool TryApplyWorldScale(
        ref Collider2D collider,
        FixedVector2 scale)
    {
        try
        {
            collider.Validate();
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        var scaleX = Fixed32.Abs(scale.X);
        var scaleY = Fixed32.Abs(scale.Y);

        if (scaleX == Fixed32.Zero ||
            scaleY == Fixed32.Zero)
        {
            return false;
        }

        switch (collider.Shape.Type)
        {
            case PhysicsShapeType.Aabb:
                {
                    var shape = collider.Shape.Aabb;

                    collider.Shape =
                        PhysicsShape2D.FromAabb(
                            new AabbShape2D(
                                new FixedVector2(
                                    shape.Size.X * scaleX,
                                    shape.Size.Y * scaleY)));

                    break;
                }

            case PhysicsShapeType.Circle:
                {
                    var shape = collider.Shape.Circle;

                    var radiusScale =
                        Fixed32.Max(scaleX, scaleY);

                    collider.Shape =
                        PhysicsShape2D.FromCircle(
                            new CircleShape2D(
                                shape.Radius * radiusScale));

                    break;
                }

            case PhysicsShapeType.Polygon:
                {
                    var shape = collider.Shape.Polygon;

                    var vertices =
                        new FixedVector2[shape.VertexCount];

                    for (var i = 0; i < shape.VertexCount; i++)
                    {
                        var vertex = shape.GetVertex(i);

                        vertices[i] =
                            new FixedVector2(
                                vertex.X * scale.X,
                                vertex.Y * scale.Y);
                    }

                    if ((scale.X < Fixed32.Zero) !=
                        (scale.Y < Fixed32.Zero))
                    {
                        Array.Reverse(vertices);
                    }

                    collider.Shape =
                        PhysicsShape2D.FromPolygon(
                            new PolygonShape2D(vertices));

                    break;
                }

            default:
                return false;
        }

        collider.Offset =
            new FixedVector2(
                collider.Offset.X * scale.X,
                collider.Offset.Y * scale.Y);

        return true;
    }
}