using Engine.Core.Math;

namespace Engine.Tests.Math;

public sealed class Fixed64Tests
{
    [Fact]
    public void FromInt_AndToInt_RoundTrip()
    {
        var value = Fixed64.FromInt(123);

        Assert.Equal(123, value.ToInt());
    }

    [Fact]
    public void FromRatio_UsesDeterministicRounding()
    {
        Assert.Equal(
            1_431_655_765L,
            Fixed64.FromRatio(1, 3).RawValue);

        Assert.Equal(
            -1_431_655_765L,
            Fixed64.FromRatio(-1, 3).RawValue);
    }

    [Fact]
    public void FromRatio_MidpointRoundsAwayFromZero()
    {
        var denominator = Fixed64.Scale * 2;

        Assert.Equal(
            1,
            Fixed64.FromRatio(1, denominator).RawValue);

        Assert.Equal(
            -1,
            Fixed64.FromRatio(-1, denominator).RawValue);
    }

    [Fact]
    public void FromRatio_DivisionByZero_Throws()
    {
        Assert.Throws<DivideByZeroException>(
            () => Fixed64.FromRatio(1, 0));
    }

    [Fact]
    public void AdditionAndSubtraction_AreCorrect()
    {
        var left = Fixed64.FromInt(10);
        var right = Fixed64.FromInt(3);

        Assert.Equal(
            Fixed64.FromInt(13),
            left + right);

        Assert.Equal(
            Fixed64.FromInt(7),
            left - right);
    }

    [Fact]
    public void Multiplication_IsCorrect()
    {
        var left = Fixed64.FromRatio(3, 2);
        var right = Fixed64.FromInt(2);

        Assert.Equal(
            Fixed64.FromInt(3),
            left * right);
    }

    [Fact]
    public void Division_IsCorrect()
    {
        var left = Fixed64.FromInt(3);
        var right = Fixed64.FromInt(2);

        Assert.Equal(
            Fixed64.FromRatio(3, 2),
            left / right);
    }

    [Fact]
    public void Multiplication_MidpointRoundsAwayFromZero()
    {
        var halfRaw = Fixed64.FromRaw(
            Fixed64.Scale / 2);

        var positive = Fixed64.FromRaw(1);
        var negative = Fixed64.FromRaw(-1);

        Assert.Equal(
            1,
            (positive * halfRaw).RawValue);

        Assert.Equal(
            -1,
            (negative * halfRaw).RawValue);
    }

    [Fact]
    public void Division_MidpointRoundsAwayFromZero()
    {
        var divisor = Fixed64.FromInt(2);

        Assert.Equal(
            1,
            (Fixed64.Epsilon / divisor).RawValue);

        Assert.Equal(
            -1,
            (-Fixed64.Epsilon / divisor).RawValue);
    }

    [Fact]
    public void DivisionByZero_Throws()
    {
        Assert.Throws<DivideByZeroException>(
            () => Fixed64.One / Fixed64.Zero);
    }

    [Fact]
    public void FromDouble_RoundsMidpointAwayFromZero()
    {
        var halfRaw = 0.5d / Fixed64.Scale;

        Assert.Equal(
            1,
            Fixed64.FromDouble(halfRaw).RawValue);

        Assert.Equal(
            -1,
            Fixed64.FromDouble(-halfRaw).RawValue);
    }

