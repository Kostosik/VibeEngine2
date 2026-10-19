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

    public void Throw(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var exception = new FatalEngineException(
            $"{code}: {message}");

        TryReport(code, message, null, exception);

        throw exception;
    }

    public void Throw(
        string code,
        string message,
        Exception innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(innerException);

        var exception = new FatalEngineException(
            $"{code}: {message}",
            innerException);

        TryReport(code, message, innerException, exception);

        throw exception;
    }

    private void TryReport(
        string code,
        string message,
        Exception? innerException,
        FatalEngineException fatalException)
    {
        try
        {
            _reporter.Critical(
                code,
                message,
                innerException);
        }
        catch (Exception reportingException)
        {
            // Ошибка диагностического приёмника не заменяет фатальную ошибку.
            fatalException.Data["DiagnosticReportingException"] =
                reportingException;
        }
    }
}