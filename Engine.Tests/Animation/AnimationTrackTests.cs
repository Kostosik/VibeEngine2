using Engine.Animation;

namespace Engine.Tests.Animation;

public sealed class AnimationTrackTests
{
    private static readonly IAnimationInterpolator<float>
        Linear =
            new DelegateAnimationInterpolator<float>(
                static (from, to, amount) =>
                    from +
                    (to - from) *
                    (float)amount);

    [Fact]
    public void Sample_BeforeFirstKeyframe_ReturnsFirstValue()
    {
        var track =
            CreateTrack();

        Assert.Equal(
            10.0f,
            track.Sample(-1.0));
    }

    [Fact]
    public void Sample_AtKeyframe_ReturnsKeyframeValue()
    {
        var track =
            CreateTrack();

        Assert.Equal(
            20.0f,
            track.Sample(1.0));
    }

    [Fact]
    public void Sample_BetweenKeyframes_Interpolates()
    {
        var track =
            CreateTrack();

        Assert.Equal(
            15.0f,
            track.Sample(0.5),
            precision: 5);
    }

    [Fact]
    public void Sample_AfterLastKeyframe_ReturnsLastValue()
    {
        var track =
            CreateTrack();

        Assert.Equal(
            30.0f,
            track.Sample(5.0));
    }

    [Fact]
    public void Constructor_RejectsEmptyTrack()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new AnimationTrack<float>(
                    Array.Empty<AnimationKeyframe<float>>(),
                    Linear));
    }

    [Fact]
    public void Constructor_RejectsNonIncreasingKeyframes()
    {
        var keyframes =
            new[]
            {
                new AnimationKeyframe<float>(
                    1.0,
                    10.0f),

                new AnimationKeyframe<float>(
                    1.0,
                    20.0f)
            };

        Assert.Throws<ArgumentException>(
            () =>
                new AnimationTrack<float>(
                    keyframes,
                    Linear));
    }

    [Fact]
    public void Sample_UsesBinarySearchCompatibleOrdering()
    {
        var keyframes =
            new[]
            {
                new AnimationKeyframe<float>(
                    0.0,
                    0.0f),

                new AnimationKeyframe<float>(
                    1.0,
                    10.0f),

                new AnimationKeyframe<float>(
                    2.0,
                    20.0f),

                new AnimationKeyframe<float>(
                    3.0,
                    30.0f)
            };

        var track =
            new AnimationTrack<float>(
                keyframes,
                Linear);

        Assert.Equal(
            25.0f,
            track.Sample(2.5),
            precision: 5);
    }

    private static AnimationTrack<float> CreateTrack()
    {
        return new AnimationTrack<float>(
            new[]
            {
                new AnimationKeyframe<float>(
                    0.0,
                    10.0f),

                new AnimationKeyframe<float>(
                    1.0,
                    20.0f),

                new AnimationKeyframe<float>(
                    2.0,
                    30.0f)
            },
            Linear);
    }
}