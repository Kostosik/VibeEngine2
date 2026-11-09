
using Engine.Animation;
using Engine.Core.Math;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Sprites;

namespace Engine.Tests.Graphics2D;

public sealed class SpriteAnimationPlaybackTests
{
    [Fact]
    public void Pause_PreventsFrameAdvancement()
    {
        var animation = CreateAnimation();

        animation.Pause();
        animation.Update(0.15f);

        Assert.Equal(0, animation.CurrentFrame);
        Assert.False(animation.IsPlaying);
    }

    [Fact]
    public void Play_ResumesPlayback()
    {
        var animation = CreateAnimation();

        animation.Pause();
        animation.Play();
        animation.Update(0.15f);

        Assert.Equal(1, animation.CurrentFrame);
        Assert.True(animation.IsPlaying);
    }

    [Fact]
    public void Stop_ResetsAnimation()
    {
        var animation = CreateAnimation();

        animation.Update(0.15f);
        animation.Stop();

        Assert.Equal(0, animation.CurrentFrame);
        Assert.False(animation.IsPlaying);
    }

    [Fact]
    public void Seek_ChangesCurrentFrame()
    {
        var animation = CreateAnimation();

        animation.Seek(0.15);

        Assert.Equal(1, animation.CurrentFrame);
    }

    [Fact]
    public void Speed_ChangesPlaybackRate()
    {
        var animation = CreateAnimation();

        animation.Speed = 2.0;
        animation.Update(0.06f);

        Assert.Equal(1, animation.CurrentFrame);
    }

