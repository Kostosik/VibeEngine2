using Engine.Core.Math;

namespace Engine.Tests.Math;

public sealed class Fixed32Tests
{
    [Fact]
    public void Addition_IsCorrect()
    {
        var a =
            Fixed32.FromFloat(1.5f);

        var b =
            Fixed32.FromFloat(2.25f);

        var result =
            a + b;

        Assert.Equal(
            3.75f,
            result.ToFloat());
    }

    [Fact]
    public void Multiplication_IsCorrect()
    {
        var a =
            Fixed32.FromFloat(1.5f);

        var b =
            Fixed32.FromFloat(2.0f);

        var result =
            a * b;

        Assert.Equal(
            3.0f,
            result.ToFloat());
    }

    [Fact]
    public void Division_IsCorrect()
    {
        var a =
            Fixed32.FromFloat(3.0f);

        var b =
            Fixed32.FromFloat(2.0f);

        var result =
            a / b;

        Assert.Equal(
            1.5f,
            result.ToFloat());
    }

    [Fact]
    public void SameValues_AreEqual()
    {
        var a =
            Fixed32.FromInt(10);

        var b =
            Fixed32.FromInt(10);

        Assert.Equal(a, b);
    }

    [Fact]
    public void DivisionByZero_Throws()
    {
        var value =
            Fixed32.FromInt(10);

        Assert.Throws<DivideByZeroException>(
            () => value / Fixed32.Zero);
    }

    [Fact]
    public void Sqrt_IsCorrect()
    {
        var value =
            Fixed32.FromInt(9);

        var result =
            Fixed32.Sqrt(value);

        Assert.Equal(
            Fixed32.FromInt(3),
            result);
    }

    [Fact]
    public void Sqrt_HandlesFraction()
    {
        var value =
            Fixed32.FromFloat(2.25f);

        var result =
            Fixed32.Sqrt(value);

        Assert.Equal(
            Fixed32.FromFloat(1.5f),
            result);
    }

    [Fact]
    public void Sqrt_Zero_IsZero()
    {
        Assert.Equal(
            Fixed32.Zero,
            Fixed32.Sqrt(Fixed32.Zero));
    }

    [Fact]
    public void Sqrt_Negative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Fixed32.Sqrt(
                Fixed32.FromInt(-1)));
    }
}