using Engine.Runtime;

namespace Engine.Tests.Runtime;

public sealed class RebuildEngineDebugCommandTests
{
    [Fact]
    public void Execute_RejectsTooManyArguments()
    {
        using var command = new RebuildEngineDebugCommand();

        var result = command.Execute(
            new[] { "one", "Debug", "extra" });

        Assert.False(result.Success);

        Assert.Contains(
            "Usage: rebuild-engine",
            result.Message);
    }

    [Fact]
    public void Execute_RejectsUnsupportedConfiguration()
    {
        using var command = new RebuildEngineDebugCommand();

        var result = command.Execute(
            new[] { Path.GetTempPath(), "Production" });

        Assert.False(result.Success);

        Assert.Contains(
            "Debug or Release",
            result.Message);
    }
}