using Engine.Editor.Commands;
using Engine.Editor.Selection;
using Engine.Core.Math;

namespace Engine.Editor.UI.Authoring;

public sealed class EditorUiDocument
{
    private readonly Dictionary<
        Guid,
        EditorUiElement> _elements = new();

    public EditorUiDocument(
    string name = "UI Document")
    : this(
        name,
        Guid.NewGuid(),
        new Vector2(
            1280.0f,
            720.0f))
    {
    }

    internal EditorUiDocument(
        string name,
        Guid rootId,
        Vector2 canvasSize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            name);

        if (rootId == Guid.Empty)
        {
            throw new ArgumentException(
                "UI root id cannot be empty.",
                nameof(rootId));
        }

        if (!float.IsFinite(canvasSize.X) ||
            !float.IsFinite(canvasSize.Y) ||
            canvasSize.X <= 0.0f ||
            canvasSize.Y <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(canvasSize),
                "UI document canvas size must be finite and positive.");
        }

        Name = name;

        Root =
            new EditorUiElement(
                rootId,
                EditorUiElementType.Root,
                "UI Root",
                new EditorUiLayout(
                    Vector2.Zero,
                    Vector2.Zero,
                    canvasSize));

        _elements.Add(
            Root.Id,
            Root);

        Selection =
            new SelectionSet<Guid>();

        CommandHistory =
            new EditorCommandHistory();

        Selection.Changed +=
            OnChanged;

        CommandHistory.Changed +=
            OnChanged;
    }

    public event Action? Changed;

    public string Name { get; }

    public EditorUiElement Root { get; }

    public SelectionSet<Guid> Selection { get; }

    public EditorCommandHistory CommandHistory { get; }

    public bool IsDirty =>
        CommandHistory.IsDirty;

    public EditorUiElement AddElement(
        EditorUiElementType type,
        Guid parentId,
        string? name = null)
    {
        if (type == EditorUiElementType.Root)
        {
            throw new ArgumentException(
                "A document can contain only one root element.",
                nameof(type));
        }

        GetElement(
            parentId);

        var command =
            new CreateEditorUiElementCommand(
                this,
                type,
                parentId,
                name);

        Execute(
            command);

        return command.Element;
    }

    public bool RemoveElement(
        Guid elementId)
    {
        if (elementId == Root.Id)
        {
            return false;
        }

        if (!_elements.ContainsKey(
                elementId))
        {
            return false;
        }

        Execute(
            new RemoveEditorUiElementCommand(
                this,
                elementId));

        return true;
    }

    public bool ContainsElement(
        Guid elementId)
    {
        return _elements.ContainsKey(
            elementId);
    }

    public bool TryGetElement(
        Guid elementId,
        out EditorUiElement? element)
    {
        return _elements.TryGetValue(
            elementId,
            out element);
    }

    public EditorUiElement GetElement(
        Guid elementId)
    {
        if (!_elements.TryGetValue(
                elementId,
                out var element))
        {
            throw new KeyNotFoundException(
                $"UI element '{elementId}' was not found.");
        }

        return element;
    }

    public IEnumerable<EditorUiElement> EnumerateDepthFirst()
    {
        return EnumerateSubtree(
            Root);
    }

    public void Execute(
        IEditorCommand command)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        CommandHistory.Execute(
            command);
    }

    public void Undo()
    {
        CommandHistory.Undo();
    }

    public void Redo()
    {
        CommandHistory.Redo();
    }

    public void MarkSaved()
    {
        CommandHistory.MarkSaved();
    }

    internal void AddElementCore(
        EditorUiElement element,
        Guid parentId,
        int index)
    {
        ArgumentNullException.ThrowIfNull(
            element);

        if (_elements.ContainsKey(
                element.Id))
        {
            throw new InvalidOperationException(
                $"UI element '{element.Id}' is already registered.");
        }

        var parent =
            GetElement(
                parentId);

        if (element.Parent is not null)
        {
            throw new InvalidOperationException(
                "The UI element already has a parent.");
        }

        parent.AddChild(
            element,
            index);

        RegisterSubtree(
            element);
    }

    internal void RemoveElementCore(
        EditorUiElement element)
    {
        ArgumentNullException.ThrowIfNull(
            element);

        if (ReferenceEquals(
                element,
                Root))
        {
            throw new InvalidOperationException(
                "The root UI element cannot be removed.");
        }

        var parent =
            element.Parent
            ?? throw new InvalidOperationException(
                "The UI element has no parent.");

        if (!parent.RemoveChild(
                element))
        {
            throw new InvalidOperationException(
                "The UI element is not attached to its parent.");
        }

        UnregisterSubtree(
            element);
    }

    internal void RestoreElementCore(
        EditorUiElement element,
        Guid parentId,
        int index)
    {
        ArgumentNullException.ThrowIfNull(
            element);

        if (element.Parent is not null)
        {
            throw new InvalidOperationException(
                "The UI element is already attached.");
        }

        var parent =
            GetElement(
                parentId);

        RegisterSubtree(
            element);

        parent.AddChild(
            element,
            index);
    }

    private void RegisterSubtree(
        EditorUiElement root)
    {
        foreach (var element in
                 EnumerateSubtree(root))
        {
            if (!_elements.TryAdd(
                    element.Id,
                    element))
            {
                throw new InvalidOperationException(
                    $"UI element '{element.Id}' is already registered.");
            }
        }
    }

    private void UnregisterSubtree(
        EditorUiElement root)
    {
        var removed =
            EnumerateSubtree(root)
                .ToArray();

        foreach (var element in removed)
        {
            Selection.Remove(
                element.Id);

            _elements.Remove(
                element.Id);
        }
    }

    private static IEnumerable<EditorUiElement> EnumerateSubtree(
        EditorUiElement root)
    {
        yield return root;

        foreach (var child in root.Children)
        {
            foreach (var descendant in
                     EnumerateSubtree(child))
            {
                yield return descendant;
            }
        }
    }

    private void OnChanged()
    {
        Changed?.Invoke();
    }
}