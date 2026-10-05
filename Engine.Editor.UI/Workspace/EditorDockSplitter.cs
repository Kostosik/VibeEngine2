using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Input;
using Engine.Input.Cursors;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

internal enum EditorDockResizeAxis
{
    Horizontal,
    Vertical
}

internal sealed class EditorDockSplitter :
    UiPanel
{
    private readonly EditorDockResizeAxis _axis;
    private readonly float _direction;
    private readonly Func<float> _getSize;
    private readonly Action<float> _resize;

    private bool _dragging;
    private Vector2 _startPointer;
    private float _startSize;

    public EditorDockSplitter(
        EditorDockResizeAxis axis,
        float direction,
        Func<float> getSize,
        Action<float> resize)
    {
        ArgumentNullException.ThrowIfNull(
            getSize);

        ArgumentNullException.ThrowIfNull(
            resize);

        if (direction is not (-1.0f or 1.0f))
        {
            throw new ArgumentOutOfRangeException(
                nameof(direction));
        }

        _axis = axis;
        _direction = direction;
        _getSize = getSize;
        _resize = resize;

        Cursor =
            axis ==
            EditorDockResizeAxis.Horizontal
                ? CursorShape.ResizeHorizontal
                : CursorShape.ResizeVertical;

        IsHitTestVisible = true;
        ZIndex = 1000;
    }

    protected override void OnRender(
    UiRenderContext context)
    {
        base.OnRender(
            context);

        var color =
            IsHovered
                ? new UiColor(
                    120,
                    120,
                    120,
                    255)
                : new UiColor(
                    65,
                    65,
                    65,
                    255);

        if (_axis ==
            EditorDockResizeAxis.Horizontal)
        {
            var centerX =
                Bounds.X +
                Bounds.Width * 0.5f;

            context.DrawRectangle(
                new UiRect(
                    centerX - 1.0f,
                    Bounds.Y,
                    2.0f,
                    Bounds.Height),
                color,
                filled: true);
        }
        else
        {
            var centerY =
                Bounds.Y +
                Bounds.Height * 0.5f;

            context.DrawRectangle(
                new UiRect(
                    Bounds.X,
                    centerY - 1.0f,
                    Bounds.Width,
                    2.0f),
                color,
                filled: true);
        }
    }

    protected override void OnPointerDown(
        UiPointerEvent pointer)
    {
        if (pointer.Button !=
            InputMouseButton.Left)
        {
            return;
        }

        _dragging = true;

        _startPointer =
            pointer.Position;

        _startSize =
            _getSize();

        pointer.RequestCapture();
        pointer.Handled = true;
    }

    protected override void OnPointerMove(
        UiPointerEvent pointer)
    {
        if (!_dragging)
        {
            return;
        }

        var delta =
            _axis ==
            EditorDockResizeAxis.Horizontal
                ? pointer.Position.X -
                  _startPointer.X
                : pointer.Position.Y -
                  _startPointer.Y;

        _resize(
            _startSize +
            delta * _direction);

        pointer.Handled = true;
    }

    protected override void OnPointerUp(
        UiPointerEvent pointer)
    {
        if (!_dragging ||
            pointer.Button !=
            InputMouseButton.Left)
        {
            return;
        }

        _dragging = false;

        pointer.ReleaseCapture();
        pointer.Handled = true;
    }
}