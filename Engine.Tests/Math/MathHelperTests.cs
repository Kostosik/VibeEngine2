using Engine.Core.Math;

namespace Engine.Tests.Math;

public sealed class MathHelperTests
{
    [Fact]
    public void AngleConversion_ConvertsDegreesAndRadians()
    {
        Assert.Equal(
            MathF.PI,
            MathHelper.DegreesToRadians(180),
            5);

        Assert.Equal(
            180.0f,
            MathHelper.RadiansToDegrees(MathF.PI),
            5);
    }

    [Fact]
    public void WrapAngle_ProducesExpectedRange()
    {
        Assert.Equal(
            -MathF.PI,
            MathHelper.WrapAngle(MathF.PI),
            5);

        Assert.Equal(
            0.0f,
            MathHelper.WrapAngle(MathF.PI * 2.0f),
            5);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => MathHelper.WrapAngle(float.PositiveInfinity));
    }

    [Fact]
    public void Interpolation_UsesExpectedBehavior()
    {
        Assert.Equal(
            15.0f,
            MathHelper.Lerp(0, 10, 1.5f));

        Assert.Equal(
            5.0f,
            MathHelper.SmoothStep(0, 10, 0.5f));

        Assert.Equal(
            10.0f,
            MathHelper.SmoothStep(0, 10, 2.0f));
    }
}