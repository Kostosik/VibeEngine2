namespace Engine.Animation;

public interface IAnimationTrack
{
    IAnimationTrackBuilder CreateBuilder();
    Type ValueType { get; }

    double StartTimeSeconds { get; }

    double EndTimeSeconds { get; }

    int KeyframeCount { get; }

    Type InterpolatorType { get; }

    double GetKeyframeTimeSeconds(int index);

    object? GetKeyframeValue(int index);

    object SampleObject(
        double timeSeconds);
}