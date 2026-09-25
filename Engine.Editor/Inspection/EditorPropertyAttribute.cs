namespace Engine.Editor.Inspection;

[AttributeUsage(
    AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = true)]
public sealed class EditorPropertyAttribute :
    Attribute
{
    public EditorPropertyAttribute(
        string? displayName = null)
    {
        DisplayName = displayName;
    }

    public string? DisplayName { get; }

    public int Order { get; init; }
}