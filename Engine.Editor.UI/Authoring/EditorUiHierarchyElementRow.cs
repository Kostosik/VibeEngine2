
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
    private const float DragPreviewWidth = 220.0f;
    private const float DragPreviewHeight = 36.0f;

    private readonly EditorUiDocument _document;
    private readonly EditorUiElement _element;
    private readonly UiOverlayLayer? _overlay;
    private readonly string _normalButtonText;

    private UiPanel? _dragPreview;

    public EditorUiHierarchyElementRow(
        EditorUiDocument document,
        EditorUiElement element,
        int depth,
        UiOverlayLayer? overlay = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(element);

        _document = document;
        _element = element;
        _overlay = overlay;

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

        _normalButtonText =
            prefix + $"{element.Name} [{element.Type}]";

        Button = new UiButton(_normalButtonText)
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
        Button.Text = $"► {_normalButtonText}";

        if (_overlay is null)
        {
            return;
        }

        _dragPreview = new UiPanel
        {
            Width = DragPreviewWidth,
            Height = DragPreviewHeight,
            Padding = new UiThickness(
                    8.0f,
                    4.0f,
                    8.0f,
                    4.0f),
            Background = new UiColor(35, 48, 68, 245),
            HorizontalAlignment = UiHorizontalAlignment.Left,
            VerticalAlignment = UiVerticalAlignment.Top,
            IsHitTestVisible = false,
            ZIndex = 16000
        };

        var label = new UiLabel(
            $"Move: {_element.Name}")
        {
            FontSize = 12.0f,
            Color = UiColor.White,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Center
        };

        _dragPreview.AddChild(label);

        _overlay.AddChild(_dragPreview);

        UpdateDragPreviewPosition(context.Position);
    }

    public void OnDragMoved(UiDragContext context)
    {
        UpdateDragPreviewPosition(context.Position);
    }

    public void OnDragEnded(
        UiDragContext context,
        UiDragEndReason reason)
    {
        Button.Text = _normalButtonText;
        ClearDragPreview();
    }

    private void UpdateDragPreviewPosition(Vector2 pointerPosition)
    {
        if (_overlay is null || _dragPreview is null)
        {
            return;
        }

        var bounds = _overlay.Bounds;

        var x = pointerPosition.X - bounds.X + 14.0f;
        var y = pointerPosition.Y - bounds.Y + 18.0f;

        x = Math.Clamp(
            x,
            0.0f,
            MathF.Max(0.0f, bounds.Width - DragPreviewWidth));

        y = Math.Clamp(
            y,
            0.0f,
            MathF.Max(0.0f, bounds.Height - DragPreviewHeight));

        _overlay.SetPosition(
            _dragPreview,
            new Vector2(x, y));
    }

    private void ClearDragPreview()
    {
        if (_overlay is not null &&
            _dragPreview is not null &&
            ReferenceEquals(_dragPreview.Parent, _overlay))
        {
            _overlay.RemoveChild(_dragPreview);
        }

        _dragPreview = null;
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

        if (!_document.CanAcceptChild(
        _element.Id,
        dragged.Type))
        {
            return false;
        }

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
        ShowDropTargetFeedback();
    }

    public void OnDragOver(UiDragContext context)
    {
        ShowDropTargetFeedback();
    }

    public void OnDragLeave(UiDragContext context)
    {
        Button.Text = _normalButtonText;
        Background = UiColor.Transparent;
    }

    private void ShowDropTargetFeedback()
    {
        Background = new UiColor(45, 105, 65, 255);
        Button.Text = $"▼ {_normalButtonText}";
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
