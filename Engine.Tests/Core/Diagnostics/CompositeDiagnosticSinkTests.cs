using Engine.Core.Diagnostics;


namespace Engine.Tests.Core.Diagnostics;

public sealed class CompositeDiagnosticSinkTests
{
    [Fact]
    public void Report_ForwardsDiagnosticToAllSinks()
    {
        var first = new DiagnosticCollector();
        var second = new DiagnosticCollector();

        var sink = new CompositeDiagnosticSink(
            first,
            second);

        var diagnostic = new Diagnostic(
            DiagnosticLevel.Error,
            "TEST001",
            "Test error.");

        sink.Report(diagnostic);

        Assert.Single(first.Diagnostics);
        Assert.Single(second.Diagnostics);

        Assert.Equal(diagnostic, first.Diagnostics[0]);
        Assert.Equal(diagnostic, second.Diagnostics[0]);
    }
}