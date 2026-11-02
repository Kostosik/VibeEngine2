using Engine.Core.Math;
using Engine.Editor.Commands;
using Engine.Input;
using Engine.UI.Input;

namespace Engine.Editor.UI.Authoring;

internal sealed class EditorUiCanvasTransformTool
{
    private const float MinimumElementSize = 1.0f;
    private Vector2 _dragStartDocumentPosition;
    private readonly EditorUiDocument _document;
    private EditorUiElement? _dragElement;
    private EditorUiLayout _dragStartLayout;
    private EditorUiLayout _dragLayout;
    private Vector2 _dragStartPointer;
    private float _dragScale;
    private Vector2 _dragStartDocumentCenter;
    private DragOperation _dragOperation;
    private ResizeHandle _resizeHandle;
    public bool SnapEnabled { get; set; } = true;
    public bool IsMoving =>
    _dragOperation == DragOperation.Move;
    private float _dragParentRotationDegrees;
    private float _dragTotalRotationDegrees;
    private Vector2 _dragRotationCenter;
    private float _dragStartPointerAngle;

    internal enum ResizeHandle
    {
        None,
        Left,
        Right,
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    private enum DragOperation
    {
        None,
        Move,
        Resize,
        Rotate
    }

    public EditorUiCanvasTransformTool(
        EditorUiDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _document = document;
    }

    public EditorUiLayout GetEffectiveLayout(
        EditorUiElement element)
    {
        return ReferenceEquals(element, _dragElement)
            ? _dragLayout
            : element.Layout;
    }

    public void BeginMove(
        EditorUiElement element,
        UiPointerEvent pointer,
        float scale,
        float parentRotationDegrees,
        float totalRotationDegrees,
        Vector2 rotationCenter, Vector2 documentStartPosition)
    {
        BeginDrag(
            element,
            DragOperation.Move,
            ResizeHandle.None,
            pointer,
            scale,
            parentRotationDegrees,
            totalRotationDegrees,
            rotationCenter);
        _dragStartDocumentPosition = documentStartPosition;
    }

    public void BeginResize(
        EditorUiElement element,
        ResizeHandle handle,
        UiPointerEvent pointer,
        float scale,
        float parentRotationDegrees,
        float totalRotationDegrees,
        Vector2 rotationCenter, Vector2 documentCenter)
    {
        BeginDrag(
            element,
            DragOperation.Resize,
            handle,
            pointer,
            scale,
            parentRotationDegrees,
            totalRotationDegrees,
            rotationCenter);
        _dragStartDocumentCenter = documentCenter;
    }

    public void BeginRotate(
        EditorUiElement element,
        UiPointerEvent pointer,
        float scale,
        float parentRotationDegrees,
        float totalRotationDegrees,
        Vector2 rotationCenter)
    {
        BeginDrag(
            element,
            DragOperation.Rotate,
            ResizeHandle.None,
            pointer,
            scale,
            parentRotationDegrees,
            totalRotationDegrees,
            rotationCenter);
    }

