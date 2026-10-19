namespace Engine.ECS.Components;

public readonly record struct EntityName
{
    private readonly string? _value;

    public string Value =>
        _value ?? string.Empty;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(_value);

    public EntityName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            value);

        _value = value.Trim();
    }

    public override string ToString() =>
        Value;
}