using Engine.Editor.Commands;

namespace Engine.Editor.UI.Authoring;

public sealed class CreateEditorUiElementCommand :
    IEditorCommand
{
    private readonly EditorUiDocument _document;
    private readonly EditorUiElementType _type;
    private readonly Guid _parentId;
    private readonly string _name;

    private EditorUiElement? _element;
    private int _index;

    public CreateEditorUiElementCommand(
        EditorUiDocument document,
        EditorUiElementType type,
        Guid parentId,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        if (type == EditorUiElementType.Root)
        {
            throw new ArgumentException(
                "A root element cannot be created.",
                nameof(type));
        }

        document.GetElement(
            parentId);

        _document = document;
        _type = type;
        _parentId = parentId;
        _name =
            string.IsNullOrWhiteSpace(name)
                ? type.ToString()
                : name;
    }

    public EditorUiElement Element =>
        _element
        ?? throw new InvalidOperationException(
            "The command has not been executed.");

    public void Execute()
    {
        if (_element is null)
        {
            _element =
                new EditorUiElement(
                    Guid.NewGuid(),
                    _type,
                    _name,
                    EditorUiLayout.Default);

            var parent =
                _document.GetElement(
                    _parentId);

            _index =
                parent.Children.Count;
        }

        _document.AddElementCore(
            _element,
            _parentId,
            _index);
    }

    public void Undo()
    {
        _document.RemoveElementCore(
            Element);
    }
}