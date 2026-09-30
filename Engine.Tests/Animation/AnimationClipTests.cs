using Engine.Animation;

namespace Engine.Tests.Animation;

public sealed class AnimationClipTests
{
    private static readonly IAnimationInterpolator<float>
        Linear =
            new DelegateAnimationInterpolator<float>(
                static (from, to, amount) =>
                    from +
                    (to - from) *
                    (float)amount);

    [Fact]
    public void AddTrack_RegistersTrack()
    {
        var clip =
            new AnimationClip(
                2.0);

        var track =
            CreateTrack();

        var id =
            new AnimationTrackId(
                "opacity");

        clip.AddTrack(
            id,
            track);

        Assert.Equal(
            1,
            clip.TrackCount);

        Assert.Same(
            track,
            clip.GetTrack<float>(
                id));
    }

    [Fact]
    public void AddTrack_RejectsDuplicateId()
    {
        var clip =
            new AnimationClip(
                2.0);

        var id =
            new AnimationTrackId(
                "opacity");

        clip.AddTrack(
            id,
            CreateTrack());

        Assert.Throws<InvalidOperationException>(
            () =>
                clip.AddTrack(
                    id,
                    CreateTrack()));
    }

    [Fact]
    public void AddTrack_RejectsTrackOutsideClipDuration()
    {
        var clip =
            new AnimationClip(
                1.0);

        var track =
            new AnimationTrack<float>(
                new[]
                {
                    new AnimationKeyframe<float>(
                        0.0,
                        0.0f),

                    new AnimationKeyframe<float>(
                        2.0,
                        1.0f)
                },
                Linear);

        Assert.Throws<ArgumentException>(
            () =>
                clip.AddTrack(
                    new AnimationTrackId(
                        "opacity"),
                    track));
    }

    [Fact]
    public void Sample_ReturnsTypedTrackValue()
    {
        var clip =
            new AnimationClip(
                2.0);

        clip.AddTrack(
            new AnimationTrackId(
                "opacity"),
            CreateTrack());

        var value =
            clip.Sample<float>(
                new AnimationTrackId(
                    "opacity"),
                0.5);

        Assert.Equal(
            15.0f,
            value,
            precision: 5);
    }

    [Fact]
    public void GetTrack_RejectsWrongValueType()
    {
        var clip =
            new AnimationClip(
                2.0);

        clip.AddTrack(
            new AnimationTrackId(
                "opacity"),
            CreateTrack());

        Assert.Throws<InvalidOperationException>(
            () =>
                clip.GetTrack<int>(
                    new AnimationTrackId(
                        "opacity")));
    }

    [Fact]
    public void RemoveTrack_RemovesRegisteredTrack()
    {
        var clip =
            new AnimationClip(
                2.0);

        var id =
            new AnimationTrackId(
                "opacity");

        clip.AddTrack(
            id,
            CreateTrack());

        Assert.True(
            clip.RemoveTrack(
                id));

        Assert.False(
            clip.RemoveTrack(
                id));

        Assert.Equal(
            0,
            clip.TrackCount);
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