using Engine.Tooling.Profiling;

namespace Engine.Tooling.Debugging;

public sealed class MetricsDebugCommand :
    IDebugCommand
{
    private readonly Profiler _profiler;

    public MetricsDebugCommand(
        Profiler profiler)
    {
        ArgumentNullException.ThrowIfNull(
            profiler);

        _profiler = profiler;
    }

    public string Name =>
        "metrics";

    public string Description =>
        "Lists collected profiler metrics.";

    public DebugCommandResult Execute(
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 0)
        {
            return DebugCommandResult.Fail(
                "Usage: metrics");
        }

        var metrics =
            _profiler.Metrics
                .OrderBy(
                    metric => metric.Name,
                    StringComparer.Ordinal)
                .ToArray();

        if (metrics.Length == 0)
        {
            return DebugCommandResult.Ok(
                "No metrics collected.");
        }

        var lines =
            new List<string>(
                metrics.Length + 1)
            {
                $"Metrics ({metrics.Length}):"
            };

        foreach (var metric in metrics)
        {
            lines.Add(
                $"  {metric.Name} [{metric.Type}] = {metric.Value}");
        }

        return DebugCommandResult.Ok(
            string.Join(
                Environment.NewLine,
                lines));
    }
}