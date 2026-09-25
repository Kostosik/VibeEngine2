using Engine.Core.Diagnostics;

namespace Engine.Core.Diagnostics;

public sealed class CompositeDiagnosticSink : IDiagnosticSink
{
    private readonly IReadOnlyList<IDiagnosticSink> _sinks;

    public CompositeDiagnosticSink(params IDiagnosticSink[] sinks)
    {
        ArgumentNullException.ThrowIfNull(sinks);

        _sinks = sinks;
    }

    public void Report(Diagnostic diagnostic)
    {
        foreach (var sink in _sinks)
        {
            sink.Report(diagnostic);
        }
    }
}