namespace Engine.Tooling.Validation;

public sealed class ValidationService
{
    private readonly List<IValidator> _validators = new();

    public IReadOnlyList<IValidator> Validators =>
        _validators;

    public void Register(
        IValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);

        if (_validators.Contains(validator))
        {
            throw new InvalidOperationException(
                "Validator is already registered.");
        }

        _validators.Add(validator);
    }

    public bool Unregister(
        IValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);

        return _validators.Remove(validator);
    }

    public ValidationResult Validate(
        ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var result =
            new ValidationResult();

        foreach (var validator in _validators)
        {
            result.Merge(
                validator.Validate(context));
        }

        return result;
    }
}