using Engine.Animation;

namespace Engine.Tests.Animation;

public sealed class AnimationBindingTests
{
    private static readonly IAnimationInterpolator<float>
        Linear =
            new FloatLinearAnimationInterpolator();

    [Fact]
    public void Apply_SamplesTrackAndPassesValueToTarget()
    {
        var clip =
            new AnimationClip(
                2.0);

        var trackId =
            new AnimationTrackId(
                "value");

        clip.AddTrack(
            trackId,
            new AnimationTrack<float>(
                new[]
                {
                    new AnimationKeyframe<float>(
                        0.0,
                        10.0f),

                    new AnimationKeyframe<float>(
                        1.0,
                        20.0f)
                },
                Linear));

        var value =
            0.0f;

        var bindings =
            new AnimationBindingSet();

        bindings.Add<float>(
            trackId,
            result =>
            {
                value =
                    result;
            });

        bindings.Apply(
            clip,
            0.5);

        Assert.Equal(
            15.0f,
            value,
            precision: 5);
    }

    [Fact]
    public void Apply_MissingTrack_DoesNothing()
    {
        var clip =
            new AnimationClip(
                1.0);

        var called =
            false;

        var bindings =
            new AnimationBindingSet();

        bindings.Add<float>(
            new AnimationTrackId(
                "missing"),
            _ =>
            {
                called = true;
            });

        bindings.Apply(
            clip,
            0.5);

        Assert.False(
            called);
    }

    [Fact]
    public void Add_RejectsDuplicateTrackBinding()
    {
        var bindings =
            new AnimationBindingSet();

        var id =
            new AnimationTrackId(
                "value");

        bindings.Add<float>(
            id,
            _ =>
            {
            });

        Assert.Throws<InvalidOperationException>(
            () =>
                bindings.Add<float>(
                    id,
                    _ =>
                    {
                    }));
    }

    [Fact]
    public void Remove_RemovesBinding()
    {
        var bindings =
            new AnimationBindingSet();

        var id =
            new AnimationTrackId(
                "value");

        bindings.Add<float>(
            id,
            _ =>
            {
            });

        Assert.True(
            bindings.Remove(
                id));

        Assert.False(
            bindings.Remove(
                id));

        Assert.Equal(
            0,
            bindings.Count);
    }

    [Fact]
    public void Apply_SupportsMultipleTrackTypes()
    {
        var clip =
            new AnimationClip(
                1.0);

        var floatId =
            new AnimationTrackId(
                "value");

        var intId =
            new AnimationTrackId(
                "index");

        clip.AddTrack(
            floatId,
            new AnimationTrack<float>(
                new[]
                {
                    new AnimationKeyframe<float>(
                        0.0,
                        1.0f),

                    new AnimationKeyframe<float>(
                        1.0,
                        3.0f)
                },
                Linear));

        clip.AddTrack(
            intId,
            new AnimationTrack<int>(
                new[]
                {
                    new AnimationKeyframe<int>(
                        0.0,
                        5),

                    new AnimationKeyframe<int>(
                        1.0,
                        10)
                },
                new StepAnimationInterpolator<int>()));

        var floatValue =
            0.0f;

        var intValue =
            0;

        var bindings =
            new AnimationBindingSet();

        bindings.Add<float>(
            floatId,
            value =>
            {
                floatValue =
                    value;
            });

        bindings.Add<int>(
            intId,
            value =>
            {
                intValue =
                    value;
            });

        bindings.Apply(
            clip,
            0.5);

        Assert.Equal(
            2.0f,
            floatValue,
            precision: 5);

        Assert.Equal(
            5,
            intValue);
    }
}