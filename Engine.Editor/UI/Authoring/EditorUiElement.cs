using Engine.Core.Assets;

namespace Engine.Editor.UI.Authoring;

public sealed class EditorUiElement
{
    private readonly List<EditorUiElement> _children = new();
    
    internal EditorUiElement(
        Guid id,
        EditorUiElementType type,
        string name,
        EditorUiLayout layout)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "UI element id cannot be empty.",
                nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            name);

        Id = id;
        Type = type;
        Name = name;
        Layout = layout;
    }

    public Guid Id { get; }

    public EditorUiElementType Type { get; }

    public string Name { get; internal set; }

    public EditorUiLayout Layout { get; internal set; }

    public string Text { get; internal set; } = string.Empty;

    public AssetPath? Texture { get; internal set; }

    public string? Action { get; internal set; }
    public EditorUiElement? Parent { get; internal set; }

    public IReadOnlyList<EditorUiElement> Children =>
        _children;

    internal void AddChild(
        EditorUiElement child,
        int index)
    {
        ArgumentNullException.ThrowIfNull(
            child);

        if (ReferenceEquals(
                child,
                this))
        {
            throw new InvalidOperationException(
                "A UI element cannot be its own child.");
        }

        if (child.Parent is not null)
        {
            throw new InvalidOperationException(
                "The UI element already has a parent.");
        }

        if (index < 0 ||
            index > _children.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index));
        }

        child.Parent = this;

        _children.Insert(
            index,
            child);
    }

    internal bool RemoveChild(
        EditorUiElement child)
    {
        ArgumentNullException.ThrowIfNull(
            child);

        if (!_children.Remove(
                child))
        {
            return false;
        }

        child.Parent = null;

        return true;
    }

    internal int IndexOf(
        EditorUiElement child)
    {
        return _children.IndexOf(
            child);
    }
}