using Engine.Core.Math;
using Engine.Physics.Shapes;

namespace Engine.Tests.Physics;

public sealed class CircleShape2DTests
{
    [Fact]
    public void Constructor_RejectsNonPositiveRadius()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new CircleShape2D(
                    Fixed32.Zero));
    }

    [Fact]
    public void GetBounds_ReturnsDiameterSizedBoundsCenteredOnPosition()
    {
        var radius =
            Fixed32.FromInt(2);

        var shape =
            new CircleShape2D(
                radius);

        var position =
            new FixedVector2(
                Fixed32.FromInt(5),
                Fixed32.FromInt(-3));

        var bounds =
            shape.GetBounds(
                position);

        Assert.Equal(
            new FixedVector2(
                Fixed32.FromInt(3),
                Fixed32.FromInt(-5)),
            bounds.Min);

        Assert.Equal(
            new FixedVector2(
                Fixed32.FromInt(7),
                Fixed32.FromInt(-1)),
            bounds.Max);
    }

    [Fact]
    public void ContainsPoint_ReturnsTrueInsideCircle()
    {
        var shape =
            new CircleShape2D(
                Fixed32.FromInt(2));

        var center =
            FixedVector2.Zero;

        Assert.True(
            shape.ContainsPoint(
                center,
                new FixedVector2(
                    Fixed32.One,
                    Fixed32.Zero)));
    }

    [Fact]
    public void ContainsPoint_ReturnsTrueOnBoundary()
    {
        var shape =
            new CircleShape2D(
                Fixed32.FromInt(2));

        var center =
            FixedVector2.Zero;

        Assert.True(
            shape.ContainsPoint(
                center,
                new FixedVector2(
                    Fixed32.FromInt(2),
                    Fixed32.Zero)));
    }

    [Fact]
    public void ContainsPoint_ReturnsFalseOutsideCircle()
    {
        var shape =
            new CircleShape2D(
                Fixed32.FromInt(2));

        var center =
            FixedVector2.Zero;

        Assert.False(
            shape.ContainsPoint(
                center,
                new FixedVector2(
                    Fixed32.FromInt(3),
                    Fixed32.Zero)));
    }

    [Fact]
    public void Equality_UsesRadius()
    {
        var first =
            new CircleShape2D(
                Fixed32.FromInt(3));

        var second =
            new CircleShape2D(
                Fixed32.FromInt(3));

        var different =
            new CircleShape2D(
                Fixed32.FromInt(4));

        Assert.Equal(
            first,
            second);

        Assert.NotEqual(
            first,
            different);
    }
}