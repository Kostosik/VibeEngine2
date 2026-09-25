using Engine.Core.Diagnostics;

namespace Engine.Tooling.Debugging;

public sealed class ConsoleDiagnosticSink :
    IDiagnosticSink
{
    private readonly DebugConsole _console;

    public ConsoleDiagnosticSink(
        DebugConsole console)
    {
        ArgumentNullException.ThrowIfNull(
            console);

        _console = console;
    }

    public void Report(
        Diagnostic diagnostic)
    {
        _console.Write(
            diagnostic.Level,
            diagnostic.Message,
            diagnostic.Code);
    }
}