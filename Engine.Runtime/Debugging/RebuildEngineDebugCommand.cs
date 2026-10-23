using Engine.Tooling.Debugging;

namespace Engine.Runtime;

/// <summary>
/// Rebuilds the engine runtime from source and updates
/// the DLLs used by the current game project.
/// </summary>
public sealed class RebuildEngineDebugCommand :
    IDebugCommand,
    IDisposable
{
    private readonly EngineRebuildService _service = new();

    public string Name => "rebuild-engine";

    public string Description =>
        "Rebuilds engine runtime and updates project DLLs.";

    public DebugCommandResult Execute(
        IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (arguments.Count > 2)
        {
            return DebugCommandResult.Fail(
                "Usage: rebuild-engine \"<source directory>\" [Debug|Release]");
        }

        var sourceDirectory =
            Environment.GetEnvironmentVariable(
                "VIBEENGINE_SOURCE_DIR");

        var configuration = "Debug";

        if (arguments.Count == 1 &&
            IsConfiguration(arguments[0]))
        {
            configuration = arguments[0];
        }
        else if (arguments.Count >= 1)
        {
            sourceDirectory = arguments[0];

            if (arguments.Count == 2)
            {
                configuration = arguments[1];
            }
        }

        if (string.IsNullOrWhiteSpace(sourceDirectory))
        {
            return DebugCommandResult.Fail(
                "Engine source directory is required. " +
                "Set VIBEENGINE_SOURCE_DIR or pass the source path.");
        }

        if (!IsConfiguration(configuration))
        {
            return DebugCommandResult.Fail(
                "Configuration must be Debug or Release.");
        }

        if (!_service.TryStart(
                sourceDirectory,
                configuration,
                Environment.CurrentDirectory,
                out var message))
        {
            return DebugCommandResult.Fail(message);
        }

        return DebugCommandResult.Ok(message);
    }

    internal void DrainOutput(DebugConsole? console)
    {
        _service.DrainTo(console);
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    private static bool IsConfiguration(string value)
    {
        return string.Equals(
                   value,
                   "Debug",
                   StringComparison.OrdinalIgnoreCase)
               || string.Equals(
                   value,
                   "Release",
                   StringComparison.OrdinalIgnoreCase);
    }
}