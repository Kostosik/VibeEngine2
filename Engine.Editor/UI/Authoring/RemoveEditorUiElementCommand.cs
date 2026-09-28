using Engine.Editor.Commands;

namespace Engine.Editor.UI.Authoring;

public sealed class RemoveEditorUiElementCommand :
    IEditorCommand
{
    private readonly EditorUiDocument _document;
    private readonly Guid _elementId;

    private EditorUiElement? _element;
    private Guid _parentId;
    private int _index;
    private Guid[] _selectedElements = Array.Empty<Guid>();

    public RemoveEditorUiElementCommand(
        EditorUiDocument document,
        Guid elementId)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        if (elementId == document.Root.Id)
        {
            throw new InvalidOperationException(
                "The root UI element cannot be removed.");
        }

        document.GetElement(
            elementId);

        _document = document;
        _elementId = elementId;
    }

    public void Execute()
    {
        _element =
            _document.GetElement(
                _elementId);

        var parent =
            _element.Parent
            ?? throw new InvalidOperationException(
                "The UI element has no parent.");

        _parentId =
            parent.Id;

        _index =
            parent.IndexOf(
                _element);

        if (_index < 0)
        {
            throw new InvalidOperationException(
                "The UI element is not attached to its parent.");
        }

        var subtree =
            EnumerateSubtree(
                _element)
            .Select(
                static element =>
                    element.Id)
            .ToArray();

        _selectedElements =
            _document.Selection.Items
                .Where(
                    subtree.Contains)
                .ToArray();

        _document.RemoveElementCore(
            _element);
    }

    public void Undo()
    {
        if (_element is null)
        {
            throw new InvalidOperationException(
                "The command has not been executed.");
        }

        _document.RestoreElementCore(
            _element,
            _parentId,
            _index);

        foreach (var elementId in
                 _selectedElements)
        {
            if (_document.ContainsElement(
                    elementId))
            {
                _document.Selection.Add(
                    elementId);
            }
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
}