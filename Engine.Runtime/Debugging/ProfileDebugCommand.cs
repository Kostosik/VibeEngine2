
using Engine.Tooling.Debugging;
using Engine.Tooling.Profiling;

namespace Engine.Runtime;

public sealed class ProfileDebugCommand : IDebugCommand
{
    private readonly EngineRuntime _runtime;

    public ProfileDebugCommand(EngineRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
    }

    public string Name => "profile";

    public string Description =>
        "Controls frame timing profiling and displays reports.";

    public DebugCommandResult Execute(
        IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (arguments.Count != 1)
        {
            return DebugCommandResult.Fail(
                "Usage: profile <start|stop|status|report|reset>");
        }

        return arguments[0].ToLowerInvariant() switch
        {
            "start" => Start(),
            "stop" => Stop(),
            "status" => Status(),
            "report" => Report(),
            "reset" => Reset(),
            _ => DebugCommandResult.Fail(
                "Unknown profile action. Use start, stop, status, report or reset.")
        };
    }

    private DebugCommandResult Start()
    {
        if (_runtime.IsProfiling)
        {
            return DebugCommandResult.Ok(
                "Profiling is already active.");
        }

        _runtime.StartProfiling();

        return DebugCommandResult.Ok(
            "Frame timing capture started.");
    }

    private DebugCommandResult Stop()
    {
        if (!_runtime.IsProfiling)
        {
            return DebugCommandResult.Ok(
                "Profiling is already stopped.");
        }

        _runtime.StopProfiling();

        return DebugCommandResult.Ok(
            "Frame timing capture stopped. Data preserved.");
    }

    private DebugCommandResult Status()
    {
        var profiler = _runtime.FrameTimingProfiler;

        return DebugCommandResult.Ok(
            $"Profiling: {(_runtime.IsProfiling ? "ON" : "OFF")}\n" +
            $"Samples: {profiler?.SampleCount ?? 0}\n" +
            $"Capacity: {profiler?.Capacity ?? 0}");
    }

    private DebugCommandResult Report()
    {
        var profiler = _runtime.FrameTimingProfiler;

        if (profiler is null || profiler.SampleCount == 0)
        {
            return DebugCommandResult.Ok(
                "No frame timing samples available. " +
                "Run 'profile start' and let the game render several frames.");
        }

        var statistics = profiler.GetStatistics();

        var interval = statistics.FrameInterval;

        var averageFps = interval.Average.TotalMilliseconds > 0
            ? 1000.0 / interval.Average.TotalMilliseconds
            : 0.0;

        var report =
            "FRAME TIMING REPORT\n" +
            $"Samples: {statistics.SampleCount}/{profiler.Capacity}\n" +
            $"Estimated average FPS: {averageFps:F1}\n\n" +
            Format("Frame Interval", statistics.FrameInterval) + "\n" +
            Format("Update CPU", statistics.Update) + "\n" +
            Format("FixedUpdate CPU", statistics.FixedUpdate) + "\n" +
            Format("Render CPU", statistics.Render) + "\n" +
            Format("EndFrame CPU", statistics.EndFrame);

        return DebugCommandResult.Ok(report);
    }

    private DebugCommandResult Reset()
    {
        _runtime.Profiler?.Clear();
        _runtime.FrameTimingProfiler?.Clear();

        return DebugCommandResult.Ok(
            _runtime.IsProfiling
                ? "Profiling data cleared. Capture remains active."
                : "Profiling data cleared.");
    }

    private static string Format(
        string name,
        TimingStatistics statistics)
    {
        return
            $"{name,-18} " +
            $"Avg {statistics.Average.TotalMilliseconds,8:F3} ms | " +
            $"Min {statistics.Minimum.TotalMilliseconds,8:F3} ms | " +
            $"Max {statistics.Maximum.TotalMilliseconds,8:F3} ms | " +
            $"P95 {statistics.P95.TotalMilliseconds,8:F3} ms | " +
            $"P99 {statistics.P99.TotalMilliseconds,8:F3} ms";
    }
}
