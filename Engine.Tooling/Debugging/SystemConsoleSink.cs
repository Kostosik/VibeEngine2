using Engine.Core.Diagnostics;
using Engine.Tooling.Console;

namespace Engine.Tooling.Debugging;

public sealed class SystemConsoleSink :
    IConsoleSink
{
    public void Write(
        ConsoleMessage message)
    {
        var code =
            string.IsNullOrWhiteSpace(message.Code)
                ? string.Empty
                : $"[{message.Code}] ";

        System.Console.WriteLine(
            $"[{message.Level}] {code}{message.Message}");
    }
}