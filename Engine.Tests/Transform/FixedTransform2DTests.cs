using Engine.Core.Math;
using Engine.Transform;

namespace Engine.Tests.Transform;

public sealed class FixedTransform2DTests
{
    [Fact]
    public void IdentityHasExpectedValues()
    {
        var transform =
            FixedTransform2D.Identity;

        Assert.Equal(
            FixedVector2.Zero,
            transform.Position);

        Assert.Equal(
            Fixed32.Zero,
            transform.Rotation);

        Assert.Equal(
            new FixedVector2(
                Fixed32.One,
                Fixed32.One),
            transform.Scale);
    }

    [Fact]
    public void TranslateChangesPosition()
    {
        var transform =
            FixedTransform2D.Identity;

        var result =
            FixedTransformOperations2D.Translate(
                transform,
                new FixedVector2(
                    Fixed32.FromInt(5),
                    Fixed32.FromInt(7)));

        Assert.Equal(
            new FixedVector2(
                Fixed32.FromInt(5),
                Fixed32.FromInt(7)),
            result.Position);
    }

    [Fact]
    public void RotateChangesRotation()
    {
        var transform =
            FixedTransform2D.Identity;

        var result =
            FixedTransformOperations2D.Rotate(
                transform,
                Fixed32.FromInt(2));

        Assert.Equal(
            Fixed32.FromInt(2),
            result.Rotation);
    }

    [Fact]
    public void ScaleMultipliesScale()
    {
        var transform =
            FixedTransformOperations2D.Create(
                FixedVector2.Zero,
                Fixed32.Zero,
                new FixedVector2(
                    Fixed32.FromInt(2),
                    Fixed32.FromInt(3)));

        var result =
            FixedTransformOperations2D.Scale(
                transform,
                new FixedVector2(
                    Fixed32.FromInt(4),
                    Fixed32.FromInt(5)));

        Assert.Equal(
            new FixedVector2(
                Fixed32.FromInt(8),
                Fixed32.FromInt(15)),
            result.Scale);
    }

    [Fact]
    public void TransformPointAppliesFullTransform()
    {
        var transform =
            FixedTransformOperations2D.Create(
                new FixedVector2(
                    Fixed32.FromInt(10),
                    Fixed32.FromInt(20)),
                Fixed32.HalfPi,
                new FixedVector2(
                    Fixed32.FromInt(2),
                    Fixed32.FromInt(2)));

        var result =
            FixedTransformOperations2D.TransformPoint(
                transform,
                new FixedVector2(
                    Fixed32.One,
                    Fixed32.Zero));

        Assert.Equal(
            10.0f,
            result.X.ToFloat(),
            2);

        Assert.Equal(
            22.0f,
            result.Y.ToFloat(),
            2);
    }

    [Fact]
    public void CreatePreservesAllComponents()
    {
        var position =
            new FixedVector2(
                Fixed32.FromInt(10),
                Fixed32.FromInt(20));

        var rotation =
            Fixed32.FromInt(3);

        var scale =
            new FixedVector2(
                Fixed32.FromInt(2),
                Fixed32.FromInt(4));

        var transform =
            FixedTransformOperations2D.Create(
                position,
                rotation,
                scale);

        Assert.Equal(position, transform.Position);
        Assert.Equal(rotation, transform.Rotation);
        Assert.Equal(scale, transform.Scale);
    }
}