namespace Engine.Editor.Inspection;

public sealed class EditorProperty
{
    private readonly Func<object?> _getter;
    private readonly Action<object?>? _setter;

    public EditorProperty(
        string name,
        Type propertyType,
        Func<object?> getter,
        Action<object?>? setter = null,
        string? displayName = null,
        int order = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            name);

        ArgumentNullException.ThrowIfNull(
            propertyType);

        ArgumentNullException.ThrowIfNull(
            getter);

        Name = name;
        DisplayName =
            string.IsNullOrWhiteSpace(displayName)
                ? name
                : displayName;

        PropertyType = propertyType;
        Order = order;

        _getter = getter;
        _setter = setter;
    }

    public string Name { get; }

    public string DisplayName { get; }

    public Type PropertyType { get; }

    public int Order { get; }

    public bool IsReadOnly =>
        _setter is null;

    public object? GetValue()
    {
        return _getter();
    }

    public void SetValue(
        object? value)
    {
        if (_setter is null)
        {
            throw new InvalidOperationException(
                $"Property '{Name}' is read-only.");
        }

        _setter(value);
    }
}