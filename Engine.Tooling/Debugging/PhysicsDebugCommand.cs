using Engine.Tooling.DebugVisualization;

namespace Engine.Tooling.Debugging;

public sealed class PhysicsDebugCommand :
    IDebugCommand
{
    private readonly PhysicsDebugVisualizer _visualizer;

    public PhysicsDebugCommand(
        PhysicsDebugVisualizer visualizer)
    {
        ArgumentNullException.ThrowIfNull(
            visualizer);

        _visualizer = visualizer;
    }

    public string Name =>
        "physics.debug";

    public string Description =>
        "Enables or disables physics debug visualization.";

    public DebugCommandResult Execute(
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 1)
        {
            return DebugCommandResult.Fail(
                "Usage: physics.debug <on|off>");
        }

        switch (arguments[0].ToLowerInvariant())
        {
            case "on":
                _visualizer.Enabled = true;

                return DebugCommandResult.Ok(
                    "Physics debug visualization enabled.");

            case "off":
                _visualizer.Enabled = false;

                return DebugCommandResult.Ok(
                    "Physics debug visualization disabled.");

            default:
                return DebugCommandResult.Fail(
                    "Usage: physics.debug <on|off>");
        }
    }
}