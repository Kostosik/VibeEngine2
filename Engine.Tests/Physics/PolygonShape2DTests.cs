using Engine.Core.Math;
using Engine.Physics.Shapes;

namespace Engine.Tests.Physics;

public sealed class PolygonShape2DTests
{
    [Fact]
    public void Constructor_RequiresAtLeastThreeVertices()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new PolygonShape2D(
                    new[]
                    {
                        FixedVector2.Zero,
                        new FixedVector2(
                            Fixed32.One,
                            Fixed32.Zero)
                    }));
    }

    [Fact]
    public void PhysicsShape2D_CanWrapPolygon()
    {
        var polygon =
            new PolygonShape2D(
                new[]
                {
                new FixedVector2(
                    Fixed32.FromInt(-1),
                    Fixed32.FromInt(-1)),

                new FixedVector2(
                    Fixed32.FromInt(1),
                    Fixed32.FromInt(-1)),

                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromInt(1))
                });

        var shape =
            PhysicsShape2D.FromPolygon(
                polygon);

        Assert.Equal(
            PhysicsShapeType.Polygon,
            shape.Type);

        Assert.Equal(
            polygon,
            shape.Polygon);

        Assert.Equal(
            new FixedVector2(
                Fixed32.FromInt(2),
                Fixed32.FromInt(2)),
            shape.Size);
    }

    [Fact]
    public void Constructor_RejectsNonConvexPolygon()
    {
        var vertices =
            new[]
            {
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.Zero),

                new FixedVector2(
                    Fixed32.FromInt(2),
                    Fixed32.Zero),

                new FixedVector2(
                    Fixed32.One,
                    Fixed32.FromInt(1)),

                new FixedVector2(
                    Fixed32.FromInt(2),
                    Fixed32.FromInt(2)),

                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromInt(2))
            };

        Assert.Throws<ArgumentException>(
            () =>
                new PolygonShape2D(
                    vertices));
    }

    [Fact]
    public void Constructor_RejectsClockwiseVertices()
    {
        var vertices =
            new[]
            {
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.Zero),

                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromInt(2)),

                new FixedVector2(
                    Fixed32.FromInt(2),
                    Fixed32.FromInt(2)),

                new FixedVector2(
                    Fixed32.FromInt(2),
                    Fixed32.Zero)
            };

        Assert.Throws<ArgumentException>(
            () =>
                new PolygonShape2D(
                    vertices));
    }

    [Fact]
    public void GetBounds_ReturnsBoundsAroundAllVertices()
    {
        var shape =
            new PolygonShape2D(
                new[]
                {
                    new FixedVector2(
                        Fixed32.FromInt(-1),
                        Fixed32.FromInt(-2)),

                    new FixedVector2(
                        Fixed32.FromInt(2),
                        Fixed32.FromInt(-1)),

                    new FixedVector2(
                        Fixed32.FromInt(1),
                        Fixed32.FromInt(3))
                });

        var position =
            new FixedVector2(
                Fixed32.FromInt(5),
                Fixed32.FromInt(-4));

        var bounds =
            shape.GetBounds(
                position);

        Assert.Equal(
            new FixedVector2(
                Fixed32.FromInt(4),
                Fixed32.FromInt(-6)),
            bounds.Min);

        Assert.Equal(
            new FixedVector2(
                Fixed32.FromInt(7),
                Fixed32.FromInt(-1)),
            bounds.Max);
    }

    [Fact]
    public void Constructor_CopiesVertices()
    {
        var vertices =
            new[]
            {
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.Zero),

                new FixedVector2(
                    Fixed32.FromInt(2),
                    Fixed32.Zero),

                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromInt(2))
            };

        var shape =
            new PolygonShape2D(
                vertices);

        vertices[0] =
            new FixedVector2(
                Fixed32.FromInt(100),
                Fixed32.FromInt(100));

        Assert.Equal(
            FixedVector2.Zero,
            shape.GetVertex(0));
    }

    [Fact]
    public void Equality_UsesVertexSequence()
    {
        var first =
            new PolygonShape2D(
                new[]
                {
                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.Zero),

                    new FixedVector2(
                        Fixed32.FromInt(2),
                        Fixed32.Zero),

                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.FromInt(2))
                });

        var second =
            new PolygonShape2D(
                new[]
                {
                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.Zero),

                    new FixedVector2(
                        Fixed32.FromInt(2),
                        Fixed32.Zero),

                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.FromInt(2))
                });

        Assert.Equal(
            first,
            second);
    }
}