namespace Engine.Animation;

public sealed class AnimationTrack<T> :
    IAnimationTrack
{
    private readonly AnimationKeyframe<T>[] _keyframes;
    private readonly IAnimationInterpolator<T> _interpolator;

    public AnimationTrack(
        IReadOnlyList<AnimationKeyframe<T>> keyframes,
        IAnimationInterpolator<T> interpolator)
    {
        ArgumentNullException.ThrowIfNull(
            keyframes);

        ArgumentNullException.ThrowIfNull(
            interpolator);

        if (keyframes.Count == 0)
        {
            throw new ArgumentException(
                "Animation track must contain at least one keyframe.",
                nameof(keyframes));
        }

        _keyframes =
            keyframes.ToArray();

        for (var i = 1;
             i < _keyframes.Length;
             i++)
        {
            if (_keyframes[i].TimeSeconds <=
                _keyframes[i - 1].TimeSeconds)
            {
                throw new ArgumentException(
                    "Keyframe times must be strictly increasing.",
                    nameof(keyframes));
            }
        }

        _interpolator =
            interpolator;
    }

    public int KeyframeCount =>
        _keyframes.Length;

    public double StartTimeSeconds =>
        _keyframes[0].TimeSeconds;

    public double EndTimeSeconds =>
        _keyframes[^1].TimeSeconds;

    public AnimationKeyframe<T> GetKeyframe(
        int index)
    {
        if ((uint)index >=
            (uint)_keyframes.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index));
        }

        return _keyframes[index];
    }

    public T Sample(
        double timeSeconds)
    {
        if (!double.IsFinite(
                timeSeconds))
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeSeconds));
        }

        if (_keyframes.Length == 1 ||
            timeSeconds <=
            _keyframes[0].TimeSeconds)
        {
            return _keyframes[0].Value;
        }

        if (timeSeconds >=
            _keyframes[^1].TimeSeconds)
        {
            return _keyframes[^1].Value;
        }

        var nextIndex =
            FindNextKeyframe(
                timeSeconds);

        var previous =
            _keyframes[
                nextIndex - 1];

        var next =
            _keyframes[
                nextIndex];

        var duration =
            next.TimeSeconds -
            previous.TimeSeconds;

        var amount =
            (timeSeconds -
             previous.TimeSeconds) /
            duration;

        return _interpolator.Interpolate(
            previous.Value,
            next.Value,
            amount);
    }

    private int FindNextKeyframe(
        double timeSeconds)
    {
        var low = 1;
        var high =
            _keyframes.Length - 1;

        while (low <= high)
        {
            var middle =
                low + (high - low) / 2;

            if (_keyframes[middle].TimeSeconds <=
                timeSeconds)
            {
                low =
                    middle + 1;
            }
            else
            {
                high =
                    middle - 1;
            }
        }

        return low;
    }

    public Type ValueType =>
    typeof(T);

    object IAnimationTrack.SampleObject(
        double timeSeconds)
    {
        return Sample(
            timeSeconds)!;
    }
}