namespace Engine.Tooling.Debugging;

public sealed class HelpDebugCommand :
    IDebugCommand
{
    private readonly DebugCommandRegistry _registry;

    public HelpDebugCommand(
        DebugCommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(
            registry);

        _registry = registry;
    }

    public string Name =>
        "help";

    public string Description =>
        "Lists available debug commands.";

    public DebugCommandResult Execute(
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 0)
        {
            return DebugCommandResult.Fail(
                "Usage: help");
        }

        var commands =
            _registry.Commands
                .OrderBy(
                    command => command.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (commands.Length == 0)
        {
            return DebugCommandResult.Ok(
                "No debug commands registered.");
        }

        var lines =
            new List<string>(
                commands.Length + 1)
            {
            "Available commands:"
            };

        foreach (var command in commands)
        {
            lines.Add(
                $"  {command.Name} - {command.Description}");
        }

        return DebugCommandResult.Ok(
            string.Join(
                Environment.NewLine,
                lines));
    }


}