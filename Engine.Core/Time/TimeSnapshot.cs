namespace Engine.Core.Time;

public readonly record struct TimeSnapshot(
    Duration Delta,
    Duration Elapsed,
    Tick Tick);