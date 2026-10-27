using System.Collections.ObjectModel;

namespace Engine.Animation;

public sealed class AnimationTrackBuilder<T> :
    IAnimationTrackBuilder
{
    private readonly List<AnimationKeyframe<T>> _keyframes;
    private readonly ReadOnlyCollection<AnimationKeyframe<T>>
        _readOnlyKeyframes;

    private readonly IAnimationInterpolator<T> _interpolator;

    public AnimationTrackBuilder(
        IAnimationInterpolator<T> interpolator,
        IReadOnlyList<AnimationKeyframe<T>>? keyframes = null)
    {
        ArgumentNullException.ThrowIfNull(interpolator);

        _interpolator = interpolator;
        _keyframes = new List<AnimationKeyframe<T>>();

        if (keyframes is not null)
        {
            for (var i = 0; i < keyframes.Count; i++)
            {
                var keyframe = keyframes[i];

                if (i > 0 &&
                    keyframe.TimeSeconds <=
                    keyframes[i - 1].TimeSeconds)
                {
                    throw new ArgumentException(
                        "Keyframe times must be strictly increasing.",
                        nameof(keyframes));
                }

                _keyframes.Add(keyframe);
            }
        }

        _readOnlyKeyframes = _keyframes.AsReadOnly();
    }



    public IReadOnlyList<AnimationKeyframe<T>> Keyframes =>
        _readOnlyKeyframes;

    public int Count =>
        _keyframes.Count;

    public int AddKeyframe(
        double timeSeconds,
        T value)
    {
        var keyframe = new AnimationKeyframe<T>(
            timeSeconds,
            value);

        var index = FindInsertionIndex(timeSeconds);

        if (index < _keyframes.Count &&
            _keyframes[index].TimeSeconds == timeSeconds)
        {
            throw new ArgumentException(
                "A keyframe already exists at this time.",
                nameof(timeSeconds));
        }

        _keyframes.Insert(index, keyframe);

        return index;
    }

    public double StartTimeSeconds =>
    _keyframes.Count == 0
        ? 0.0
        : _keyframes[0].TimeSeconds;

    public double EndTimeSeconds =>
        _keyframes.Count == 0
            ? 0.0
            : _keyframes[^1].TimeSeconds;

    void IAnimationTrackBuilder.AddTo(
        AnimationClip clip,
        AnimationTrackId id)
    {
        ArgumentNullException.ThrowIfNull(clip);

        clip.AddTrack(
            id,
            Build());
    }

    public void SetKeyframeValue(
        int index,
        T value)
    {
        ValidateIndex(index);

        _keyframes[index] = new AnimationKeyframe<T>(
            _keyframes[index].TimeSeconds,
            value);
    }

    public int MoveKeyframe(
        int index,
        double newTimeSeconds)
    {
        ValidateIndex(index);

        var keyframe = new AnimationKeyframe<T>(
            newTimeSeconds,
            _keyframes[index].Value);

        for (var i = 0; i < _keyframes.Count; i++)
        {
            if (i != index &&
                _keyframes[i].TimeSeconds == newTimeSeconds)
            {
                throw new ArgumentException(
                    "A keyframe already exists at this time.",
                    nameof(newTimeSeconds));
            }
        }

        _keyframes.RemoveAt(index);

        var newIndex = FindInsertionIndex(newTimeSeconds);

        _keyframes.Insert(newIndex, keyframe);

        return newIndex;
    }

    public void RemoveKeyframe(int index)
    {
        ValidateIndex(index);

        _keyframes.RemoveAt(index);
    }

    public AnimationTrack<T> Build()
    {
        return new AnimationTrack<T>(
            _keyframes,
            _interpolator);
    }

    private int FindInsertionIndex(double timeSeconds)
    {
        var low = 0;
        var high = _keyframes.Count;

        while (low < high)
        {
            var middle = low + (high - low) / 2;

            if (_keyframes[middle].TimeSeconds < timeSeconds)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)_keyframes.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}