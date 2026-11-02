
using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Authoring;

internal sealed class EditorUiHierarchyElementRow :
    UiPanel,
    IUiDragSource,
    IUiDropTarget
{
    private readonly EditorUiDocument _document;
    private readonly EditorUiElement _element;

    public EditorUiHierarchyElementRow(
        EditorUiDocument document,
        EditorUiElement element,
        int depth)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(element);

        _document = document;
        _element = element;

        Height = 30.0f;
        HorizontalAlignment = UiHorizontalAlignment.Stretch;
        VerticalAlignment = UiVerticalAlignment.Top;

        Margin = new UiThickness(
            depth * 14.0f,
            0.0f,
            0.0f,
            0.0f);

        var selected =
            document.Selection.Contains(element.Id);

        var prefix = selected ? "● " : "  ";

        Button = new UiButton(
            prefix + $"{element.Name} [{element.Type}]")
        {
            Height = 30.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        AddChild(Button);
    }

    public UiButton Button { get; }

    public event Action? Dropped;

    public UiDragPayload? CreateDragPayload(
        Vector2 startPosition)
    {
        if (ReferenceEquals(_element, _document.Root))
        {
            return null;
        }

        return new UiDragPayload<Guid>(_element.Id);
    }

    public void OnDragStarted(UiDragContext context)
    {
        Background = new UiColor(55, 75, 95, 255);
    }

    public void OnDragEnded(
        UiDragContext context,
        UiDragEndReason reason)
    {
        Background = UiColor.Transparent;
    }

    public bool CanAcceptDrop(UiDragPayload payload)
    {
        if (payload is not UiDragPayload<Guid> elementPayload ||
            !_document.TryGetElement(
                elementPayload.Value,
                out var dragged))
        {
            return false;
        }

        if (ReferenceEquals(dragged, _document.Root) ||
            ReferenceEquals(dragged, _element) ||
            ReferenceEquals(dragged.Parent, _element))
        {
            return false;
        }

        // Запрещаем перенос элемента внутрь
        // собственного поддерева.
        for (var current = _element;
             current is not null;
             current = current.Parent)
        {
            if (ReferenceEquals(current, dragged))
            {
                return false;
            }
        }

        return true;
    }

    public void OnDragEnter(UiDragContext context)
    {
        Background = new UiColor(45, 105, 65, 255);
    }

    public void OnDragOver(UiDragContext context)
    {
        Background = new UiColor(45, 105, 65, 255);
    }

    public void OnDragLeave(UiDragContext context)
    {
        Background = UiColor.Transparent;
    }

    public bool OnDrop(UiDragContext context)
    {
        if (context.Payload is not UiDragPayload<Guid> payload ||
            !CanAcceptDrop(context.Payload))
        {
            return false;
        }

        _document.Execute(
            new ReparentEditorUiElementCommand(
                _document,
                payload.Value,
                _element.Id));

        _document.Selection.Set(payload.Value);

        Dropped?.Invoke();

        return true;
    }
}
