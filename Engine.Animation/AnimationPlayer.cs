namespace Engine.Animation;

public sealed class AnimationPlayer
{
    private AnimationClip _clip;
    private AnimationBindingSet? _bindings;
    public AnimationBindingSet? Bindings =>
    _bindings;
    public void SetBindings(
    AnimationBindingSet? bindings)
    {
        _bindings =
            bindings;
        ApplyCurrentPose();
    }

    public AnimationPlayer(
        AnimationClip clip)
    {
        ArgumentNullException.ThrowIfNull(
            clip);

        _clip =
            clip;
    }

    public AnimationClip Clip =>
        _clip;

    public AnimationLoopMode LoopMode { get; set; } =
        AnimationLoopMode.Once;

    public double Speed { get; set; } = 1.0;

    public double TimeSeconds { get; private set; }

    public double Progress =>
        TimeSeconds /
        _clip.DurationSeconds;

    public bool IsPlaying { get; private set; }

    public bool IsFinished { get; private set; }

    public event Action? Completed;

    public void SetClip(
        AnimationClip clip,
        bool play = false)
    {
        ArgumentNullException.ThrowIfNull(
            clip);

        _clip =
            clip;

        TimeSeconds =
            0.0;

        IsFinished =
            false;

        IsPlaying =
            play;
        ApplyCurrentPose();
    }

    public void Play()
    {
        if (IsFinished)
        {
            TimeSeconds =
                Speed >= 0.0
                    ? 0.0
                    : _clip.DurationSeconds;

            IsFinished =
                false;
        }

        IsPlaying =
            true;
        ApplyCurrentPose();
    }

    public void Pause()
    {
        IsPlaying =
            false;
    }

    public void Stop()
    {
        IsPlaying =
            false;

        IsFinished =
            false;

        TimeSeconds =
            0.0;
        ApplyCurrentPose();
    }

    public void Reset()
    {
        Stop();
    }

    public void Seek(
        double timeSeconds)
    {
        if (!double.IsFinite(
                timeSeconds))
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeSeconds));
        }

        TimeSeconds =
            Math.Clamp(
                timeSeconds,
                0.0,
                _clip.DurationSeconds);

        IsFinished =
            TimeSeconds >=
            _clip.DurationSeconds;

        if (IsFinished)
        {
            IsPlaying =
                false;
        }
        ApplyCurrentPose();
    }

    public void Update(
        double deltaSeconds)
    {
        if (!double.IsFinite(
                deltaSeconds) ||
            deltaSeconds < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deltaSeconds));
        }

        if (!IsPlaying ||
            deltaSeconds == 0.0)
        {
            return;
        }

        var delta =
            deltaSeconds *
            Speed;

        var nextTime =
            TimeSeconds +
            delta;

        switch (LoopMode)
        {
            case AnimationLoopMode.Once:
                UpdateOnce(
                    nextTime);
                break;

            case AnimationLoopMode.Loop:
                UpdateLoop(
                    nextTime);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(LoopMode),
                    LoopMode,
                    null);
        }
        ApplyCurrentPose();
    }

    private void ApplyCurrentPose()
    {
        _bindings?.Apply(
            _clip,
            TimeSeconds);
    }

    private void UpdateOnce(
        double nextTime)
    {
        if (Speed >= 0.0)
        {
            if (nextTime >=
                _clip.DurationSeconds)
            {
                TimeSeconds =
                    _clip.DurationSeconds;

                IsPlaying =
                    false;

                IsFinished =
                    true;

                Completed?.Invoke();

                return;
            }

            TimeSeconds =
                Math.Max(
                    0.0,
                    nextTime);

            return;
        }

        if (nextTime <= 0.0)
        {
            TimeSeconds =
                0.0;

            IsPlaying =
                false;

            IsFinished =
                true;

            Completed?.Invoke();

            return;
        }

        TimeSeconds =
            Math.Min(
                _clip.DurationSeconds,
                nextTime);
    }

    private void UpdateLoop(
        double nextTime)
    {
        var duration =
            _clip.DurationSeconds;

        TimeSeconds =
            nextTime %
            duration;

        if (TimeSeconds < 0.0)
        {
            TimeSeconds +=
                duration;
        }

        IsFinished =
            false;
    }
}