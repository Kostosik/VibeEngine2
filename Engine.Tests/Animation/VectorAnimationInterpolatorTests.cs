using Engine.Animation;
using Engine.Core.Math;

namespace Engine.Tests.Animation;

public sealed class VectorAnimationInterpolatorTests
{
    [Fact]
    public void InterpolatesVector2Linearly()
    {
        var interpolator = new Vector2LinearAnimationInterpolator();

        var result = interpolator.Interpolate(
            new Vector2(0.0f, 10.0f),
            new Vector2(10.0f, 30.0f),
            0.5);

        Assert.Equal(new Vector2(5.0f, 20.0f), result);
    }

    [Fact]
    public void InterpolatesVector3Linearly()
    {
        var interpolator = new Vector3LinearAnimationInterpolator();

        var result = interpolator.Interpolate(
            new Vector3(0.0f, 10.0f, 20.0f),
            new Vector3(10.0f, 30.0f, 40.0f),
            0.5);

        Assert.Equal(
            new Vector3(5.0f, 20.0f, 30.0f),
            result);
    }
}