
namespace Engine.Tooling.Profiling;

public readonly record struct TimingStatistics(
    int SampleCount,
    TimeSpan Average,
    TimeSpan Minimum,
    TimeSpan Maximum,
    TimeSpan P95,
    TimeSpan P99);

public readonly record struct FrameTimingStatistics(
    int SampleCount,
    TimingStatistics FrameInterval,
    TimingStatistics Update,
    TimingStatistics FixedUpdate,
    TimingStatistics Render,
    TimingStatistics EndFrame);
