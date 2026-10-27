using Engine.Animation;

namespace Engine.Tests.Animation;

public sealed class AnimationTrackBuilderTests
{
    [Fact]
    public void AddKeyframe_InsertsKeysInTimeOrder()
    {
        var builder = new AnimationTrackBuilder<float>(
            new FloatLinearAnimationInterpolator());

        builder.AddKeyframe(2.0, 20.0f);
        builder.AddKeyframe(0.0, 0.0f);

        var index = builder.AddKeyframe(1.0, 10.0f);

        Assert.Equal(1, index);
        Assert.Equal(3, builder.Count);

        Assert.Equal(0.0, builder.Keyframes[0].TimeSeconds);
        Assert.Equal(1.0, builder.Keyframes[1].TimeSeconds);
        Assert.Equal(2.0, builder.Keyframes[2].TimeSeconds);
    }

    [Fact]
    public void AddKeyframe_RejectsDuplicateTime()
    {
        var builder = new AnimationTrackBuilder<float>(
            new FloatLinearAnimationInterpolator());

        builder.AddKeyframe(1.0, 10.0f);

        Assert.Throws<ArgumentException>(
            () => builder.AddKeyframe(1.0, 20.0f));

        Assert.Equal(1, builder.Count);
        Assert.Equal(10.0f, builder.Keyframes[0].Value);
    }

    [Fact]
    public void MoveKeyframe_ReordersKeyAndPreservesValue()
    {
        var builder = new AnimationTrackBuilder<float>(
            new FloatLinearAnimationInterpolator());

        builder.AddKeyframe(0.0, 0.0f);
        builder.AddKeyframe(1.0, 10.0f);
        builder.AddKeyframe(2.0, 20.0f);

        var newIndex = builder.MoveKeyframe(1, 3.0);

        Assert.Equal(2, newIndex);
        Assert.Equal(3.0, builder.Keyframes[2].TimeSeconds);
        Assert.Equal(10.0f, builder.Keyframes[2].Value);
    }

    [Fact]
    public void MoveKeyframe_RejectsDuplicateTimeWithoutChangingTrack()
    {
        var builder = new AnimationTrackBuilder<float>(
            new FloatLinearAnimationInterpolator());

        builder.AddKeyframe(0.0, 0.0f);
        builder.AddKeyframe(1.0, 10.0f);

        Assert.Throws<ArgumentException>(
            () => builder.MoveKeyframe(0, 1.0));

        Assert.Equal(2, builder.Count);
        Assert.Equal(0.0, builder.Keyframes[0].TimeSeconds);
        Assert.Equal(1.0, builder.Keyframes[1].TimeSeconds);
    }

    [Fact]
    public void SetKeyframeValue_ChangesOnlyValue()
    {
        var builder = new AnimationTrackBuilder<float>(
            new FloatLinearAnimationInterpolator());

        builder.AddKeyframe(1.0, 10.0f);

        builder.SetKeyframeValue(0, 25.0f);

        Assert.Equal(1.0, builder.Keyframes[0].TimeSeconds);
        Assert.Equal(25.0f, builder.Keyframes[0].Value);
    }

    [Fact]
    public void RemoveKeyframe_RemovesSelectedKey()
    {
        var builder = new AnimationTrackBuilder<float>(
            new FloatLinearAnimationInterpolator());

        builder.AddKeyframe(0.0, 0.0f);
        builder.AddKeyframe(1.0, 10.0f);

        builder.RemoveKeyframe(0);

        Assert.Single(builder.Keyframes);
        Assert.Equal(1.0, builder.Keyframes[0].TimeSeconds);
    }

    [Fact]
    public void Build_CreatesUsableAnimationTrack()
    {
        var builder = new AnimationTrackBuilder<float>(
            new FloatLinearAnimationInterpolator());

        builder.AddKeyframe(0.0, 0.0f);
        builder.AddKeyframe(2.0, 20.0f);

        var track = builder.Build();

        Assert.Equal(10.0f, track.Sample(1.0), precision: 5);
    }
}