using Engine.Core.Diagnostics;

namespace Engine.Tooling.Console;

public readonly record struct ConsoleMessage(
    DateTime Timestamp,
    DiagnosticLevel Level,
    string Message,
    string? Code)
{
    public Exception? Exception { get; init; }

    public ConsoleMessage(
        DateTime timestamp,
        DiagnosticLevel level,
        string message,
        string? code,
        Exception? exception)
        : this(timestamp, level, message, code)
    {
        Exception = exception;
    }
}