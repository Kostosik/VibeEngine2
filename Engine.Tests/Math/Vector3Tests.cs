
using Engine.Core.Math;

namespace Engine.Tests.Math;

public sealed class Vector3Tests
{
    [Fact]
    public void Addition_IsCorrect()
    {
        var result =
            new Vector3(1, 2, 3) +
            new Vector3(4, 5, 6);

        Assert.Equal(
            new Vector3(5, 7, 9),
            result);
    }

    [Fact]
    public void Subtraction_IsCorrect()
    {
        var result =
            new Vector3(5, 7, 9) -
            new Vector3(1, 2, 3);

        Assert.Equal(
            new Vector3(4, 5, 6),
            result);
    }

    [Fact]
    public void Length_IsCorrect()
    {
        var value =
            new Vector3(2, 3, 6);

        Assert.Equal(
            7.0f,
            value.Length());
    }

    [Fact]
    public void Normalize_IsCorrect()
    {
        var value =
            new Vector3(3, 0, 4);

        var result =
            value.Normalize();

        Assert.Equal(0.6f, result.X, 5);
        Assert.Equal(0.0f, result.Y, 5);
        Assert.Equal(0.8f, result.Z, 5);
    }

    [Fact]
    public void Normalize_Zero_IsZero()
    {
        Assert.Equal(
            Vector3.Zero,
            Vector3.Zero.Normalize());
    }

    [Fact]
    public void Dot_IsCorrect()
    {
        var result =
            Vector3.Dot(
                new Vector3(1, 2, 3),
                new Vector3(4, 5, 6));

        Assert.Equal(32.0f, result);
    }

    [Fact]
    public void Cross_IsCorrect()
    {
        var result =
            Vector3.Cross(
                Vector3.UnitX,
                Vector3.UnitY);

        Assert.Equal(
            Vector3.UnitZ,
            result);
    }

    [Fact]
    public void Distance_IsCorrect()
    {
        var result =
            Vector3.Distance(
                Vector3.Zero,
                new Vector3(3, 4, 0));

        Assert.Equal(5.0f, result);
    }

    [Fact]
    public void Lerp_IsCorrect()
    {
        var result =
            Vector3.Lerp(
                Vector3.Zero,
                new Vector3(10, 20, 30),
                0.5f);

        Assert.Equal(
            new Vector3(5, 10, 15),
            result);
    }
}
