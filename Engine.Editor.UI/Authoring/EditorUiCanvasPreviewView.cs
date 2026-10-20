using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.Input;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Authoring;

public sealed class EditorUiCanvasPreviewView :
    UiPanel
{
    private const float CanvasPadding = 16.0f;

    private readonly EditorUiDocument _document;

    private readonly ITextureResourceManager? _textures;

    private readonly HashSet<AssetPath> _unavailableTextures = new();

    private const float MinZoom = 0.25f;
    private const float MaxZoom = 8.0f;
    private const float ZoomStep = 1.15f;

    private float _zoom = 1.0f;
    private Vector2 _pan = Vector2.Zero;

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
    private ResizeHandle _resizeHandle;

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

    private enum ResizeHandle
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

    public EditorUiCanvasPreviewView(
        EditorUiDocument document,
        ITextureResourceManager? textures = null)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        _document = document;
        _textures = textures;
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


        RenderElement(
            context,
            _document.Root,
            Vector2.Zero,
            _document.Root.Layout.Size,
            canvasRect.Position,
            GetScale());
        RenderSelectionGizmo(context);

        context.PopClip();
    }

    protected override void OnPointerWheel(
    UiPointerEvent pointer)
    {
        if (!Bounds.Contains(pointer.Position) ||
            MathF.Abs(pointer.ScrollDelta) < 0.0001f)
        {
            return;
        }

        var rootSize = _document.Root.Layout.Size;
        var oldScale = GetScale();

        if (oldScale <= 0.0f)
        {
            return;
        }

        // Точка документа под курсором до масштабирования.
        var oldCanvas = GetCanvasRect();

        var documentPoint = new Vector2(
            (pointer.Position.X - oldCanvas.X) / oldScale,
            (pointer.Position.Y - oldCanvas.Y) / oldScale);

        var newZoom = Math.Clamp(
            _zoom * MathF.Pow(
                ZoomStep,
                pointer.ScrollDelta),
            MinZoom,
            MaxZoom);

        if (MathF.Abs(newZoom - _zoom) < 0.0001f)
        {
            pointer.Handled = true;
            return;
        }

        _zoom = newZoom;

        var newScale = GetScale();

        // Положение Canvas при центрировании без pan.
        var centeredOrigin = new Vector2(
            Bounds.X +
            (Bounds.Width - rootSize.X * newScale) * 0.5f,

            Bounds.Y +
            (Bounds.Height - rootSize.Y * newScale) * 0.5f);

        // Оставляем прежнюю точку документа под курсором.
        _pan = new Vector2(
            pointer.Position.X -
            documentPoint.X * newScale -
            centeredOrigin.X,

            pointer.Position.Y -
            documentPoint.Y * newScale -
            centeredOrigin.Y);

        pointer.Handled = true;
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
            _panStartOffset = _pan;

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
                BeginDrag(
                    selected,
                    DragOperation.Rotate,
                    ResizeHandle.None,
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
                BeginDrag(
                    selected,
                    DragOperation.Resize,
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

        var hit = FindElementAt(
            _document.Root,
            documentPosition,
            Vector2.Zero,
            _document.Root.Layout.Size);

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
            BeginDrag(
                hit,
                DragOperation.Move,
                ResizeHandle.None,
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
            _pan = _panStartOffset +
                   (pointer.Position - _panStartPointer);

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

    private void RenderElement(
    UiRenderContext context,
    EditorUiElement element,
    Vector2 parentOrigin,
    Vector2 parentSize,
    Vector2 canvasOrigin,
    float scale)
    {
        if (!element.Visible)
        {
            return;
        }

        var layout = GetEffectiveLayout(element);

        var localRect = layout.Resolve(parentSize);

        var elementOrigin = new Vector2(
            parentOrigin.X + localRect.X,
            parentOrigin.Y + localRect.Y);

        var rect = new UiRect(
            canvasOrigin.X + elementOrigin.X * scale,
            canvasOrigin.Y + elementOrigin.Y * scale,
            localRect.Width * scale,
            localRect.Height * scale);

        // Центр вращения элемента в экранных координатах.
        var rotationCenter = new Vector2(
            rect.X + rect.Width / 2.0f,
            rect.Y + rect.Height / 2.0f);

        // Начинаем вращение этого элемента.
        context.PushRotation(
            rotationCenter,
            layout.RotationDegrees);

        try
        {
            if (!ReferenceEquals(element, _document.Root))
            {
                context.DrawRectangle(
                    rect,
                    GetElementBackground(element),
                    filled: true);

                if (element.Type == EditorUiElementType.Image)
                {
                    RenderImage(
                        context,
                        element,
                        rect);
                }

                context.DrawRectangle(
                    rect,
                    _document.Selection.Contains(element.Id)
                        ? new UiColor(235, 235, 235, 255)
                        : new UiColor(105, 105, 105, 255),
                    filled: false,
                    layer: 1);

                if (ShouldDrawText(element) &&
                    !string.IsNullOrEmpty(element.Text))
                {
                    var textSize = context.MeasureText(
                        element.Text,
                        12.0f);

                    var textPosition = UiTextLayout.CalculatePosition(
                        rect,
                        textSize,
                        UiTextHorizontalAlignment.Center,
                        UiTextVerticalAlignment.Center);

                    context.DrawText(
                        element.Text,
                        textPosition,
                        12.0f,
                        new UiColor(235, 235, 235, 255),
                        layer: 2);
                }

                if (element.Type != EditorUiElementType.Image &&
                    string.IsNullOrEmpty(element.Text) &&
                    rect.Width >= 80.0f &&
                    rect.Height >= 26.0f)
                {
                    var text = element.Name;

                    var textSize = context.MeasureText(
                        text,
                        11.0f);

                    var textPosition = UiTextLayout.CalculatePosition(
                        rect,
                        textSize,
                        UiTextHorizontalAlignment.Center,
                        UiTextVerticalAlignment.Center);

                    context.DrawText(
                        text,
                        textPosition,
                        11.0f,
                        new UiColor(190, 190, 190, 255),
                        layer: 2);
                }
            }

            // Дочерние элементы отрисовываются внутри
            // системы координат с поворотом родителя.
            foreach (var child in element.Children.OrderBy(
                         static child => child.ZIndex))
            {
                RenderElement(
                    context,
                    child,
                    elementOrigin,
                    new Vector2(
                        localRect.Width,
                        localRect.Height),
                    canvasOrigin,
                    scale);
            }
        }
        finally
        {
            // Обязательно снимаем поворот перед выходом.
            context.PopRotation();
        }
    }

    private void RenderImage(
    UiRenderContext context,
    EditorUiElement element,
    UiRect rect)
    {
        if (_textures is null ||
            element.Texture is not AssetPath path ||
            string.IsNullOrWhiteSpace(path.Value))
        {
            DrawImagePlaceholder(
                context,
                rect,
                "No texture");

            return;
        }

        if (_unavailableTextures.Contains(path))
        {
            DrawImagePlaceholder(
                context,
                rect,
                "Texture unavailable");

            return;
        }

        try
        {
            if (!_textures.TryGet(
                    path,
                    out var texture))
            {
                texture = _textures.Load(path);
            }

            if (!texture.IsValid)
            {
                _unavailableTextures.Add(path);

                DrawImagePlaceholder(
                    context,
                    rect,
                    "Texture unavailable");

                return;
            }

            var destination = rect;

            if (element.PreserveAspectRatio &&
                _textures.TryGetDescription(
                    path,
                    out var description) &&
                description.Width > 0 &&
                description.Height > 0 &&
                rect.Width > 0.0f &&
                rect.Height > 0.0f)
            {
                var scale = MathF.Min(
                    rect.Width / description.Width,
                    rect.Height / description.Height);

                var width =
                    description.Width * scale;

                var height =
                    description.Height * scale;

                destination = new UiRect(
                    rect.X + (rect.Width - width) * 0.5f,
                    rect.Y + (rect.Height - height) * 0.5f,
                    width,
                    height);
            }

            context.DrawTexture(
                texture,
                destination,
                new Rectangle(
                    0.0f,
                    0.0f,
                    1.0f,
                    1.0f));
        }
        catch (Exception)
        {
            _unavailableTextures.Add(path);

            DrawImagePlaceholder(
                context,
                rect,
                "Texture unavailable");
        }
    }

    private static void DrawImagePlaceholder(
        UiRenderContext context,
        UiRect rect,
        string message)
    {
        if (rect.Width < 1.0f ||
            rect.Height < 1.0f)
        {
            return;
        }

        var textSize = context.MeasureText(
            message,
            10.0f);

        var textPosition = UiTextLayout.CalculatePosition(
            rect,
            textSize,
            UiTextHorizontalAlignment.Center,
            UiTextVerticalAlignment.Center);

        context.DrawText(
            message,
            textPosition,
            10.0f,
            new UiColor(
                210,
                170,
                170,
                255),
            layer: 2);
    }

    private static bool ShouldDrawText(
    EditorUiElement element)
    {
        return element.Type is
            EditorUiElementType.Label or
            EditorUiElementType.Button or
            EditorUiElementType.TextBox or
            EditorUiElementType.Toggle or
            EditorUiElementType.Dropdown;
    }
    private EditorUiElement? FindElementAt(
    EditorUiElement element,
    Vector2 point,
    Vector2 parentOrigin,
    Vector2 parentSize)
    {
        if (!element.Visible)
        {
            return null;
        }

        var layout = GetEffectiveLayout(element);
        var localRect = layout.Resolve(parentSize);

        var elementOrigin = new Vector2(
            parentOrigin.X + localRect.X,
            parentOrigin.Y + localRect.Y);

        var elementCenter = new Vector2(
            elementOrigin.X + localRect.Width * 0.5f,
            elementOrigin.Y + localRect.Height * 0.5f);

        var unrotatedPoint = RotatePointForHitTest(
            point,
            elementCenter,
            -layout.RotationDegrees);

        foreach (var child in element.Children
                     .OrderByDescending(
                         static child => child.ZIndex))
        {
            var hit = FindElementAt(
                child,
                unrotatedPoint,
                elementOrigin,
                new Vector2(
                    localRect.Width,
                    localRect.Height));

            if (hit is not null)
            {
                return hit;
            }
        }

        if (unrotatedPoint.X >= elementOrigin.X &&
            unrotatedPoint.Y >= elementOrigin.Y &&
            unrotatedPoint.X <= elementOrigin.X + localRect.Width &&
            unrotatedPoint.Y <= elementOrigin.Y + localRect.Height)
        {
            return element;
        }

        return null;
    }

    private UiRect GetCanvasRect()
    {
        var rootSize =
            _document.Root.Layout.Size;

        var availableWidth =
            MathF.Max(
                0.0f,
                Bounds.Width -
                CanvasPadding * 2.0f);

        var availableHeight =
            MathF.Max(
                0.0f,
                Bounds.Height -
                CanvasPadding * 2.0f);

        if (rootSize.X <= 0.0f ||
            rootSize.Y <= 0.0f)
        {
            return new UiRect(
                Bounds.X,
                Bounds.Y,
                0.0f,
                0.0f);
        }

        var scale = GetScale();

        var width =
            rootSize.X *
            scale;

        var height =
            rootSize.Y *
            scale;

        return new UiRect(
            Bounds.X +
            (Bounds.Width - width) * 0.5f +
            _pan.X,

            Bounds.Y +
            (Bounds.Height - height) * 0.5f +
            _pan.Y,

            width,
            height);
    }

    private float GetScale()
    {
        var rootSize =
            _document.Root.Layout.Size;

        if (rootSize.X <= 0.0f ||
            rootSize.Y <= 0.0f)
        {
            return 0.0f;
        }

        var availableWidth =
            MathF.Max(
                0.0f,
                Bounds.Width -
                CanvasPadding * 2.0f);

        var availableHeight =
            MathF.Max(
                0.0f,
                Bounds.Height -
                CanvasPadding * 2.0f);

        return _zoom * MathF.Min(
            availableWidth / rootSize.X,
            availableHeight / rootSize.Y);
    }

    private static UiColor GetElementBackground(
        EditorUiElement element)
    {
        return element.Type switch
        {
            EditorUiElementType.Panel =>
                new UiColor(60, 70, 85, 255),

            EditorUiElementType.Label =>
                new UiColor(70, 70, 70, 255),

            EditorUiElementType.Button =>
                new UiColor(65, 85, 105, 255),

            EditorUiElementType.Image =>
                new UiColor(75, 65, 90, 255),

            EditorUiElementType.TextBox =>
                new UiColor(55, 80, 75, 255),

            EditorUiElementType.Toggle =>
                new UiColor(75, 80, 55, 255),

            EditorUiElementType.Dropdown =>
                new UiColor(80, 65, 55, 255),

            EditorUiElementType.ScrollView =>
                new UiColor(55, 75, 85, 255),

            _ =>
                new UiColor(
                    60,
                    60,
                    60,
                    255)
        };
    }
}