
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

        Assert.Equal(new Vector2(13, 24), result);
    }

    [Fact]
    public void SubtractWorks()
    {
        var result =
            new Vector2(10, 20) -
            new Vector2(3, 4);

        Assert.Equal(new Vector2(7, 16), result);
    }

    [Fact]
    public void UnaryMinusWorks()
    {
        Assert.Equal(
            new Vector2(-3, -4),
            -new Vector2(3, 4));
    }

    [Fact]
    public void UnitVectors_ReturnExpectedValues()
    {
        Assert.Equal(new Vector2(1, 0), Vector2.UnitX);
        Assert.Equal(new Vector2(0, 1), Vector2.UnitY);
    }

    [Fact]
    public void IsFinite_DetectsInvalidComponents()
    {
        Assert.True(new Vector2(1, 2).IsFinite);
        Assert.False(new Vector2(float.NaN, 2).IsFinite);
        Assert.False(new Vector2(1, float.PositiveInfinity).IsFinite);
    }

    [Fact]
    public void LengthSquared_ReturnsCorrectValue()
    {
        Assert.Equal(
            25.0f,
            new Vector2(3, 4).LengthSquared());
    }

    [Fact]
    public void Length_ReturnsCorrectValue()
    {
        Assert.Equal(
            5.0f,
            new Vector2(3, 4).Length());
    }

    [Fact]
    public void Normalize_ReturnsUnitVector()
    {
        var result = new Vector2(3, 4).Normalize();

        Assert.Equal(0.6f, result.X, 5);
        Assert.Equal(0.8f, result.Y, 5);
    }

    [Fact]
    public void Normalize_ZeroReturnsZero()
    {
        Assert.Equal(
            Vector2.Zero,
            Vector2.Zero.Normalize());
    }

    [Fact]
    public void Dot_ReturnsCorrectValue()
    {
        var result = Vector2.Dot(
            new Vector2(2, 3),
            new Vector2(4, 5));

        Assert.Equal(23.0f, result);
    }

    [Fact]
    public void Cross_ReturnsSignedScalar()
    {
        Assert.Equal(
            1.0f,
            Vector2.Cross(Vector2.UnitX, Vector2.UnitY));

        Assert.Equal(
            -1.0f,
            Vector2.Cross(Vector2.UnitY, Vector2.UnitX));
    }

    [Fact]
    public void DistanceSquared_ReturnsCorrectValue()
    {
        Assert.Equal(
            25.0f,
            Vector2.DistanceSquared(
                Vector2.Zero,
                new Vector2(3, 4)));
    }

    [Fact]
    public void Distance_ReturnsCorrectValue()
    {
        Assert.Equal(
            5.0f,
            Vector2.Distance(
                Vector2.Zero,
                new Vector2(3, 4)));
    }

    [Fact]
    public void Lerp_ReturnsMidpoint()
    {
        var result = Vector2.Lerp(
            Vector2.Zero,
            new Vector2(10, 20),
            0.5f);

        Assert.Equal(new Vector2(5, 10), result);
    }

    [Fact]
    public void Lerp_DoesNotClampAmount()
    {
        var result = Vector2.Lerp(
            Vector2.Zero,
            new Vector2(10, 20),
            2.0f);

        Assert.Equal(new Vector2(20, 40), result);
    }

    [Fact]
    public void Min_ReturnsComponentMinimum()
    {
        var result = Vector2.Min(
            new Vector2(5, 2),
            new Vector2(3, 8));

        Assert.Equal(new Vector2(3, 2), result);
    }

    [Fact]
    public void Max_ReturnsComponentMaximum()
    {
        var result = Vector2.Max(
            new Vector2(5, 2),
            new Vector2(3, 8));

        Assert.Equal(new Vector2(5, 8), result);
    }

    [Fact]
    public void Clamp_RestrictsComponents()
    {
        var result = Vector2.Clamp(
            new Vector2(-2, 8),
            new Vector2(0, 1),
            new Vector2(5, 6));

        Assert.Equal(new Vector2(0, 6), result);
    }

    [Fact]
    public void Clamp_InvalidRangeThrows()
    {
        Assert.Throws<ArgumentException>(() =>
            Vector2.Clamp(
                Vector2.Zero,
                new Vector2(5, 0),
                new Vector2(1, 10)));
    }

    [Fact]
    public void MultiplyComponents_MultipliesEachAxis()
    {
        var result = Vector2.MultiplyComponents(
            new Vector2(2, 3),
            new Vector2(4, 5));

        Assert.Equal(new Vector2(8, 15), result);
    }

    [Fact]
    public void DivideComponents_DividesEachAxis()
    {
        var result = Vector2.DivideComponents(
            new Vector2(8, 15),
            new Vector2(2, 3));

        Assert.Equal(new Vector2(4, 5), result);
    }

    [Fact]
    public void DivideComponents_ZeroComponentThrows()
    {
        Assert.Throws<DivideByZeroException>(() =>
            Vector2.DivideComponents(
                new Vector2(10, 20),
                new Vector2(2, 0)));
    }

    [Fact]
    public void Perpendicular_RotatesDirectionBy90Degrees()
    {
        var result = Vector2.UnitX.Perpendicular();

        Assert.Equal(Vector2.UnitY, result);
    }

    [Fact]
    public void Rotated_RotatesCounterClockwise()
    {
        var result = Vector2.UnitX.Rotated(MathF.PI / 2.0f);

        Assert.Equal(0.0f, result.X, 5);
        Assert.Equal(1.0f, result.Y, 5);
    }

    [Fact]
    public void DivideByZeroThrows()
    {
        Assert.Throws<DivideByZeroException>(() =>
            new Vector2(10, 20) / 0.0f);
    }
}
