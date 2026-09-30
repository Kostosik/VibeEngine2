namespace Engine.Animation;

public interface IAnimationBinding
{
    AnimationTrackId TrackId { get; }

    void Apply(
        AnimationClip clip,
        double timeSeconds);
}