    [Fact]
    public void FromDouble_NonFiniteValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Fixed64.FromDouble(double.NaN));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Fixed64.FromDouble(double.PositiveInfinity));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Fixed64.FromDouble(double.NegativeInfinity));
    }

    [Fact]
    public void FromDouble_OutOfRange_Throws()
    {
        Assert.Throws<OverflowException>(
            () => Fixed64.FromDouble(2_147_483_648d));
    }

    [Fact]
    public void FromDouble_MinimumValue_IsSupported()
    {
        var value = Fixed64.FromDouble(-2_147_483_648d);

        Assert.Equal(
            long.MinValue,
            value.RawValue);
    }

    [Fact]
    public void FromInt_OutOfRange_Throws()
    {
        Assert.Throws<OverflowException>(
            () => Fixed64.FromInt(2_147_483_648L));
    }

    [Fact]
    public void FloorCeilingAndRound_AreCorrect()
    {
        var positive = Fixed64.FromDouble(1.75);
        var negative = Fixed64.FromDouble(-1.75);

        Assert.Equal(1, positive.ToInt());
        Assert.Equal(-1, negative.ToInt());

        Assert.Equal(1, positive.FloorToInt());
        Assert.Equal(-2, negative.FloorToInt());

        Assert.Equal(2, positive.CeilingToInt());
        Assert.Equal(-1, negative.CeilingToInt());

        Assert.Equal(2, positive.RoundToInt());
        Assert.Equal(-2, negative.RoundToInt());
    }

    [Fact]
    public void ArithmeticOverflow_Throws()
    {
        Assert.Throws<OverflowException>(
            () => Fixed64.MaxValue + Fixed64.Epsilon);

        Assert.Throws<OverflowException>(
            () => Fixed64.MinValue - Fixed64.Epsilon);

        Assert.Throws<OverflowException>(
            () => Fixed64.MaxValue * Fixed64.FromInt(2));

        Assert.Throws<OverflowException>(
            () => Fixed64.One / Fixed64.Epsilon);
    }

    [Fact]
    public void NegationAndAbs_MinValue_Throw()
    {
        Assert.Throws<OverflowException>(
            () => -Fixed64.MinValue);

        Assert.Throws<OverflowException>(
            () => Fixed64.Abs(Fixed64.MinValue));
    }

    [Fact]
    public void Clamp_ValidatesRange()
    {
        Assert.Equal(
            Fixed64.FromInt(2),
            Fixed64.Clamp(
                Fixed64.One,
                Fixed64.FromInt(2),
                Fixed64.FromInt(5)));

        Assert.Throws<ArgumentException>(
            () => Fixed64.Clamp(
                Fixed64.One,
                Fixed64.FromInt(5),
                Fixed64.FromInt(2)));
    }

    [Fact]
    public void Sqrt_Zero_ReturnsZero()
    {
        Assert.Equal(
            Fixed64.Zero,
            Fixed64.Sqrt(Fixed64.Zero));
    }

    [Fact]
    public void Sqrt_Integer_ReturnsExactResult()
    {
        Assert.Equal(
            Fixed64.FromInt(3),
            Fixed64.Sqrt(Fixed64.FromInt(9)));
    }

    [Fact]
    public void Sqrt_Fraction_ReturnsExactResult()
    {
        Assert.Equal(
            Fixed64.FromRatio(3, 2),
            Fixed64.Sqrt(Fixed64.FromRatio(9, 4)));
    }

    [Fact]
    public void Sqrt_Two_ReturnsDeterministicFloor()
    {
        var result = Fixed64.Sqrt(Fixed64.FromInt(2));

        Assert.Equal(
            6_074_000_999L,
            result.RawValue);
    }

    [Fact]
    public void Sqrt_ResultIsAlwaysFloored()
    {
        var input = Fixed64.FromInt(2);
        var result = Fixed64.Sqrt(input);

        var root = (UInt128)(ulong)result.RawValue;

        var scaledInput =
            (UInt128)(ulong)input.RawValue <<
            Fixed64.FractionalBits;

        Assert.True(root * root <= scaledInput);
        Assert.True((root + 1) * (root + 1) > scaledInput);
    }

    [Fact]
    public void Sqrt_MaxValue_DoesNotOverflow()
    {
        var result = Fixed64.Sqrt(Fixed64.MaxValue);

        Assert.True(result > Fixed64.Zero);
    }

    [Fact]
    public void Sqrt_NegativeValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Fixed64.Sqrt(Fixed64.FromInt(-1)));
    }

    [Fact]
    public void ArithmeticSequence_ProducesExpectedRawValue()
    {
        var value = Fixed64.FromRatio(7, 3);

        value *= Fixed64.FromInt(3);

        Assert.Equal(
            30_064_771_071L,
            value.RawValue);
    }
}