namespace Engine.Animation;

public interface IAnimationTrackBuilder
{
    int Count { get; }

    double StartTimeSeconds { get; }

    double EndTimeSeconds { get; }

    void AddTo(
        AnimationClip clip,
        AnimationTrackId id);
}