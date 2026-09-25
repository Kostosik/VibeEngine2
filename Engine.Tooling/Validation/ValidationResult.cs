namespace Engine.Tooling.Validation;

public sealed class ValidationResult
{
    private readonly List<ValidationIssue> _issues = new();

    public IReadOnlyList<ValidationIssue> Issues =>
        _issues;

    public bool IsValid =>
        _issues.All(
            issue => issue.Severity < ValidationSeverity.Error);

    public void Add(
        ValidationIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);

        _issues.Add(issue);
    }

    public void Add(
        ValidationSeverity severity,
        string code,
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        _issues.Add(
            new ValidationIssue(
                severity,
                code,
                message));
    }

    public void Merge(
        ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        _issues.AddRange(
            result.Issues);
    }
}