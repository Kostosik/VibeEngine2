using Engine.ECS;
using Engine.Tooling.Debugging;
using Engine.Tooling.Validation;

namespace Engine.Tests.Validation;

public sealed class ValidateDebugCommandTests
{
    [Fact]
    public void Execute_WithValidWorld_ReturnsSuccess()
    {
        var world =
            new World();

        var validation =
            new ValidationService();

        validation.Register(
            new WorldValidator(
                world));

        var command =
            new ValidateDebugCommand(
                validation);

        var result =
            command.Execute(
                Array.Empty<string>());

        Assert.True(
            result.Success);

        Assert.Equal(
            "Validation passed.",
            result.Message);
    }
}