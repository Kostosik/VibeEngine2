using Engine.Core.Diagnostics;
using Engine.Core.Logging;

namespace Engine.Core.Diagnostics;

public sealed class DiagnosticLogger : IDiagnosticSink
{
    private readonly ILogger _logger;

    public DiagnosticLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void Report(Diagnostic diagnostic)
    {
        var message = $"[{diagnostic.Code}] {diagnostic.Message}";

        switch (diagnostic.Level)
        {
            case DiagnosticLevel.Trace:
                _logger.Trace(message);
                break;

            case DiagnosticLevel.Debug:
                _logger.Debug(message);
                break;

            case DiagnosticLevel.Info:
                _logger.Info(message);
                break;

            case DiagnosticLevel.Warning:
                _logger.Warning(message);
                break;

            case DiagnosticLevel.Error:
                _logger.Error(message);
                break;

            case DiagnosticLevel.Critical:
                _logger.Critical(message);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(diagnostic),
                    diagnostic.Level,
                    "Unknown diagnostic level.");
        }
    }
}