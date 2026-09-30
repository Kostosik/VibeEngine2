using Engine.Animation;

namespace Engine.Tests.Animation;

public sealed class AnimationPlayerTests
{
    [Fact]
    public void Update_AdvancesTime()
    {
        var clip =
            new AnimationClip(
                2.0);

        var player =
            new AnimationPlayer(
                clip);

        player.Play();

        player.Update(
            0.5);

        Assert.Equal(
            0.5,
            player.TimeSeconds,
            precision: 10);

        Assert.True(
            player.IsPlaying);
    }

    [Fact]
    public void Update_Once_StopsAtEnd()
    {
        var clip =
            new AnimationClip(
                2.0);

        var player =
            new AnimationPlayer(
                clip);

        var completed =
            0;

        player.Completed +=
            () =>
            {
                completed++;
            };

        player.Play();
        player.Update(
            3.0);

        Assert.Equal(
            2.0,
            player.TimeSeconds);

        Assert.False(
            player.IsPlaying);

        Assert.True(
            player.IsFinished);

        Assert.Equal(
            1,
            completed);
    }

    [Fact]
    public void Update_Loop_WrapsTime()
    {
        var clip =
            new AnimationClip(
                2.0);

        var player =
            new AnimationPlayer(
                clip)
            {
                LoopMode =
                    AnimationLoopMode.Loop
            };

        player.Play();

        player.Update(
            2.5);

        Assert.Equal(
            0.5,
            player.TimeSeconds,
            precision: 10);

        Assert.True(
            player.IsPlaying);

        Assert.False(
            player.IsFinished);
    }

    [Fact]
    public void Pause_PreventsTimeAdvance()
    {
        var clip =
            new AnimationClip(
                2.0);

        var player =
            new AnimationPlayer(
                clip);

        player.Play();

        player.Update(
            0.5);

        player.Pause();

        player.Update(
            0.5);

        Assert.Equal(
            0.5,
            player.TimeSeconds,
            precision: 10);
    }

    [Fact]
    public void Stop_ResetsPlayback()
    {
        var clip =
            new AnimationClip(
                2.0);

        var player =
            new AnimationPlayer(
                clip);

        player.Play();

        player.Update(
            0.5);

        player.Stop();

        Assert.Equal(
            0.0,
            player.TimeSeconds);

        Assert.False(
            player.IsPlaying);

        Assert.False(
            player.IsFinished);
    }

    [Fact]
    public void SetClip_ResetsPlayback()
    {
        var first =
            new AnimationClip(
                2.0);

        var second =
            new AnimationClip(
                5.0);

        var player =
            new AnimationPlayer(
                first);

        player.Play();
        player.Update(
            1.0);

        player.SetClip(
            second);

        Assert.Same(
            second,
            player.Clip);

        Assert.Equal(
            0.0,
            player.TimeSeconds);

        Assert.False(
            player.IsPlaying);

        Assert.False(
            player.IsFinished);
    }

    [Fact]
    public void Constructor_RejectsInvalidDuration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new AnimationClip(
                    0.0));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new AnimationClip(
                    double.NaN));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new AnimationClip(
                    double.PositiveInfinity));
    }
}