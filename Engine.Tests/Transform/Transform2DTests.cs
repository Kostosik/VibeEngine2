using Engine.Core.Math;
using Engine.Transform;

namespace Engine.Tests.Transform;

public sealed class Transform2DTests
{
    [Fact]
    public void IdentityHasExpectedValues()
    {
        var transform = Transform2D.Identity;

        Assert.Equal(Vector2.Zero, transform.Position);
        Assert.Equal(0.0f, transform.Rotation);
        Assert.Equal(
            new Vector2(1.0f, 1.0f),
            transform.Scale);

        Assert.True(transform.IsFinite);
    }

    [Fact]
    public void CombineAppliesTranslation()
    {
        var parent = new Transform2D(
            new Vector2(10.0f, 20.0f));

        var local = new Transform2D(
            new Vector2(3.0f, 4.0f));

        var world =
            TransformOperations2D.Combine(
                parent,
                local);

        Assert.Equal(
            new Vector2(13.0f, 24.0f),
            world.Position);
    }

    [Fact]
    public void CombineAppliesRotationToLocalPosition()
    {
        var parent = new Transform2D(
            Vector2.Zero)
        {
            Rotation = MathF.PI * 0.5f
        };

        var local = new Transform2D(
            new Vector2(1.0f, 0.0f));

        var world =
            TransformOperations2D.Combine(
                parent,
                local);

        Assert.Equal(
            0.0f,
            world.Position.X,
            5);

        Assert.Equal(
            1.0f,
            world.Position.Y,
            5);
    }

    [Fact]
    public void CombineAppliesScale()
    {
        var parent = new Transform2D(
            Vector2.Zero)
        {
            Scale = new Vector2(2.0f, 3.0f)
        };

        var local = new Transform2D(
            new Vector2(4.0f, 5.0f))
        {
            Scale = new Vector2(6.0f, 7.0f)
        };

        var world =
            TransformOperations2D.Combine(
                parent,
                local);

        Assert.Equal(
            new Vector2(8.0f, 15.0f),
            world.Position);

        Assert.Equal(
            new Vector2(12.0f, 21.0f),
            world.Scale);
    }

    [Fact]
    public void CombineAccumulatesRotation()
    {
        var parent = new Transform2D(
            Vector2.Zero)
        {
            Rotation = 1.0f
        };

        var local = new Transform2D(
            Vector2.Zero)
        {
            Rotation = 2.0f
        };

        var world =
            TransformOperations2D.Combine(
                parent,
                local);

        Assert.Equal(
            3.0f,
            world.Rotation);
    }

    [Fact]
    public void TransformPointAppliesFullTransform()
    {
        var transform = new Transform2D(
            new Vector2(10.0f, 20.0f))
        {
            Rotation = MathF.PI * 0.5f,
            Scale = new Vector2(2.0f, 2.0f)
        };

        var result =
            TransformOperations2D.TransformPoint(
                transform,
                new Vector2(1.0f, 0.0f));

        Assert.Equal(10.0f, result.X, 5);
        Assert.Equal(22.0f, result.Y, 5);
    }
}