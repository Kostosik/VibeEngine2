using Engine.Editor.Commands;

namespace Engine.Editor.UI.Authoring;

public sealed class SetEditorUiLayoutCommand : IEditorCommand
{
    private readonly EditorUiDocument _document;
    private readonly Guid _elementId;
    private readonly EditorUiLayout _oldLayout;
    private readonly EditorUiLayout _newLayout;

    public SetEditorUiLayoutCommand(
        EditorUiDocument document,
        Guid elementId,
        EditorUiLayout oldLayout,
        EditorUiLayout newLayout)
    {
        ArgumentNullException.ThrowIfNull(document);

        var element = document.GetElement(elementId);

        if (ReferenceEquals(element, document.Root))
        {
            throw new ArgumentException(
                "The root UI element cannot be transformed.",
                nameof(elementId));
        }

        _document = document;
        _elementId = elementId;
        _oldLayout = oldLayout;
        _newLayout = newLayout;
    }

    public void Execute()
    {
        SetLayout(_newLayout);
    }

    public void Undo()
    {
        SetLayout(_oldLayout);
    }

    private void SetLayout(EditorUiLayout layout)
    {
        var element = _document.GetElement(_elementId);

        if (ReferenceEquals(element, _document.Root))
        {
            throw new InvalidOperationException(
                "The root UI element cannot be transformed.");
        }

        element.Layout = layout;
    }
}