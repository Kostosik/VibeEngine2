namespace Engine.Animation;

public sealed class AnimationClip
{
    private readonly Dictionary<
        AnimationTrackId,
        IAnimationTrack> _tracks =
        new();

    public AnimationClip(
        double durationSeconds)
    {
        if (!double.IsFinite(
                durationSeconds) ||
            durationSeconds <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(durationSeconds),
                "Animation duration must be finite and greater than zero.");
        }

        DurationSeconds =
            durationSeconds;
    }

    public double DurationSeconds { get; }

    public int TrackCount =>
        _tracks.Count;

    public IReadOnlyDictionary<
        AnimationTrackId,
        IAnimationTrack> Tracks =>
        _tracks;

    public void AddTrack<T>(
        AnimationTrackId id,
        AnimationTrack<T> track)
    {
        ArgumentNullException.ThrowIfNull(
            track);

        if (track.StartTimeSeconds < 0.0 ||
            track.EndTimeSeconds >
            DurationSeconds)
        {
            throw new ArgumentException(
                "Track keyframes must fit within the animation clip duration.",
                nameof(track));
        }

        if (!_tracks.TryAdd(
                id,
                track))
        {
            throw new InvalidOperationException(
                $"Animation track '{id}' is already registered.");
        }
    }

    public bool RemoveTrack(
        AnimationTrackId id)
    {
        return _tracks.Remove(
            id);
    }

    public bool TryGetTrack<T>(
        AnimationTrackId id,
        out AnimationTrack<T>? track)
    {
        if (_tracks.TryGetValue(
                id,
                out var value) &&
            value is AnimationTrack<T> typed)
        {
            track =
                typed;

            return true;
        }

        track =
            null;

        return false;
    }

    public AnimationTrack<T> GetTrack<T>(
        AnimationTrackId id)
    {
        if (!_tracks.TryGetValue(
                id,
                out var track))
        {
            throw new KeyNotFoundException(
                $"Animation track '{id}' was not found.");
        }

        if (track is not AnimationTrack<T> typed)
        {
            throw new InvalidOperationException(
                $"Animation track '{id}' contains values of type " +
                $"'{track.ValueType.FullName}', not '{typeof(T).FullName}'.");
        }

        return typed;
    }

    public T Sample<T>(
        AnimationTrackId id,
        double timeSeconds)
    {
        return GetTrack<T>(
            id)
            .Sample(
                timeSeconds);
    }
}