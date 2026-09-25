namespace Engine.Core.Diagnostics;

public interface IDiagnosticSink
{
    void Report(Diagnostic diagnostic);
}
