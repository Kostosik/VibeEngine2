using Engine.Core.Math;
using Engine.Graphics.Commands;
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

    private enum DragOperation
    {
        None,
        Move,
        Resize
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
        EditorUiDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        _document = document;

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

        if (pointer.Button != InputMouseButton.Left)
        {
            return;
        }

        if (!Bounds.Contains(pointer.Position))
        {
            return;
        }

        var canvasRect = GetCanvasRect();

        if (!canvasRect.Contains(pointer.Position))
        {
            return;
        }

        var scale = GetScale();

        if (scale <= 0.0f)
        {
            return;
        }

        // Сначала проверяем маркеры уже выбранного элемента.
        // Они должны иметь приоритет над обычным hit-test.
        if (TryGetSelectedElement(out var selected) &&
            selected is not null &&
            TryGetElementScreenRect(
                selected,
                scale,
                out var selectedRect))
        {
            var handle = HitTestResizeHandle(
                pointer.Position,
                selectedRect);

            if (handle != ResizeHandle.None)
            {
                BeginDrag(
                    selected,
                    DragOperation.Resize,
                    handle,
                    pointer,
                    scale);

                pointer.Handled = true;
                return;
            }
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

        // Корневой canvas не перемещаем и не масштабируем.
        if (!ReferenceEquals(hit, _document.Root))
        {
            BeginDrag(
                hit,
                DragOperation.Move,
                ResizeHandle.None,
                pointer,
                scale);
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

        var deltaX =
            (pointer.Position.X - _dragStartPointer.X) /
            _dragScale;

        var deltaY =
            (pointer.Position.Y - _dragStartPointer.Y) /
            _dragScale;

        if (_dragOperation == DragOperation.Move)
        {
            _dragLayout = _dragStartLayout with
            {
                Offset = new Vector2(
                    _dragStartLayout.Offset.X + deltaX,
                    _dragStartLayout.Offset.Y + deltaY)
            };
        }
        else
        {
            _dragLayout = CreateResizedLayout(
                deltaX,
                deltaY);
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
    float scale)
    {
        _dragElement = element;
        _dragStartLayout = element.Layout;
        _dragLayout = element.Layout;
        _dragStartPointer = pointer.Position;
        _dragScale = scale;
        _dragOperation = operation;
        _resizeHandle = handle;

        pointer.RequestCapture();
    }

    private void ClearDrag()
    {
        _dragElement = null;
        _dragOperation = DragOperation.None;
        _resizeHandle = ResizeHandle.None;
        _dragScale = 0.0f;
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

    private bool TryGetElementScreenRect(
        EditorUiElement element,
        float scale,
        out UiRect screenRect)
    {
        // Собираем путь от root до выбранного элемента,
        // чтобы учесть все родительские смещения.
        var path = new Stack<EditorUiElement>();

        for (var current = element;
             current is not null;
             current = current.Parent)
        {
            path.Push(current);
        }

        var parentSize = _document.Root.Layout.Size;
        var parentOrigin = Vector2.Zero;

        while (path.Count > 0)
        {
            var current = path.Pop();

            if (!current.Visible)
            {
                screenRect = default;
                return false;
            }

            var localRect = GetEffectiveLayout(current)
                .Resolve(parentSize);

            var origin = new Vector2(
                parentOrigin.X + localRect.X,
                parentOrigin.Y + localRect.Y);

            if (ReferenceEquals(current, element))
            {
                var canvasRect = GetCanvasRect();

                screenRect = new UiRect(
                    canvasRect.X + origin.X * scale,
                    canvasRect.Y + origin.Y * scale,
                    localRect.Width * scale,
                    localRect.Height * scale);

                return true;
            }

            parentOrigin = origin;
            parentSize = new Vector2(
                localRect.Width,
                localRect.Height);
        }

        screenRect = default;
        return false;
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
            !TryGetElementScreenRect(
                selected,
                scale,
                out var rect))
        {
            return;
        }

        context.DrawRectangle(
            rect,
            new UiColor(45, 155, 255, 255),
            filled: false,
            layer: 10);

        foreach (var (_, center) in GetResizeHandles(rect))
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
                layer: 11);

            context.DrawRectangle(
                handleRect,
                new UiColor(245, 245, 245, 255),
                filled: false,
                layer: 12);
        }
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

                if (string.IsNullOrEmpty(element.Text) &&
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

        var localRect =
            element.Layout.Resolve(
                parentSize);

        var elementOrigin =
            new Vector2(
                parentOrigin.X +
                localRect.X,
                parentOrigin.Y +
                localRect.Y);

        foreach (var child in element.Children
             .OrderByDescending(
                 static child =>
                     child.ZIndex))
        {
            var hit =
                FindElementAt(
                    child,
                    point,
                    elementOrigin,
                    new Vector2(
                        localRect.Width,
                        localRect.Height));

            if (hit is not null)
            {
                return hit;
            }
        }

        if (point.X >= elementOrigin.X &&
            point.Y >= elementOrigin.Y &&
            point.X <=
                elementOrigin.X +
                localRect.Width &&
            point.Y <=
                elementOrigin.Y +
                localRect.Height)
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