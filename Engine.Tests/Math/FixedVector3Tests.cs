
using Engine.Core.Math;

namespace Engine.Tests.Math;

public sealed class FixedVector3Tests
{
    [Fact]
    public void Addition_IsCorrect()
    {
        var first = new FixedVector3(
            Fixed32.FromInt(1),
            Fixed32.FromInt(2),
            Fixed32.FromInt(3));

        var second = new FixedVector3(
            Fixed32.FromInt(4),
            Fixed32.FromInt(5),
            Fixed32.FromInt(6));

        Assert.Equal(
            new FixedVector3(
                Fixed32.FromInt(5),
                Fixed32.FromInt(7),
                Fixed32.FromInt(9)),
            first + second);
    }

    [Fact]
    public void Subtraction_IsCorrect()
    {
        var first = new FixedVector3(
            Fixed32.FromInt(5),
            Fixed32.FromInt(7),
            Fixed32.FromInt(9));

        var second = new FixedVector3(
            Fixed32.FromInt(1),
            Fixed32.FromInt(2),
            Fixed32.FromInt(3));

        Assert.Equal(
            new FixedVector3(
                Fixed32.FromInt(4),
                Fixed32.FromInt(5),
                Fixed32.FromInt(6)),
            first - second);
    }

    [Fact]
    public void ScalarMultiplication_IsCorrect()
    {
        var value = new FixedVector3(
            Fixed32.FromInt(2),
            Fixed32.FromInt(3),
            Fixed32.FromInt(4));

        Assert.Equal(
            new FixedVector3(
                Fixed32.FromInt(4),
                Fixed32.FromInt(6),
                Fixed32.FromInt(8)),
            value * Fixed32.FromInt(2));
    }

    [Fact]
    public void Length_IsCorrect()
    {
        var value = new FixedVector3(
            Fixed32.FromInt(2),
            Fixed32.FromInt(3),
            Fixed32.FromInt(6));

        Assert.Equal(Fixed32.FromInt(7), value.Length());
    }

    [Fact]
    public void Dot_IsCorrect()
    {
        var first = new FixedVector3(
            Fixed32.FromInt(1),
            Fixed32.FromInt(2),
            Fixed32.FromInt(3));

        var second = new FixedVector3(
            Fixed32.FromInt(4),
            Fixed32.FromInt(5),
            Fixed32.FromInt(6));

        Assert.Equal(Fixed32.FromInt(32), first.Dot(second));
    }

    [Fact]
    public void Cross_IsCorrect()
    {
        var result = FixedVector3.UnitX.Cross(FixedVector3.UnitY);

        Assert.Equal(FixedVector3.UnitZ, result);
    }

    [Fact]
    public void Normalize_IsCorrect()
    {
        var value = new FixedVector3(
            Fixed32.FromInt(2),
            Fixed32.FromInt(3),
            Fixed32.FromInt(6));

        var normalized = value.Normalize();

        Assert.True(
            Fixed32.NearlyEqual(
                normalized.Length(),
                Fixed32.One,
                Fixed32.FromFloat(0.0001f)));
    }

    [Fact]
    public void Normalize_Zero_IsZero()
    {
        Assert.Equal(
            FixedVector3.Zero,
            FixedVector3.Zero.Normalize());
    }

    [Fact]
    public void Distance_IsCorrect()
    {
        var first = FixedVector3.Zero;

        var second = new FixedVector3(
            Fixed32.FromInt(2),
            Fixed32.FromInt(3),
            Fixed32.FromInt(6));

        Assert.Equal(
            Fixed32.FromInt(7),
            FixedVector3.Distance(first, second));
    }

    [Fact]
    public void Lerp_IsCorrect()
    {
        var result = FixedVector3.Lerp(
            FixedVector3.Zero,
            new FixedVector3(
                Fixed32.FromInt(10),
                Fixed32.FromInt(20),
                Fixed32.FromInt(30)),
            Fixed32.FromFloat(0.5f));

        Assert.Equal(
            new FixedVector3(
                Fixed32.FromInt(5),
                Fixed32.FromInt(10),
                Fixed32.FromInt(15)),
            result);
    }

    [Fact]
    public void WithZ_ChangesOnlyZ()
    {
        var value = new FixedVector3(
            Fixed32.FromInt(1),
            Fixed32.FromInt(2),
            Fixed32.FromInt(3));

        var result = value.WithZ(Fixed32.FromInt(10));

        Assert.Equal(Fixed32.FromInt(1), result.X);
        Assert.Equal(Fixed32.FromInt(2), result.Y);
        Assert.Equal(Fixed32.FromInt(10), result.Z);
    }
}
