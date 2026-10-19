using Engine.Tooling.Console;

namespace Engine.Tooling.Debugging;

public sealed class SystemConsoleSink :
    IConsoleSink
{
    public void Write(ConsoleMessage message)
    {
        var code =
            string.IsNullOrWhiteSpace(message.Code)
                ? string.Empty
                : $"[{message.Code}] ";

        System.Console.WriteLine(
            $"[{message.Level}] {code}{message.Message}");

        if (message.Exception is not null)
        {
            System.Console.WriteLine(message.Exception);
        }
    }
}