    public bool HandlePointerMove(
        UiPointerEvent pointer)
    {
        if (_dragElement is null ||
            _dragOperation == DragOperation.None ||
            _dragScale <= 0.0f)
        {
            return false;
        }

        var screenDelta = new Vector2(
            (pointer.Position.X - _dragStartPointer.X) / _dragScale,
            (pointer.Position.Y - _dragStartPointer.Y) / _dragScale);

        switch (_dragOperation)
        {
            case DragOperation.Move:
                {
                    var localDelta = RotatePoint(
                        screenDelta,
                        Vector2.Zero,
                        -_dragParentRotationDegrees);

                    if (SnapEnabled)
                    {
                        var documentDelta = RotatePoint(
                            localDelta,
                            Vector2.Zero,
                            _dragParentRotationDegrees);

                        var unsnappedPosition =
                            _dragStartDocumentPosition + documentDelta;

                        var snappedPosition =
                            EditorUiCanvasGrid.SnapPosition(unsnappedPosition);

                        var correction = RotatePoint(
                            snappedPosition - unsnappedPosition,
                            Vector2.Zero,
                            -_dragParentRotationDegrees);

                        localDelta += correction;
                    }

                    _dragLayout = _dragStartLayout with
                    {
                        Offset = new Vector2(
                            _dragStartLayout.Offset.X + localDelta.X,
                            _dragStartLayout.Offset.Y + localDelta.Y)
                    };

                    break;
                }

            case DragOperation.Resize:
                {
                    var localDelta = RotatePoint(
                        screenDelta,
                        Vector2.Zero,
                        -_dragTotalRotationDegrees);

                    if (SnapEnabled)
                    {
                        var candidateLayout = CreateResizedLayout(
                            localDelta.X,
                            localDelta.Y);

                        var centerDelta = new Vector2(
                            candidateLayout.Offset.X - _dragStartLayout.Offset.X +
                            (candidateLayout.Size.X - _dragStartLayout.Size.X) *
                            (0.5f - _dragStartLayout.Anchor.X),

                            candidateLayout.Offset.Y - _dragStartLayout.Offset.Y +
                            (candidateLayout.Size.Y - _dragStartLayout.Size.Y) *
                            (0.5f - _dragStartLayout.Anchor.Y));

                        var documentCenter = _dragStartDocumentCenter +
                            RotatePoint(
                                centerDelta,
                                Vector2.Zero,
                                _dragParentRotationDegrees);

                        var handleOffset = GetResizeHandleOffset(
                            candidateLayout.Size);

                        var handlePosition = documentCenter +
                            RotatePoint(
                                handleOffset,
                                Vector2.Zero,
                                _dragTotalRotationDegrees);

                        var snappedPosition =
                            EditorUiCanvasGrid.SnapPosition(handlePosition);

                        var correction = RotatePoint(
                            snappedPosition - handlePosition,
                            Vector2.Zero,
                            -_dragTotalRotationDegrees);

                        localDelta = new Vector2(
                            localDelta.X +
                            (MovesLeft(_resizeHandle) || MovesRight(_resizeHandle)
                                ? correction.X
                                : 0.0f),

                            localDelta.Y +
                            (MovesTop(_resizeHandle) || MovesBottom(_resizeHandle)
                                ? correction.Y
                                : 0.0f));
                    }

                    _dragLayout = CreateResizedLayout(
                        localDelta.X,
                        localDelta.Y);

                    break;
                }

            case DragOperation.Rotate:
                {
                    var currentAngle = GetPointerAngle(
                        pointer.Position,
                        _dragRotationCenter);

                    var angleDelta = NormalizeAngle(
                        currentAngle - _dragStartPointerAngle);

                    _dragLayout = _dragStartLayout with
                    {
                        RotationDegrees =
                            _dragStartLayout.RotationDegrees + angleDelta
                    };

                    break;
                }
        }

        pointer.Handled = true;
        return true;
    }

    private Vector2 GetResizeHandleOffset(Vector2 size)
    {
        var x = MovesLeft(_resizeHandle)
            ? -size.X * 0.5f
            : MovesRight(_resizeHandle)
                ? size.X * 0.5f
                : 0.0f;

        var y = MovesTop(_resizeHandle)
            ? -size.Y * 0.5f
            : MovesBottom(_resizeHandle)
                ? size.Y * 0.5f
                : 0.0f;

        return new Vector2(x, y);
    }

    public bool HandlePointerUp(
        UiPointerEvent pointer)
    {
        if (pointer.Button != InputMouseButton.Left ||
            _dragElement is null ||
            _dragOperation == DragOperation.None)
        {
            return false;
        }

        var element = _dragElement;
        var oldLayout = _dragStartLayout;
        var newLayout = _dragLayout;

        ClearDrag();

        pointer.ReleaseCapture();
        pointer.Handled = true;

        if (oldLayout != newLayout)
        {
            _document.Execute(
                new SetEditorUiLayoutCommand(
                    _document,
                    element.Id,
                    oldLayout,
                    newLayout));
        }

        return true;
    }

    private void BeginDrag(
        EditorUiElement element,
        DragOperation operation,
        ResizeHandle handle,
        UiPointerEvent pointer,
        float scale,
        float parentRotationDegrees,
        float totalRotationDegrees,
        Vector2 rotationCenter)
    {
        _dragElement = element;
        _dragStartLayout = element.Layout;
        _dragLayout = element.Layout;
        _dragStartPointer = pointer.Position;
        _dragScale = scale;
        _dragOperation = operation;
        _resizeHandle = handle;

        _dragParentRotationDegrees = parentRotationDegrees;
        _dragTotalRotationDegrees = totalRotationDegrees;
        _dragRotationCenter = rotationCenter;

        _dragStartPointerAngle = GetPointerAngle(
            pointer.Position,
            rotationCenter);

        pointer.RequestCapture();
    }

