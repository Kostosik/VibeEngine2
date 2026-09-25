using Engine.Core.Math;

namespace Engine.Tests.Math;

public sealed class FixedVector2Tests
{
    [Fact]
    public void Addition_IsCorrect()
    {
        var first =
            new FixedVector2(
                Fixed32.FromFloat(1.5f),
                Fixed32.FromFloat(2.0f));

        var second =
            new FixedVector2(
                Fixed32.FromFloat(2.5f),
                Fixed32.FromFloat(3.0f));

        var result =
            first + second;

        Assert.Equal(
            Fixed32.FromFloat(4.0f),
            result.X);

        Assert.Equal(
            Fixed32.FromFloat(5.0f),
            result.Y);
    }

    [Fact]
    public void Subtraction_IsCorrect()
    {
        var first =
            new FixedVector2(
                Fixed32.FromFloat(5.0f),
                Fixed32.FromFloat(4.0f));

        var second =
            new FixedVector2(
                Fixed32.FromFloat(2.0f),
                Fixed32.FromFloat(1.5f));

        var result =
            first - second;

        Assert.Equal(
            Fixed32.FromFloat(3.0f),
            result.X);

        Assert.Equal(
            Fixed32.FromFloat(2.5f),
            result.Y);
    }

    [Fact]
    public void ScalarMultiplication_IsCorrect()
    {
        var value =
            new FixedVector2(
                Fixed32.FromFloat(2.0f),
                Fixed32.FromFloat(3.0f));

        var result =
            value *
            Fixed32.FromFloat(2.0f);

        Assert.Equal(
            Fixed32.FromFloat(4.0f),
            result.X);

        Assert.Equal(
            Fixed32.FromFloat(6.0f),
            result.Y);
    }

    [Fact]
    public void Length_IsCorrect()
    {
        var value =
            new FixedVector2(
                Fixed32.FromInt(3),
                Fixed32.FromInt(4));

        Assert.Equal(
            Fixed32.FromInt(5),
            value.Length());
    }

    [Fact]
    public void Dot_IsCorrect()
    {
        var first =
            new FixedVector2(
                Fixed32.FromInt(2),
                Fixed32.FromInt(3));

        var second =
            new FixedVector2(
                Fixed32.FromInt(4),
                Fixed32.FromInt(5));

        Assert.Equal(
            Fixed32.FromInt(23),
            first.Dot(second));
    }

    [Fact]
    public void Normalize_IsCorrect()
    {
        var value =
            new FixedVector2(
                Fixed32.FromInt(3),
                Fixed32.FromInt(4));

        var normalized =
            value.Normalize();

        var tolerance =
            Fixed32.FromFloat(0.0001f);

        Assert.True(
            Fixed32.NearlyEqual(
                normalized.Length(),
                Fixed32.One,
                tolerance));
    }

    [Fact]
    public void Normalize_Zero_IsZero()
    {
        Assert.Equal(
            FixedVector2.Zero.Normalize(),
            FixedVector2.Zero);
    }

    [Fact]
    public void Distance_IsCorrect()
    {
        var first =
            new FixedVector2(
                Fixed32.FromInt(0),
                Fixed32.FromInt(0));

        var second =
            new FixedVector2(
                Fixed32.FromInt(3),
                Fixed32.FromInt(4));

        Assert.Equal(
            Fixed32.FromInt(5),
            FixedVector2.Distance(
                first,
                second));
    }

    [Fact]
    public void Lerp_IsCorrect()
    {
        var first =
            new FixedVector2(
                Fixed32.Zero,
                Fixed32.Zero);

        var second =
            new FixedVector2(
                Fixed32.FromInt(10),
                Fixed32.FromInt(20));

        var result =
            FixedVector2.Lerp(
                first,
                second,
                Fixed32.FromFloat(0.5f));

        Assert.Equal(
            Fixed32.FromInt(5),
            result.X);

        Assert.Equal(
            Fixed32.FromInt(10),
            result.Y);
    }
}