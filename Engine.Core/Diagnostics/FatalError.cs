namespace Engine.Core.Diagnostics;

public sealed class FatalError
{
    private readonly DiagnosticReporter _reporter;

    public FatalError(
        DiagnosticReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(
            reporter);

        _reporter = reporter;
    }

    public void Throw(
        string code,
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            code);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            message);

        _reporter.Critical(
            code,
            message);

        throw new FatalEngineException(
            $"{code}: {message}");
    }

    public void Throw(
        string code,
        string message,
        Exception innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            code);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            message);

        ArgumentNullException.ThrowIfNull(
            innerException);

        _reporter.Critical(
            code,
            message);

        throw new FatalEngineException(
            $"{code}: {message}",
            innerException);
    }
}