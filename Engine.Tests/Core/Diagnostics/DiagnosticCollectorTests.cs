using Engine.Core.Diagnostics;

namespace Engine.Tests.Core.Diagnostics; 
public sealed class DiagnosticCollectorTests 
{ 
    [Fact]
    public void Report_AddsDiagnostic() { var collector = new DiagnosticCollector(); var diagnostic = new Diagnostic(DiagnosticLevel.Warning, "TEST001", "Test warning."); collector.Report(diagnostic); Assert.Single(collector.Diagnostics); Assert.Equal(diagnostic, collector.Diagnostics[0]); } [Fact] public void Clear_RemovesDiagnostics() { var collector = new DiagnosticCollector(); collector.Report(new Diagnostic(DiagnosticLevel.Error, "TEST001", "Test error.")); collector.Clear(); Assert.Empty(collector.Diagnostics); Assert.Equal(0, collector.Count); } }