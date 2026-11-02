using Engine.Core.Math;
using Engine.Editor.Commands;

namespace Engine.Editor.UI.Authoring;

public sealed class PasteEditorUiElementCommand : IEditorCommand
{
    private readonly EditorUiDocument _document;
    private readonly Guid _parentId;
    private readonly int _index;
    private readonly EditorUiElement _element;

    public PasteEditorUiElementCommand(
        EditorUiDocument document,
        EditorUiElement clipboardElement,
        Guid parentId,
        Vector2 offset,
        string nameSuffix)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(clipboardElement);

        var parent = document.GetElement(parentId);

        _document = document;
        _parentId = parentId;
        _index = parent.Children.Count;

        _element = EditorUiElementCloner.Clone(
            clipboardElement,
            offset,
            nameSuffix);
    }

    public EditorUiElement PastedElement => _element;

    public void Execute()
    {
        _document.AddElementCore(
            _element,
            _parentId,
            _index);
    }

    public void Undo()
    {
        _document.RemoveElementCore(_element);
    }
}