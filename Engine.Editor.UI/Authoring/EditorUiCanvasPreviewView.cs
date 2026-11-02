using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.Input;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;
using ResizeHandle =
    Engine.Editor.UI.Authoring.EditorUiCanvasTransformTool.ResizeHandle;

namespace Engine.Editor.UI.Authoring;

public sealed class EditorUiCanvasPreviewView :
    UiPanel
{
    private readonly EditorUiDocument _document;
    private readonly EditorUiCanvasElementRenderer _elementRenderer;
    private readonly EditorUiCanvasViewportTransform _viewportTransform =
    new();
    private readonly EditorUiCanvasTransformTool _transformTool;
    private bool _panning;
    private Vector2 _panStartPointer;
    private Vector2 _panStartOffset;

    private const float GizmoHandleSize = 8.0f;
    private const float GizmoHandleHitRadius = 7.0f;
    private const float MinimumElementSize = 1.0f;

    private EditorUiElement? _dragElement;
    private EditorUiLayout _dragStartLayout;
    private EditorUiLayout _dragLayout;
    private Vector2 _dragStartPointer;
    private float _dragScale;

    private DragOperation _dragOperation;

    private const float GizmoRotateHandleOffset = 22.0f;

    private float _dragParentRotationDegrees;
    private float _dragTotalRotationDegrees;
    private Vector2 _dragRotationCenter;
    private float _dragStartPointerAngle;

    private readonly record struct ElementScreenTransform(
    UiRect LayoutRect,
    Vector2 Center,
    float RotationDegrees,
    float ParentRotationDegrees);

    private enum DragOperation
    {
        None,
        Move,
        Resize,
        Rotate
    }

    public EditorUiCanvasPreviewView(
        EditorUiDocument document,
        ITextureResourceManager? textures = null,
        EditorContext? editor = null,
        ITextureManager? projectTextureManager = null)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        _document = document;
        _transformTool = new EditorUiCanvasTransformTool(document);
        _elementRenderer =
            new EditorUiCanvasElementRenderer(
                textures,
                editor,
                projectTextureManager);
        Background =
            new UiColor(
                22,
                22,
                22,
                255);

        HorizontalAlignment =
            UiHorizontalAlignment.Stretch;

        VerticalAlignment =
            UiVerticalAlignment.Stretch;
    }

    public EditorUiDocument Document =>
        _document;

    protected override void OnRender(
        UiRenderContext context)
    {
        base.OnRender(context);
        context.PushClip(Bounds);
        var canvasRect =
            GetCanvasRect();

        context.DrawRectangle(
            canvasRect,
            new UiColor(
                38,
                38,
                38,
                255),
            filled: true);

        context.DrawRectangle(
            canvasRect,
            new UiColor(
                100,
                100,
                100,
                255),
            filled: false);


        _elementRenderer.Render(
            context,
            _document,
            canvasRect.Position,
            GetScale(),
            GetEffectiveLayout);

        RenderSelectionGizmo(context);

        context.PopClip();
    }

    protected override void OnPointerWheel(
    UiPointerEvent pointer)
    {
        if (!Bounds.Contains(pointer.Position))
        {
            return;
        }

        if (_viewportTransform.ZoomAt(
                Bounds,
                _document.Root.Layout.Size,
                pointer.Position,
                pointer.ScrollDelta))
        {
            pointer.Handled = true;
        }
    }

    protected override void OnPointerDown(
    UiPointerEvent pointer)
    {
        if (!Bounds.Contains(pointer.Position))
        {
            return;
        }

        if (pointer.Button == InputMouseButton.Middle)
        {
            _panning = true;
            _panStartPointer = pointer.Position;
            _panStartOffset = _viewportTransform.Pan;

            pointer.RequestCapture();
            pointer.Handled = true;
            return;
        }

        if (pointer.Button != InputMouseButton.Left)
        {
            return;
        }

        var scale = GetScale();

        if (scale <= 0.0f)
        {
            return;
        }

        // Сначала обрабатываем gizmo выделенного элемента.
        // Маркер вращения может находиться за пределами Canvas,
        // поэтому проверку canvasRect выполняем позже.
        if (TryGetSelectedElement(out var selected) &&
            selected is not null &&
            TryGetElementScreenTransform(
                selected,
                scale,
                out var selectedTransform))
        {
            var gizmoRect = CreateGizmoRect(
                selectedTransform.LayoutRect,
                selectedTransform.Center);

            var gizmoPoint = RotatePointForHitTest(
                pointer.Position,
                selectedTransform.Center,
                -selectedTransform.RotationDegrees);

            if (HitTestRotateHandle(gizmoPoint, gizmoRect))
            {
                _transformTool.BeginRotate(
                    selected,
                    pointer,
                    scale,
                    selectedTransform.ParentRotationDegrees,
                    selectedTransform.RotationDegrees,
                    selectedTransform.Center);

                pointer.Handled = true;
                return;
            }

            var handle = HitTestResizeHandle(
                gizmoPoint,
                gizmoRect);

            if (handle != ResizeHandle.None)
            {
                _transformTool.BeginResize(
                    selected,
                    handle,
                    pointer,
                    scale,
                    selectedTransform.ParentRotationDegrees,
                    selectedTransform.RotationDegrees,
                    selectedTransform.Center);

                pointer.Handled = true;
                return;
            }
        }

        var canvasRect = GetCanvasRect();

        if (!canvasRect.Contains(pointer.Position))
        {
            return;
        }

        var documentPosition = new Vector2(
            (pointer.Position.X - canvasRect.X) / scale,
            (pointer.Position.Y - canvasRect.Y) / scale);

        var hit = EditorUiCanvasHitTester.HitTest(
            _document,
            documentPosition,
            GetEffectiveLayout);

        if (hit is null)
        {
            return;
        }

        _document.Selection.Set(hit.Id);

        if (!ReferenceEquals(hit, _document.Root) &&
            TryGetElementScreenTransform(
                hit,
                scale,
                out var hitTransform))
        {
            _transformTool.BeginMove(
                hit,
                pointer,
                scale,
                hitTransform.ParentRotationDegrees,
                hitTransform.RotationDegrees,
                hitTransform.Center);
        }

        pointer.Handled = true;
    }

    protected override void OnPointerMove(
    UiPointerEvent pointer)
    {
        if (_panning)
        {
            _viewportTransform.SetPan(
                _panStartOffset +
                (pointer.Position - _panStartPointer));

            pointer.Handled = true;
            return;
        }

        if (_dragElement is null ||
            _dragOperation == DragOperation.None ||
            _dragScale <= 0.0f)
        {
            return;
        }

        var screenDelta = new Vector2(
            (pointer.Position.X - _dragStartPointer.X) /
            _dragScale,
            (pointer.Position.Y - _dragStartPointer.Y) /
            _dragScale);

        switch (_dragOperation)
        {
            case DragOperation.Move:
                {
                    var localDelta = RotatePointForHitTest(
                        screenDelta,
                        Vector2.Zero,
                        -_dragParentRotationDegrees);

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
                    var localDelta = RotatePointForHitTest(
                        screenDelta,
                        Vector2.Zero,
                        -_dragTotalRotationDegrees);

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
                            _dragStartLayout.RotationDegrees +
                            angleDelta
                    };

                    break;
                }
        }

        pointer.Handled = true;
    }

    protected override void OnPointerUp(
        UiPointerEvent pointer)
    {
        if (pointer.Button == InputMouseButton.Middle &&
    _panning)
        {
            _panning = false;

            pointer.ReleaseCapture();
            pointer.Handled = true;
            return;
        }

        if (pointer.Button != InputMouseButton.Left ||
            _dragElement is null ||
            _dragOperation == DragOperation.None)
        {
            return;
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
    }

    private void BeginDrag(
        EditorUiElement element,
        DragOperation operation,
        ResizeHandle handle,
        UiPointerEvent pointer,
        float scale,
        float parentRotationDegrees = 0.0f,
        float totalRotationDegrees = 0.0f,
        Vector2? rotationCenter = null)
    {
        _dragElement = element;
        _dragStartLayout = element.Layout;
        _dragLayout = element.Layout;
        _dragStartPointer = pointer.Position;
        _dragScale = scale;
        _dragOperation = operation;
        _resizeHandle = handle;

        _dragParentRotationDegrees =
            parentRotationDegrees;

        _dragTotalRotationDegrees =
            totalRotationDegrees;

        _dragRotationCenter =
            rotationCenter ?? pointer.Position;

        _dragStartPointerAngle = GetPointerAngle(
            pointer.Position,
            _dragRotationCenter);

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
    }

    private EditorUiLayout GetEffectiveLayout(
        EditorUiElement element)
    {
        return ReferenceEquals(element, _dragElement)
            ? _dragLayout
            : element.Layout;
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

            // Сохраняем правую границу на месте,
            // учитывая точку привязки элемента.
            offset = new Vector2(
                offset.X +
                (start.Size.X - width) * (1.0f - anchor.X),
                offset.Y);

            size = new Vector2(width, size.Y);
        }
        else if (MovesRight(_resizeHandle))
        {
            var width = MathF.Max(
                MinimumElementSize,
                start.Size.X + deltaX);

            // Сохраняем левую границу на месте.
            offset = new Vector2(
                offset.X +
                (width - start.Size.X) * anchor.X,
                offset.Y);

            size = new Vector2(width, size.Y);
        }

        if (MovesTop(_resizeHandle))
        {
            var height = MathF.Max(
                MinimumElementSize,
                start.Size.Y - deltaY);

            // Сохраняем нижнюю границу на месте.
            offset = new Vector2(
                offset.X,
                offset.Y +
                (start.Size.Y - height) * (1.0f - anchor.Y));

            size = new Vector2(size.X, height);
        }
        else if (MovesBottom(_resizeHandle))
        {
            var height = MathF.Max(
                MinimumElementSize,
                start.Size.Y + deltaY);

            // Сохраняем верхнюю границу на месте.
            offset = new Vector2(
                offset.X,
                offset.Y +
                (height - start.Size.Y) * anchor.Y);

            size = new Vector2(size.X, height);
        }

        return start with
        {
            Offset = offset,
            Size = size
        };
    }

    private static bool MovesLeft(ResizeHandle handle)
    {
        return handle is
            ResizeHandle.Left or
            ResizeHandle.TopLeft or
            ResizeHandle.BottomLeft;
    }

    private static bool MovesRight(ResizeHandle handle)
    {
        return handle is
            ResizeHandle.Right or
            ResizeHandle.TopRight or
            ResizeHandle.BottomRight;
    }

    private static bool MovesTop(ResizeHandle handle)
    {
        return handle is
            ResizeHandle.Top or
            ResizeHandle.TopLeft or
            ResizeHandle.TopRight;
    }

    private static bool MovesBottom(ResizeHandle handle)
    {
        return handle is
            ResizeHandle.Bottom or
            ResizeHandle.BottomLeft or
            ResizeHandle.BottomRight;
    }

    private bool TryGetSelectedElement(
    out EditorUiElement? selected)
    {
        selected = null;

        if (_document.Selection.Count != 1)
        {
            return false;
        }

        var id = _document.Selection.Items.First();

        if (!_document.TryGetElement(id, out selected) ||
            selected is null ||
            ReferenceEquals(selected, _document.Root))
        {
            selected = null;
            return false;
        }

        return true;
    }

    private bool TryGetElementScreenTransform(
    EditorUiElement element,
    float scale,
    out ElementScreenTransform transform)
    {
        var path = new List<EditorUiElement>();

        for (EditorUiElement? current = element;
             current is not null;
             current = current.Parent)
        {
            path.Add(current);
        }

        path.Reverse();

        var parentSize = _document.Root.Layout.Size;
        var parentOrigin = Vector2.Zero;
        var canvasRect = GetCanvasRect();

        var elementRect = default(UiRect);
        var center = Vector2.Zero;
        var totalRotation = 0.0f;
        var parentRotation = 0.0f;

        for (var i = 0; i < path.Count; i++)
        {
            var current = path[i];

            if (!current.Visible)
            {
                transform = default;
                return false;
            }

            var layout = GetEffectiveLayout(current);
            var localRect = layout.Resolve(parentSize);

            var origin = new Vector2(
                parentOrigin.X + localRect.X,
                parentOrigin.Y + localRect.Y);

            elementRect = new UiRect(
                canvasRect.X + origin.X * scale,
                canvasRect.Y + origin.Y * scale,
                localRect.Width * scale,
                localRect.Height * scale);

            center = new Vector2(
                elementRect.X + elementRect.Width * 0.5f,
                elementRect.Y + elementRect.Height * 0.5f);

            totalRotation += layout.RotationDegrees;

            if (i < path.Count - 1)
            {
                parentRotation += layout.RotationDegrees;
            }

            parentOrigin = origin;
            parentSize = new Vector2(
                localRect.Width,
                localRect.Height);
        }

        if (path.Count == 0)
        {
            transform = default;
            return false;
        }

        transform = new ElementScreenTransform(
            elementRect,
            center,
            totalRotation,
            parentRotation);

        return true;
    }

    private void RenderSelectionGizmo(
    UiRenderContext context)
    {
        if (!TryGetSelectedElement(out var selected) ||
            selected is null)
        {
            return;
        }

        var scale = GetScale();

        if (scale <= 0.0f ||
            !TryGetElementScreenTransform(
                selected,
                scale,
                out var transform))
        {
            return;
        }

        var rect = CreateGizmoRect(
            transform.LayoutRect,
            transform.Center);

        context.PushRotation(
            transform.Center,
            transform.RotationDegrees);

        try
        {
            context.DrawRectangle(
                rect,
                new UiColor(45, 155, 255, 255),
                filled: false,
                layer: 10);

            foreach (var (_, center) in GetResizeHandles(rect))
            {
                DrawGizmoHandle(
                    context,
                    center,
                    11);
            }

            var centerX = rect.X + rect.Width * 0.5f;

            var rotationHandleCenter = new Vector2(
                centerX,
                rect.Y - GizmoRotateHandleOffset);

            var stemTop =
                rotationHandleCenter.Y + GizmoHandleSize * 0.5f;

            context.DrawRectangle(
                new UiRect(
                    centerX - 1.0f,
                    stemTop,
                    2.0f,
                    MathF.Max(0.0f, rect.Y - stemTop)),
                new UiColor(45, 155, 255, 255),
                filled: true,
                layer: 10);

            DrawGizmoHandle(
                context,
                rotationHandleCenter,
                11);
        }
        finally
        {
            context.PopRotation();
        }
    }

    private void DrawGizmoHandle(
        UiRenderContext context,
        Vector2 center,
        int layer)
    {
        var halfSize = GizmoHandleSize * 0.5f;

        var handleRect = new UiRect(
            center.X - halfSize,
            center.Y - halfSize,
            GizmoHandleSize,
            GizmoHandleSize);

        context.DrawRectangle(
            handleRect,
            new UiColor(45, 155, 255, 255),
            filled: true,
            layer: layer);

        context.DrawRectangle(
            handleRect,
            new UiColor(245, 245, 245, 255),
            filled: false,
            layer: layer + 1);
    }

    private static UiRect CreateGizmoRect(
    UiRect sourceRect,
    Vector2 center)
    {
        return new UiRect(
            center.X - sourceRect.Width * 0.5f,
            center.Y - sourceRect.Height * 0.5f,
            sourceRect.Width,
            sourceRect.Height);
    }

    private static bool HitTestRotateHandle(
        Vector2 pointer,
        UiRect rect)
    {
        var centerX = rect.X + rect.Width * 0.5f;

        var handleCenter = new Vector2(
            centerX,
            rect.Y - GizmoRotateHandleOffset);

        return MathF.Abs(pointer.X - handleCenter.X) <=
                   GizmoHandleHitRadius &&
               MathF.Abs(pointer.Y - handleCenter.Y) <=
                   GizmoHandleHitRadius;
    }

    private static float GetPointerAngle(
        Vector2 pointer,
        Vector2 center)
    {
        return MathF.Atan2(
            pointer.Y - center.Y,
            pointer.X - center.X) *
            (180.0f / MathF.PI);
    }

    private static float NormalizeAngle(float degrees)
    {
        degrees %= 360.0f;

        if (degrees > 180.0f)
        {
            degrees -= 360.0f;
        }
        else if (degrees < -180.0f)
        {
            degrees += 360.0f;
        }

        return degrees;
    }

    private static Vector2 RotatePointForHitTest(
        Vector2 point,
        Vector2 origin,
        float rotationDegrees)
    {
        if (MathF.Abs(rotationDegrees) < 0.0001f)
        {
            return point;
        }

        var radians =
            rotationDegrees * (MathF.PI / 180.0f);

        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);

        var dx = point.X - origin.X;
        var dy = point.Y - origin.Y;

        return new Vector2(
            origin.X + dx * cos - dy * sin,
            origin.Y + dx * sin + dy * cos);
    }

    private static (
        ResizeHandle Handle,
        Vector2 Center)[] GetResizeHandles(
            UiRect rect)
    {
        var left = rect.X;
        var top = rect.Y;
        var right = rect.X + rect.Width;
        var bottom = rect.Y + rect.Height;

        var centerX = (left + right) * 0.5f;
        var centerY = (top + bottom) * 0.5f;

        // Угловые маркеры идут первыми, чтобы при
        // небольшом размере элемента они имели приоритет.
        return new[]
        {
        (ResizeHandle.TopLeft,
            new Vector2(left, top)),

        (ResizeHandle.TopRight,
            new Vector2(right, top)),

        (ResizeHandle.BottomLeft,
            new Vector2(left, bottom)),

        (ResizeHandle.BottomRight,
            new Vector2(right, bottom)),

        (ResizeHandle.Left,
            new Vector2(left, centerY)),

        (ResizeHandle.Right,
            new Vector2(right, centerY)),

        (ResizeHandle.Top,
            new Vector2(centerX, top)),

        (ResizeHandle.Bottom,
            new Vector2(centerX, bottom))
    };
    }

    private static ResizeHandle HitTestResizeHandle(
        Vector2 pointer,
        UiRect rect)
    {
        foreach (var (handle, center) in GetResizeHandles(rect))
        {
            if (MathF.Abs(pointer.X - center.X) <=
                    GizmoHandleHitRadius &&
                MathF.Abs(pointer.Y - center.Y) <=
                    GizmoHandleHitRadius)
            {
                return handle;
            }
        }

        return ResizeHandle.None;
    }


    public void ReleaseResources()
    {
        _elementRenderer.Dispose();
    }


    private UiRect GetCanvasRect()
    {
        return _viewportTransform.GetCanvasRect(
            Bounds,
            _document.Root.Layout.Size);
    }

    private float GetScale()
    {
        return _viewportTransform.GetScale(
            Bounds,
            _document.Root.Layout.Size);
    }
}