    [Fact]
    public void Speed_RejectsNonFiniteValues()
    {
        var animation = CreateAnimation();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => animation.Speed = double.NaN);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => animation.Speed = double.PositiveInfinity);
    }

    [Fact]
    public void Completed_IsForwardedFromAnimationPlayer()
    {
        var animation = CreateAnimation(loop: false);
        var completedCount = 0;

        animation.Completed += () => completedCount++;

        animation.Update(0.25f);

        Assert.True(animation.IsFinished);
        Assert.False(animation.IsPlaying);
        Assert.Equal(1, completedCount);
    }

    [Fact]
    public void Update_RaisesFrameChanged()
    {
        var animation = CreateAnimation();
        var changedFrames = new List<int>();

        animation.FrameChanged += changedFrames.Add;

        animation.Update(0.11f);

        Assert.Equal(new[] { 1 }, changedFrames);
    }

    [Fact]
    public void Update_LargeDelta_RaisesEventForEveryTransition()
    {
        var animation = CreateAnimation();
        var changedFrames = new List<int>();

        animation.FrameChanged += changedFrames.Add;

        animation.Update(0.45f);

        Assert.Equal(new[] { 1, 0, 1, 0 }, changedFrames);
    }

    [Fact]
    public void Update_Loop_NotifiesWhenWrappingToFirstFrame()
    {
        var animation = CreateAnimation();
        var changedFrames = new List<int>();

        animation.Update(0.11f);
        animation.FrameChanged += changedFrames.Add;

        animation.Update(0.11f);

        Assert.Equal(new[] { 0 }, changedFrames);
    }

    [Fact]
    public void Update_Once_DoesNotNotifyBeyondLastFrame()
    {
        var animation = CreateAnimation(loop: false);
        var changedFrames = new List<int>();

        animation.FrameChanged += changedFrames.Add;

        animation.Update(0.5f);

        Assert.Equal(new[] { 1 }, changedFrames);
    }

    [Fact]
    public void Seek_RaisesFrameChanged()
    {
        var animation = CreateAnimation();
        var changedFrames = new List<int>();

        animation.FrameChanged += changedFrames.Add;

        animation.Seek(0.15);

        Assert.Equal(1, animation.CurrentFrame);
        Assert.Equal(new[] { 1 }, changedFrames);
    }

    [Fact]
    public void Seek_DoesNotRaiseEvent_WhenFrameDoesNotChange()
    {
        var animation = CreateAnimation();
        animation.Seek(0.15);

        var changedFrames = new List<int>();
        animation.FrameChanged += changedFrames.Add;

        animation.Seek(0.18);

        Assert.Equal(1, animation.CurrentFrame);
        Assert.Empty(changedFrames);
    }

    [Fact]
    public void Stop_RaisesFrameChanged_WhenReturningToFirstFrame()
    {
        var animation = CreateAnimation();
        animation.Seek(0.15);

        var changedFrames = new List<int>();
        animation.FrameChanged += changedFrames.Add;

        animation.Stop();

        Assert.Equal(0, animation.CurrentFrame);
        Assert.Equal(new[] { 0 }, changedFrames);
    }

    [Fact]
    public void Reset_RaisesFrameChanged_WhenReturningToFirstFrame()
    {
        var animation = CreateAnimation();
        animation.Seek(0.15);

        var changedFrames = new List<int>();
        animation.FrameChanged += changedFrames.Add;

        animation.Reset();

        Assert.Equal(0, animation.CurrentFrame);
        Assert.Equal(new[] { 0 }, changedFrames);
        Assert.True(animation.IsPlaying);
    }

    [Fact]
    public void Play_RaisesFrameChanged_WhenRestartingFinishedAnimation()
    {
        var animation = CreateAnimation(loop: false);
        animation.Update(0.25f);

        var changedFrames = new List<int>();
        animation.FrameChanged += changedFrames.Add;

        animation.Play();

        Assert.Equal(0, animation.CurrentFrame);
        Assert.Equal(new[] { 0 }, changedFrames);
        Assert.True(animation.IsPlaying);
    }

    [Fact]
    public void Update_NegativeSpeed_FromExactFrameBoundary_NotifiesTransition()
    {
        var animation = CreateAnimation();

        animation.Seek((double)0.1f);
        animation.Speed = -1.0;

        var changedFrames = new List<int>();
        animation.FrameChanged += changedFrames.Add;

        animation.Update(0.02f);

        Assert.Equal(0, animation.CurrentFrame);
        Assert.Equal(new[] { 0 }, changedFrames);
    }

    [Fact]
    public void Update_NegativeSpeed_LargeDelta_RaisesEveryTransition()
    {
        var animation = CreateAnimation();

        animation.Seek(0.05);
        animation.Speed = -1.0;

        var changedFrames = new List<int>();
        animation.FrameChanged += changedFrames.Add;

        animation.Update(0.45f);

        Assert.Equal(new[] { 1, 0, 1, 0 }, changedFrames);
    }

    [Fact]
    public void SwitchingFromLoopToOnce_ReversePlayback_FinishesAtStart()
    {
        var animation = CreateAnimation();

        animation.Seek(0.15);
        animation.LoopMode = AnimationLoopMode.Once;
        animation.Speed = -1.0;

        var changedFrames = new List<int>();
        animation.FrameChanged += changedFrames.Add;

        animation.Update(0.2f);

        Assert.Equal(0, animation.CurrentFrame);
        Assert.Equal(new[] { 0 }, changedFrames);
        Assert.True(animation.IsFinished);
    }

    [Fact]
    public void Update_NegativeSpeed_NotifiesBackwardTransitions()
    {
        var animation = CreateAnimation();

        animation.Seek(0.15);
        animation.Speed = -1.0;

        var changedFrames = new List<int>();
        animation.FrameChanged += changedFrames.Add;

        animation.Update(0.11f);

        Assert.Equal(new[] { 0 }, changedFrames);
    }

    [Fact]
    public void Play_RestartsFinishedAnimation()
    {
        var animation = CreateAnimation(loop: false);

        animation.Update(0.25f);
        animation.Play();

        Assert.False(animation.IsFinished);
        Assert.True(animation.IsPlaying);
        Assert.Equal(0, animation.CurrentFrame);
    }

    private static SpriteAnimation CreateAnimation(bool loop = true)
    {
        var texture = new TextureHandle(1);

        var frames = new[]
        {
            new Sprite(
                texture,
                new Rectangle(0, 0, 0.5f, 1),
                new Vector2(16, 16)),

            new Sprite(
                texture,
                new Rectangle(0.5f, 0, 0.5f, 1),
                new Vector2(16, 16))
        };

        return new SpriteAnimation(
            frames,
            0.1f,
            loop);
    }
}
