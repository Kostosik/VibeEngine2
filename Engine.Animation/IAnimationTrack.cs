namespace Engine.Animation;

public interface IAnimationTrack
{
    Type ValueType { get; }

    double StartTimeSeconds { get; }

    double EndTimeSeconds { get; }

    object SampleObject(
        double timeSeconds);
}