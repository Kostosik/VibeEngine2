namespace Engine.Tooling.Debugging;

public sealed class ResumeDebugCommand :
    IDebugCommand
{
    private readonly DebugExecutionController _execution;

    public ResumeDebugCommand(
        DebugExecutionController execution)
    {
        ArgumentNullException.ThrowIfNull(
            execution);

        _execution = execution;
    }

    public string Name =>
        "resume";

    public string Description =>
        "Resumes fixed simulation.";

    public DebugCommandResult Execute(
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 0)
        {
            return DebugCommandResult.Fail(
                "Usage: resume");
        }

        return _execution.Resume();
    }
}