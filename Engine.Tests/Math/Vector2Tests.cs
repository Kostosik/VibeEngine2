using Engine.Core.Math;

namespace Engine.Tests.Core.Math;

public sealed class Vector2Tests
{
    [Fact]
    public void AddWorks()
    {
        var result =
            new Vector2(10, 20) +
            new Vector2(3, 4);

        Assert.Equal(
            new Vector2(13, 24),
            result);
    }

    [Fact]
    public void SubtractWorks()
    {
        var result =
            new Vector2(10, 20) -
            new Vector2(3, 4);

        Assert.Equal(
            new Vector2(7, 16),
            result);
    }

    [Fact]
    public void LengthSquared_ReturnsCorrectValue()
    {
        var vector = new Vector2(3, 4);

        Assert.Equal(25.0f, vector.LengthSquared());
    }

    [Fact]
    public void Length_ReturnsCorrectValue()
    {
        var vector = new Vector2(3, 4);

        Assert.Equal(5.0f, vector.Length());
    }

    [Fact]
    public void Normalize_ReturnsUnitVector()
    {
        var vector = new Vector2(3, 4);

        var result = vector.Normalize();

        Assert.Equal(0.6f, result.X, 5);
        Assert.Equal(0.8f, result.Y, 5);
    }
}