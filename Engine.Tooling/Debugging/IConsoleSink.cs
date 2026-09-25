using Engine.Tooling.Console;

namespace Engine.Tooling.Debugging;

public interface IConsoleSink
{
    void Write(
        ConsoleMessage message);
}