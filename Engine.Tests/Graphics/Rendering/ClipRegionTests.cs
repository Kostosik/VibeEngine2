using Engine.Core.Math;
using Engine.Graphics.Rendering;

namespace Engine.Tests.Graphics.Rendering;

public sealed class ClipRegionTests
{
    [Fact]
    public void Intersect_RotatedRegion_ProducesClippedPolygon()
    {
        var diamond =
            ClipRegion.FromVertices(
            [
                new Vector2(20.0f, 0.0f),
                new Vector2(40.0f, 20.0f),
                new Vector2(20.0f, 40.0f),
                new Vector2(0.0f, 20.0f)
            ]);

        var rectangle =
            ClipRegion.FromRectangle(
                new Rectangle(
                    10.0f,
                    10.0f,
                    20.0f,
                    20.0f));

        var intersection =
            diamond.Intersect(rectangle);

        Assert.False(intersection.IsEmpty);

        Assert.Equal(
            4,
            intersection.Vertices.Length);

        var bounds =
            intersection.Bounds;

        Assert.Equal(10.0f, bounds.X, precision: 4);
        Assert.Equal(10.0f, bounds.Y, precision: 4);
        Assert.Equal(20.0f, bounds.Width, precision: 4);
        Assert.Equal(20.0f, bounds.Height, precision: 4);

        foreach (var vertex in intersection.Vertices)
        {
            Assert.InRange(vertex.X, 10.0f, 30.0f);
            Assert.InRange(vertex.Y, 10.0f, 30.0f);

            var distance =
                MathF.Abs(vertex.X - 20.0f) +
                MathF.Abs(vertex.Y - 20.0f);

            Assert.True(distance <= 20.001f);
        }
    }

    [Fact]
    public void Intersect_DisjointRegions_ReturnsEmptyRegion()
    {
        var first =
            ClipRegion.FromRectangle(
                new Rectangle(0.0f, 0.0f, 10.0f, 10.0f));

        var second =
            ClipRegion.FromRectangle(
                new Rectangle(20.0f, 20.0f, 10.0f, 10.0f));

        var intersection =
            first.Intersect(second);

        Assert.True(intersection.IsEmpty);
        Assert.Equal(0, intersection.Vertices.Length);
    }
}