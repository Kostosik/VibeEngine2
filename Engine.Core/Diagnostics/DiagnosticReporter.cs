using Engine.Core.Diagnostics;

namespace Engine.Core.Diagnostics;

public sealed class DiagnosticReporter
{
    private readonly IDiagnosticSink _sink;
    private readonly string _source;

    public DiagnosticReporter(
        IDiagnosticSink sink,
        string source)
    {
        ArgumentNullException.ThrowIfNull(sink);

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException(
                "Source cannot be empty.",
                nameof(source));

        _sink = sink;
        _source = source;
    }

    public void Trace(string code, string message)
    {
        Report(
            DiagnosticLevel.Trace,
            code,
            message);
    }

    public void Debug(string code, string message)
    {
        Report(
            DiagnosticLevel.Debug,
            code,
            message);
    }

    public void Info(string code, string message)
    {
        Report(
            DiagnosticLevel.Info,
            code,
            message);
    }

    public void Warning(string code, string message)
    {
        Report(
            DiagnosticLevel.Warning,
            code,
            message);
    }

    public void Error(string code, string message)
    {
        Report(
            DiagnosticLevel.Error,
            code,
            message);
    }

    public void Critical(string code, string message)
    {
        Report(
            DiagnosticLevel.Critical,
            code,
            message);
    }

    private void Report(
        DiagnosticLevel level,
        string code,
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var diagnostic = new Diagnostic(
            level,
            $"{_source}.{code}",
            message);

        _sink.Report(diagnostic);
    }
}