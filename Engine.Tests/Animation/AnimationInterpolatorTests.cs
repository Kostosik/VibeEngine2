using Engine.Animation;

namespace Engine.Tests.Animation;

public sealed class AnimationInterpolatorTests
{
    [Fact]
    public void Step_ReturnsFromValue()
    {
        var interpolator =
            new StepAnimationInterpolator<int>();

        Assert.Equal(
            10,
            interpolator.Interpolate(
                10,
                20,
                0.75));
    }

    [Fact]
    public void FloatLinear_Interpolates()
    {
        var interpolator =
            new FloatLinearAnimationInterpolator();

        Assert.Equal(
            15.0f,
            interpolator.Interpolate(
                10.0f,
                20.0f,
                0.5),
            precision: 5);
    }

    [Fact]
    public void FloatLinear_HandlesEndpoints()
    {
        var interpolator =
            new FloatLinearAnimationInterpolator();

        Assert.Equal(
            10.0f,
            interpolator.Interpolate(
                10.0f,
                20.0f,
                0.0),
            precision: 5);

        Assert.Equal(
            20.0f,
            interpolator.Interpolate(
                10.0f,
                20.0f,
                1.0),
            precision: 5);
    }
}