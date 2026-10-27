using Engine.Input;

namespace Engine.Editor.UI.Workspace;

public readonly record struct EditorHierarchyShortcut(
    InputBinding Key,
    string DisplayText,
    bool Control = false,
    bool Shift = false,
    bool Alt = false)
{
    public bool IsValid =>
        Key.IsValid &&
        !string.IsNullOrWhiteSpace(DisplayText);
}

public sealed class EditorHierarchyActionDefinition
{
    private readonly Func<EditorHierarchyActionContext, bool>
        _canExecute;

    private readonly Action<EditorHierarchyActionContext>
        _execute;

    public EditorHierarchyActionDefinition(
        string id,
        string label,
        EditorHierarchyActionSurface surfaces,
        Func<EditorHierarchyActionContext, bool> canExecute,
        Action<EditorHierarchyActionContext> execute,
        EditorHierarchyShortcut? shortcut = null,
        string? section = null,
        int order = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(canExecute);
        ArgumentNullException.ThrowIfNull(execute);

        if (shortcut is { IsValid: false })
        {
            throw new ArgumentException(
                "The hierarchy action shortcut is invalid.",
                nameof(shortcut));
        }

        Id = id;
        Label = label;
        Surfaces = surfaces;
        Shortcut = shortcut;
        Section = string.IsNullOrWhiteSpace(section)
            ? null
            : section;
        Order = order;

        _canExecute = canExecute;
        _execute = execute;
    }

    public string Id { get; }

    public string Label { get; }

    public EditorHierarchyActionSurface Surfaces { get; }

    public EditorHierarchyShortcut? Shortcut { get; }

    public string? Section { get; }

    public int Order { get; }

    public bool CanExecute(
        EditorHierarchyActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _canExecute(context);
    }

    internal void Invoke(
        EditorHierarchyActionContext context)
    {
        _execute(context);
    }
}