using Engine.Core.Diagnostics;


namespace Engine.Tests.Core.Diagnostics;

public sealed class DiagnosticReporterTests
{
    [Fact]
    public void Warning_AddsSourceToCode()
    {
        var collector = new DiagnosticCollector();

        var reporter = new DiagnosticReporter(
            collector,
            "ECS");

        reporter.Warning(
            "001",
            "Entity does not exist.");

        var diagnostic = Assert.Single(
            collector.Diagnostics);

        Assert.Equal(
            DiagnosticLevel.Warning,
            diagnostic.Level);

        Assert.Equal(
            "ECS.001",
            diagnostic.Code);

        Assert.Equal(
            "Entity does not exist.",
            diagnostic.Message);
    }

    [Fact]
    public void Error_CreatesErrorDiagnostic()
    {
        var collector = new DiagnosticCollector();

        var reporter = new DiagnosticReporter(
            collector,
            "WORLD");

        reporter.Error(
            "002",
            "Chunk is invalid.");

        var diagnostic = Assert.Single(
            collector.Diagnostics);

        Assert.Equal(
            DiagnosticLevel.Error,
            diagnostic.Level);

        Assert.Equal(
            "WORLD.002",
            diagnostic.Code);
    }

    [Fact]
    public void EmptySource_Throws()
    {
        var collector = new DiagnosticCollector();

        Assert.Throws<ArgumentException>(
            () => new DiagnosticReporter(
                collector,
                ""));
    }
}