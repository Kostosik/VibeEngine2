using Engine.Tooling.Console;

namespace Engine.Editor.UI.Panels;

public static class EditorConsoleMessageFormatter
{
    public static string FormatText(ConsoleMessage message)
    {
        return message.Exception is null
            ? message.Message
            : $"{message.Message}{Environment.NewLine}{message.Exception}";
    }
}