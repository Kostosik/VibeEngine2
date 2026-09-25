using Engine.Tooling.Validation;

namespace Engine.Tooling.Debugging;

public sealed class ValidateDebugCommand :
    IDebugCommand
{
    private readonly ValidationService _validation;

    public ValidateDebugCommand(
        ValidationService validation)
    {
        ArgumentNullException.ThrowIfNull(
            validation);

        _validation = validation;
    }

    public string Name =>
        "validate";

    public string Description =>
        "Validates the current engine state.";

    public DebugCommandResult Execute(
        IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(
            arguments);

        if (arguments.Count != 0)
        {
            return DebugCommandResult.Fail(
                "Usage: validate");
        }

        var result =
            _validation.Validate(
                new ValidationContext());

        if (result.IsValid)
        {
            if (result.Issues.Count == 0)
            {
                return DebugCommandResult.Ok(
                    "Validation passed.");
            }

            return DebugCommandResult.Ok(
                $"Validation passed with " +
                $"{result.Issues.Count} warning(s).");
        }

        var errors =
            result.Issues.Count(
                issue =>
                    issue.Severity == ValidationSeverity.Error);

        return DebugCommandResult.Fail(
            $"Validation failed with {errors} error(s).");
    }
}