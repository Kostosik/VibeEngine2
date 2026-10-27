using Engine.Animation;

namespace Engine.Tests.Animation;

public sealed class AnimationClipBuilderTests
{
    [Fact]
    public void AddTrack_CreatesEditableTrack()
    {
        var builder = new AnimationClipBuilder(2.0);

        var id = new AnimationTrackId("opacity");

        var track = builder.AddTrack(
            id,
            new FloatLinearAnimationInterpolator());

        track.AddKeyframe(0.0, 0.0f);
        track.AddKeyframe(2.0, 1.0f);

        Assert.Equal(1, builder.TrackCount);
        Assert.Same(track, builder.GetTrack<float>(id));
    }

    [Fact]
    public void Build_CreatesClipWithMultipleTrackTypes()
    {
        var builder = new AnimationClipBuilder(2.0);

        var opacityId = new AnimationTrackId("opacity");
        var frameId = new AnimationTrackId("frame");

        var opacity = builder.AddTrack(
            opacityId,
            new FloatLinearAnimationInterpolator());

        opacity.AddKeyframe(0.0, 0.0f);
        opacity.AddKeyframe(2.0, 1.0f);

        var frame = builder.AddTrack(
            frameId,
            new StepAnimationInterpolator<int>());

        frame.AddKeyframe(0.0, 0);
        frame.AddKeyframe(1.0, 1);

        var clip = builder.Build();

        Assert.Equal(2.0, clip.DurationSeconds);
        Assert.Equal(2, clip.TrackCount);

        Assert.Equal(
            0.5f,
            clip.Sample<float>(opacityId, 1.0),
            precision: 5);

        Assert.Equal(
            1,
            clip.Sample<int>(frameId, 1.5));
    }

    [Fact]
    public void AddTrack_RejectsDuplicateId()
    {
        var builder = new AnimationClipBuilder(2.0);
        var id = new AnimationTrackId("opacity");

        builder.AddTrack(
            id,
            new FloatLinearAnimationInterpolator());

        Assert.Throws<InvalidOperationException>(
            () => builder.AddTrack(
                id,
                new FloatLinearAnimationInterpolator()));
    }

    [Fact]
    public void SetDuration_RejectsDurationBeforeLastKey()
    {
        var builder = new AnimationClipBuilder(3.0);

        var track = builder.AddTrack(
            new AnimationTrackId("opacity"),
            new FloatLinearAnimationInterpolator());

        track.AddKeyframe(0.0, 0.0f);
        track.AddKeyframe(3.0, 1.0f);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => builder.SetDuration(2.0));

        Assert.Equal(3.0, builder.DurationSeconds);
    }

    [Fact]
    public void RemoveTrack_RemovesTrackFromBuiltClip()
    {
        var builder = new AnimationClipBuilder(2.0);
        var id = new AnimationTrackId("opacity");

        var track = builder.AddTrack(
            id,
            new FloatLinearAnimationInterpolator());

        track.AddKeyframe(0.0, 0.0f);

        Assert.True(builder.RemoveTrack(id));
        Assert.False(builder.RemoveTrack(id));

        Assert.Equal(0, builder.TrackCount);
        Assert.Equal(0, builder.Build().TrackCount);
    }

    [Fact]
    public void GetTrack_RejectsWrongValueType()
    {
        var builder = new AnimationClipBuilder(2.0);
        var id = new AnimationTrackId("opacity");

        builder.AddTrack(
            id,
            new FloatLinearAnimationInterpolator());

        Assert.Throws<InvalidOperationException>(
            () => builder.GetTrack<int>(id));
    }
}