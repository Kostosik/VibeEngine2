namespace Engine.Tooling.Debugging;

public readonly record struct DebugCommandResult(
    bool Success,
    string Message)
{
    public static DebugCommandResult Ok(
        string message)
    {
        return new DebugCommandResult(
            true,
            message);
    }

    public static DebugCommandResult Fail(
        string message)
    {
        return new DebugCommandResult(
            false,
            message);
    }
}