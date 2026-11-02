using Engine.Editor.Commands;

namespace Engine.Editor.UI.Authoring;

public sealed class DuplicateEditorUiElementCommand : IEditorCommand
{
    private readonly EditorUiDocument _document;
    private readonly Guid _parentId;
    private readonly int _index;
    private readonly EditorUiElement _duplicate;

    public DuplicateEditorUiElementCommand(
        EditorUiDocument document,
        Guid elementId)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (elementId == document.Root.Id)
        {
            throw new InvalidOperationException(
                "The root UI element cannot be duplicated.");
        }

        var source = document.GetElement(elementId);

        var parent = source.Parent
            ?? throw new InvalidOperationException(
                "The selected element has no parent.");

        _document = document;
        _parentId = parent.Id;
        _index = parent.IndexOf(source) + 1;

        _duplicate = EditorUiElementCloner.Clone(
            source,
            new Engine.Core.Math.Vector2(20.0f, 20.0f),
            " Copy");
    }

    public EditorUiElement DuplicatedElement => _duplicate;

    public void Execute()
    {
        _document.AddElementCore(
            _duplicate,
            _parentId,
            _index);
    }

    public void Undo()
    {
        _document.RemoveElementCore(_duplicate);
    }
}