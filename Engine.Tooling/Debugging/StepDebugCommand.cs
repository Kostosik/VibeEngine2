namespace Engine.Tooling.Debugging;

public sealed class StepDebugCommand :
    IDebugCommand
{
    private readonly DebugExecutionController _execution;

    public StepDebugCommand(
        DebugExecutionController execution)
    {
        ArgumentNullException.ThrowIfNull(
            execution);

        _execution = execution;
    }

    public string Name =>
        "step";

    public string Description =>
        "Executes one fixed simulation tick.";

    public DebugCommandResult Execute(
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 0)
        {
            return DebugCommandResult.Fail(
                "Usage: step");
        }

        return _execution.Step();
    }
}