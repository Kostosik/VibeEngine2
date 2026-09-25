namespace Engine.Tooling.Debugging;

public sealed class PauseDebugCommand :
    IDebugCommand
{
    private readonly DebugExecutionController _execution;

    public PauseDebugCommand(
        DebugExecutionController execution)
    {
        ArgumentNullException.ThrowIfNull(
            execution);

        _execution = execution;
    }

    public string Name =>
        "pause";

    public string Description =>
        "Pauses fixed simulation.";

    public DebugCommandResult Execute(
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 0)
        {
            return DebugCommandResult.Fail(
                "Usage: pause");
        }

        return _execution.Pause();
    }
}