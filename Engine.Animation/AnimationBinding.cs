namespace Engine.Animation;

public sealed class AnimationBinding<T> :
    IAnimationBinding
{
    private readonly Action<T> _apply;

    public AnimationBinding(
        AnimationTrackId trackId,
        Action<T> apply)
    {
        ArgumentNullException.ThrowIfNull(
            apply);

        TrackId =
            trackId;

        _apply =
            apply;
    }

    public AnimationTrackId TrackId { get; }

    public void Apply(
        AnimationClip clip,
        double timeSeconds)
    {
        ArgumentNullException.ThrowIfNull(
            clip);

        if (!clip.TryGetTrack<T>(
                TrackId,
                out var track) ||
            track is null)
        {
            return;
        }

        _apply(
            track.Sample(
                timeSeconds));
    }
}