    private void ClearDrag()
    {
        _dragElement = null;
        _dragOperation = DragOperation.None;
        _resizeHandle = ResizeHandle.None;
        _dragScale = 0.0f;
        _dragParentRotationDegrees = 0.0f;
        _dragTotalRotationDegrees = 0.0f;
        _dragRotationCenter = Vector2.Zero;
        _dragStartPointerAngle = 0.0f;
        _dragStartDocumentCenter = Vector2.Zero;
    }

    public void ApplyMoveCorrection(Vector2 documentCorrection)
    {
        if (!IsMoving)
        {
            return;
        }

        var localCorrection = RotatePoint(
            documentCorrection,
            Vector2.Zero,
            -_dragParentRotationDegrees);

        _dragLayout = _dragLayout with
        {
            Offset = new Vector2(
                _dragLayout.Offset.X + localCorrection.X,
                _dragLayout.Offset.Y + localCorrection.Y)
        };
    }
    private EditorUiLayout CreateResizedLayout(
        float deltaX,
        float deltaY)
    {
        var start = _dragStartLayout;
        var offset = start.Offset;
        var size = start.Size;
        var anchor = start.Anchor;

        if (MovesLeft(_resizeHandle))
        {
            var width = MathF.Max(
                MinimumElementSize,
                start.Size.X - deltaX);

            offset = new Vector2(
                offset.X + (start.Size.X - width) * (1.0f - anchor.X),
                offset.Y);

            size = new Vector2(width, size.Y);
        }
        else if (MovesRight(_resizeHandle))
        {
            var width = MathF.Max(
                MinimumElementSize,
                start.Size.X + deltaX);

            offset = new Vector2(
                offset.X + (width - start.Size.X) * anchor.X,
                offset.Y);

            size = new Vector2(width, size.Y);
        }

        if (MovesTop(_resizeHandle))
        {
            var height = MathF.Max(
                MinimumElementSize,
                start.Size.Y - deltaY);

            offset = new Vector2(
                offset.X,
                offset.Y + (start.Size.Y - height) * (1.0f - anchor.Y));

            size = new Vector2(size.X, height);
        }
        else if (MovesBottom(_resizeHandle))
        {
            var height = MathF.Max(
                MinimumElementSize,
                start.Size.Y + deltaY);

            offset = new Vector2(
                offset.X,
                offset.Y + (height - start.Size.Y) * anchor.Y);

            size = new Vector2(size.X, height);
        }

        return start with
        {
            Offset = offset,
            Size = size
        };
    }

    private static bool MovesLeft(ResizeHandle handle) =>
        handle is ResizeHandle.Left or
            ResizeHandle.TopLeft or
            ResizeHandle.BottomLeft;

    private static bool MovesRight(ResizeHandle handle) =>
        handle is ResizeHandle.Right or
            ResizeHandle.TopRight or
            ResizeHandle.BottomRight;

    private static bool MovesTop(ResizeHandle handle) =>
        handle is ResizeHandle.Top or
            ResizeHandle.TopLeft or
            ResizeHandle.TopRight;

    private static bool MovesBottom(ResizeHandle handle) =>
        handle is ResizeHandle.Bottom or
            ResizeHandle.BottomLeft or
            ResizeHandle.BottomRight;

    private static float GetPointerAngle(
        Vector2 pointer,
        Vector2 center)
    {
        return MathF.Atan2(
            pointer.Y - center.Y,
            pointer.X - center.X) * (180.0f / MathF.PI);
    }

    private static float NormalizeAngle(float degrees)
    {
        degrees %= 360.0f;

        if (degrees > 180.0f)
            degrees -= 360.0f;
        else if (degrees < -180.0f)
            degrees += 360.0f;

        return degrees;
    }

    private static Vector2 RotatePoint(
        Vector2 point,
        Vector2 origin,
        float rotationDegrees)
    {
        if (MathF.Abs(rotationDegrees) < 0.0001f)
            return point;

        var radians = rotationDegrees * (MathF.PI / 180.0f);
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);

        var dx = point.X - origin.X;
        var dy = point.Y - origin.Y;

        return new Vector2(
            origin.X + dx * cos - dy * sin,
            origin.Y + dx * sin + dy * cos);
    }
}