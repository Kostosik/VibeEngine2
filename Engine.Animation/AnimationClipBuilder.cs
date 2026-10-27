using System.Collections.ObjectModel;

namespace Engine.Animation;

public sealed class AnimationClipBuilder
{
    private readonly Dictionary<
        AnimationTrackId,
        IAnimationTrackBuilder> _tracks = new();

    private readonly ReadOnlyDictionary<
        AnimationTrackId,
        IAnimationTrackBuilder> _readOnlyTracks;

    public AnimationClipBuilder(
        double durationSeconds)
    {
        ValidateDuration(durationSeconds);

        DurationSeconds = durationSeconds;

        _readOnlyTracks =
            new ReadOnlyDictionary<
                AnimationTrackId,
                IAnimationTrackBuilder>(
                    _tracks);
    }

    public AnimationClipBuilder(AnimationClip clip)
    : this(
        (clip ?? throw new ArgumentNullException(nameof(clip)))
        .DurationSeconds)
    {
        foreach (var pair in clip.Tracks)
        {
            _tracks.Add(
                pair.Key,
                pair.Value.CreateBuilder());
        }
    }

    public double DurationSeconds { get; private set; }

    public int TrackCount =>
        _tracks.Count;

    public IReadOnlyDictionary<
        AnimationTrackId,
        IAnimationTrackBuilder> Tracks =>
            _readOnlyTracks;

    public void SetDuration(double durationSeconds)
    {
        ValidateDuration(durationSeconds);

        foreach (var pair in _tracks)
        {
            var track = pair.Value;

            if (track.Count > 0 &&
                track.EndTimeSeconds > durationSeconds)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(durationSeconds),
                    $"Track '{pair.Key}' extends beyond " +
                    "the requested clip duration.");
            }
        }

        DurationSeconds = durationSeconds;
    }

    public AnimationTrackBuilder<T> AddTrack<T>(
        AnimationTrackId id,
        IAnimationInterpolator<T> interpolator)
    {
        var builder =
            new AnimationTrackBuilder<T>(interpolator);

        AddTrack(id, builder);

        return builder;
    }

    public void AddTrack<T>(
        AnimationTrackId id,
        AnimationTrackBuilder<T> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.Count > 0 &&
            builder.EndTimeSeconds > DurationSeconds)
        {
            throw new ArgumentException(
                "Track keyframes must fit within " +
                "the animation clip duration.",
                nameof(builder));
        }

        if (!_tracks.TryAdd(id, builder))
        {
            throw new InvalidOperationException(
                $"Animation track '{id}' is already registered.");
        }
    }

    public bool RemoveTrack(AnimationTrackId id)
    {
        return _tracks.Remove(id);
    }

    public bool TryGetTrack<T>(
        AnimationTrackId id,
        out AnimationTrackBuilder<T>? track)
    {
        if (_tracks.TryGetValue(id, out var value) &&
            value is AnimationTrackBuilder<T> typed)
        {
            track = typed;
            return true;
        }

        track = null;
        return false;
    }

    public AnimationTrackBuilder<T> GetTrack<T>(
        AnimationTrackId id)
    {
        if (!_tracks.TryGetValue(id, out var value))
        {
            throw new KeyNotFoundException(
                $"Animation track '{id}' was not found.");
        }

        if (value is not AnimationTrackBuilder<T> typed)
        {
            throw new InvalidOperationException(
                $"Animation track '{id}' has a different value type.");
        }

        return typed;
    }

    public AnimationClip Build()
    {
        var clip = new AnimationClip(DurationSeconds);

        foreach (var pair in _tracks)
        {
            // An empty track may exist while the user is editing.
            // It cannot be compiled into an AnimationTrack yet.
            if (pair.Value.Count == 0)
            {
                continue;
            }

            pair.Value.AddTo(
                clip,
                pair.Key);
        }

        return clip;
    }

    private static void ValidateDuration(double durationSeconds)
    {
        if (!double.IsFinite(durationSeconds) ||
            durationSeconds <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(durationSeconds),
                "Animation duration must be finite and greater than zero.");
        }
    }
}