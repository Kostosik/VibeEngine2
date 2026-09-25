using Engine.Core.Diagnostics;

namespace Engine.Core.Diagnostics;

public sealed class DiagnosticCollector : IDiagnosticSink
{
    private readonly List<Diagnostic> _diagnostics = new();

    public int Count => _diagnostics.Count;

    public IReadOnlyList<Diagnostic> Diagnostics =>
        _diagnostics;

    public void Report(Diagnostic diagnostic)
    {
        _diagnostics.Add(diagnostic);
    }

    public void Clear()
    {
        _diagnostics.Clear();
    }
}