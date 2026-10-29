using Engine.Core.Diagnostics;
using Engine.Editor.UI.Panels;
using Engine.Tooling.Console;

namespace Engine.Tests.Editor;

public sealed class EditorConsoleMessageFormattingTests
{
    [Fact]
    public void FormatText_WithException_IncludesMessageAndStackTrace()
    {
        var exception = CaptureProbeException();

        var message = new ConsoleMessage(
            DateTime.UtcNow,
            DiagnosticLevel.Error,
            "Script execution failed",
            "SCRIPT_FAILED",
            exception);

        var text =
            EditorConsoleMessageFormatter.FormatText(message);

        Assert.Contains("Script execution failed", text);
        Assert.Contains(exception.ToString(), text);
        Assert.Contains("probe failure", text);
    }

    [Fact]
    public void FormatText_WithoutException_ReturnsOriginalMessage()
    {
        var message = new ConsoleMessage(
            DateTime.UtcNow,
            DiagnosticLevel.Info,
            "Game started",
            null);

        var text =
            EditorConsoleMessageFormatter.FormatText(message);

        Assert.Equal("Game started", text);
    }

    private static Exception CaptureProbeException()
    {
        try
        {
            throw new InvalidOperationException("probe failure");
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}