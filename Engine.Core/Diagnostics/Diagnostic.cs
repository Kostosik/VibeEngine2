namespace Engine.Core.Diagnostics;

public readonly record struct Diagnostic(
    DiagnosticLevel Level,
    string Code,
    string Message)
{
    public Exception? Exception { get; init; }

    public Diagnostic(
        DiagnosticLevel level,
        string code,
        string message,
        Exception? exception)
        : this(level, code, message)
    {
        Exception = exception;
    }
}