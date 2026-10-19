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
        {
            throw new ArgumentException(
                "Source cannot be empty.",
                nameof(source));
        }

        _sink = sink;
        _source = source;
    }

    public void Trace(
        string code,
        string message,
        Exception? exception = null)
    {
        Report(DiagnosticLevel.Trace, code, message, exception);
    }

    public void Debug(
        string code,
        string message,
        Exception? exception = null)
    {
        Report(DiagnosticLevel.Debug, code, message, exception);
    }

    public void Info(
        string code,
        string message,
        Exception? exception = null)
    {
        Report(DiagnosticLevel.Info, code, message, exception);
    }

    public void Warning(
        string code,
        string message,
        Exception? exception = null)
    {
        Report(DiagnosticLevel.Warning, code, message, exception);
    }

    public void Error(
        string code,
        string message,
        Exception? exception = null)
    {
        Report(DiagnosticLevel.Error, code, message, exception);
    }

    public void Critical(
        string code,
        string message,
        Exception? exception = null)
    {
        Report(DiagnosticLevel.Critical, code, message, exception);
    }

    private void Report(
        DiagnosticLevel level,
        string code,
        string message,
        Exception? exception)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var diagnostic = new Diagnostic(
            level,
            $"{_source}.{code}",
            message,
            exception);

        _sink.Report(diagnostic);
    }
}