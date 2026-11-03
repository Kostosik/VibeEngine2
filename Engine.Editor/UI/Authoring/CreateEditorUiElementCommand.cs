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
            var parent = _document.GetElement(_parentId);

            var layout =
                parent.Type == EditorUiElementType.Expander
                    ? new EditorUiLayout(
                        0.0f,
                        0.0f,
                        MathF.Max(0.0f, parent.Layout.Size.X - 16.0f),
                        80.0f)
                    : _type switch
                    {
                        EditorUiElementType.Separator =>
                            new EditorUiLayout(
                                0.0f, 0.0f, 200.0f, 1.0f),

                        EditorUiElementType.Expander =>
                            new EditorUiLayout(
                                0.0f, 0.0f, 200.0f, 132.0f),

                        _ => EditorUiLayout.Default
                    };

            _element =
                new EditorUiElement(
                    Guid.NewGuid(),
                    _type,
                    _name,
                    layout);
            if (_type == EditorUiElementType.Expander)
            {
                _element.Text = _name;
            }

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