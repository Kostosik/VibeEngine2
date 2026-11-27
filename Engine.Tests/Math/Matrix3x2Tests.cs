
using Engine.Core.Math;

namespace Engine.Tests.Math;

public sealed class Matrix3x2Tests
{
    [Fact]
    public void TransformPoint_ComposesScaleRotationTranslation()
    {
        var matrix =
            Matrix3x2.CreateScale(new Vector2(2, 3)) *
            Matrix3x2.CreateRotation(MathF.PI / 2) *
            Matrix3x2.CreateTranslation(new Vector2(10, 20));

        var result = matrix.TransformPoint(Vector2.UnitX);

        Assert.Equal(10.0f, result.X, 5);
        Assert.Equal(22.0f, result.Y, 5);
    }

    [Fact]
    public void TryInvert_RestoresOriginalPoint()
    {
        var matrix =
            Matrix3x2.CreateScale(new Vector2(2, 3)) *
            Matrix3x2.CreateRotation(0.7f) *
            Matrix3x2.CreateTranslation(new Vector2(10, -4));

        var original = new Vector2(5, 8);

        Assert.True(matrix.TryInvert(out var inverse));

        var transformed = matrix.TransformPoint(original);
        var restored = inverse.TransformPoint(transformed);

        Assert.Equal(original.X, restored.X, 4);
        Assert.Equal(original.Y, restored.Y, 4);
    }

    [Fact]
    public void TryInvert_SingularMatrixReturnsFalse()
    {
        var matrix = Matrix3x2.CreateScale(
            new Vector2(0, 1));

        Assert.False(matrix.TryInvert(out _));
    }
}
