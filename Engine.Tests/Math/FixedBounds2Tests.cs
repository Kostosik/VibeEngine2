using Engine.Core.Math;

namespace Engine.Tests.Math;

public sealed class FixedBounds2Tests
{
    [Fact]
    public void Contains_Point()
    {
        var bounds =
            new FixedBounds2(
                new FixedVector2(
                    Fixed32.FromInt(0),
                    Fixed32.FromInt(0)),

                new FixedVector2(
                    Fixed32.FromInt(10),
                    Fixed32.FromInt(10)));

        Assert.True(
            bounds.Contains(
                new FixedVector2(
                    Fixed32.FromInt(5),
                    Fixed32.FromInt(5))));
    }

    [Fact]
    public void Contains_MinAndMax()
    {
        var bounds =
            new FixedBounds2(
                FixedVector2.Zero,
                new FixedVector2(
                    Fixed32.FromInt(10),
                    Fixed32.FromInt(10)));

        Assert.True(
            bounds.Contains(
                FixedVector2.Zero));

        Assert.True(
            bounds.Contains(
                new FixedVector2(
                    Fixed32.FromInt(10),
                    Fixed32.FromInt(10))));
    }

    [Fact]
    public void DoesNotContain_PointOutside()
    {
        var bounds =
            new FixedBounds2(
                FixedVector2.Zero,
                new FixedVector2(
                    Fixed32.FromInt(10),
                    Fixed32.FromInt(10)));

        Assert.False(
            bounds.Contains(
                new FixedVector2(
                    Fixed32.FromInt(11),
                    Fixed32.FromInt(5))));
    }

    [Fact]
    public void Intersects_WhenOverlapping()
    {
        var first =
            new FixedBounds2(
                FixedVector2.Zero,
                new FixedVector2(
                    Fixed32.FromInt(10),
                    Fixed32.FromInt(10)));

        var second =
            new FixedBounds2(
                new FixedVector2(
                    Fixed32.FromInt(5),
                    Fixed32.FromInt(5)),

                new FixedVector2(
                    Fixed32.FromInt(15),
                    Fixed32.FromInt(15)));

        Assert.True(
            first.Intersects(second));
    }

    [Fact]
    public void DoesNotIntersect_WhenSeparated()
    {
        var first =
            new FixedBounds2(
                FixedVector2.Zero,
                new FixedVector2(
                    Fixed32.FromInt(10),
                    Fixed32.FromInt(10)));

        var second =
            new FixedBounds2(
                new FixedVector2(
                    Fixed32.FromInt(20),
                    Fixed32.FromInt(20)),

                new FixedVector2(
                    Fixed32.FromInt(30),
                    Fixed32.FromInt(30)));

        Assert.False(
            first.Intersects(second));
    }

    [Fact]
    public void Translate_MovesBounds()
    {
        var bounds =
            new FixedBounds2(
                FixedVector2.Zero,
                new FixedVector2(
                    Fixed32.FromInt(10),
                    Fixed32.FromInt(10)));

        var translated =
            bounds.Translate(
                new FixedVector2(
                    Fixed32.FromInt(5),
                    Fixed32.FromInt(2)));

        Assert.Equal(
            Fixed32.FromInt(5),
            translated.Min.X);

        Assert.Equal(
            Fixed32.FromInt(2),
            translated.Min.Y);
    }

    [Fact]
    public void Size_IsCorrect()
    {
        var bounds =
            new FixedBounds2(
                new FixedVector2(
                    Fixed32.FromInt(2),
                    Fixed32.FromInt(3)),

                new FixedVector2(
                    Fixed32.FromInt(12),
                    Fixed32.FromInt(13)));

        Assert.Equal(
            Fixed32.FromInt(10),
            bounds.Size.X);

        Assert.Equal(
            Fixed32.FromInt(10),
            bounds.Size.Y);
    }
}