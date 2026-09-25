using Engine.Tooling.Validation;

namespace Engine.Tests.Validation;

public sealed class ValidationServiceTests
{
    [Fact]
    public void Validate_WithNoValidators_ReturnsValidResult()
    {
        var service =
            new ValidationService();

        var result =
            service.Validate(
                new ValidationContext());

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Register_AddsValidator()
    {
        var service =
            new ValidationService();

        var validator =
            new TestValidator();

        service.Register(
            validator);

        Assert.Single(
            service.Validators);

        Assert.Same(
            validator,
            service.Validators[0]);
    }

    [Fact]
    public void Validate_MergesValidatorResults()
    {
        var service =
            new ValidationService();

        service.Register(
            new TestValidator(
                new ValidationIssue(
                    ValidationSeverity.Warning,
                    "TEST_WARNING",
                    "Test warning.")));

        service.Register(
            new TestValidator(
                new ValidationIssue(
                    ValidationSeverity.Error,
                    "TEST_ERROR",
                    "Test error.")));

        var result =
            service.Validate(
                new ValidationContext());

        Assert.Equal(
            2,
            result.Issues.Count);

        Assert.False(
            result.IsValid);

        Assert.Equal(
            ValidationSeverity.Warning,
            result.Issues[0].Severity);

        Assert.Equal(
            ValidationSeverity.Error,
            result.Issues[1].Severity);
    }

    [Fact]
    public void Unregister_RemovesValidator()
    {
        var service =
            new ValidationService();

        var validator =
            new TestValidator();

        service.Register(
            validator);

        var removed =
            service.Unregister(
                validator);

        Assert.True(removed);
        Assert.Empty(service.Validators);
    }

    private sealed class TestValidator : IValidator
    {
        private readonly ValidationIssue? _issue;

        public TestValidator(
            ValidationIssue? issue = null)
        {
            _issue = issue;
        }

        public ValidationResult Validate(
            ValidationContext context)
        {
            var result =
                new ValidationResult();

            if (_issue is not null)
            {
                result.Add(_issue);
            }

            return result;
        }
    }
}