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
}