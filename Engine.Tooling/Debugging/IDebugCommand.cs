namespace Engine.Tooling.Debugging;

public interface IDebugCommand
{
    string Name { get; }

    string Description { get; }

    DebugCommandResult Execute(
        IReadOnlyList<string> arguments);
}