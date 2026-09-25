using Engine.Core.Diagnostics;
using Engine.Core.Logging;

namespace Engine.Tooling.Debugging;

public sealed class ConsoleLogger :
    ILogger
{
    private readonly DebugConsole _console;

    public ConsoleLogger(
        DebugConsole console)
    {
        ArgumentNullException.ThrowIfNull(
            console);

        _console = console;
    }

    public void Trace(
        string message)
    {
        Write(
            DiagnosticLevel.Trace,
            message);
    }

    public void Debug(
        string message)
    {
        Write(
            DiagnosticLevel.Debug,
            message);
    }

    public void Info(
        string message)
    {
        Write(
            DiagnosticLevel.Info,
            message);
    }

    public void Warning(
        string message)
    {
        Write(
            DiagnosticLevel.Warning,
            message);
    }

    public void Error(
        string message)
    {
        Write(
            DiagnosticLevel.Error,
            message);
    }

    public void Critical(
        string message)
    {
        Write(
            DiagnosticLevel.Critical,
            message);
    }

    private void Write(
        DiagnosticLevel level,
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            message);

        _console.Write(
            level,
            message);
    }
}