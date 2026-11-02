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
    private readonly EditorUiCanvasGizmoRenderer _gizmoRenderer = new();
    private readonly EditorUiDocument _document;
    private readonly EditorUiCanvasElementRenderer _elementRenderer;
    private readonly EditorUiCanvasGridRenderer _gridRenderer = new();
    private readonly EditorUiCanvasViewportTransform _viewportTransform =
    new();
    private readonly EditorUiCanvasTransformTool _transformTool;
    public bool ShowGrid { get; set; } = true;
    private enum DragOperation
    {
        None,
        Move,
        Resize,
        Rotate
    }

    public bool SnapToGrid
    {
        get => _transformTool.SnapEnabled;
        set => _transformTool.SnapEnabled = value;
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

        if (ShowGrid)
        {
            _gridRenderer.Render(
                context,
                canvasRect,
                _document.Root.Layout.Size,
                GetScale());
        }

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
            _viewportTransform.BeginPan(pointer.Position);

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
            var gizmoRect = _gizmoRenderer.CreateGizmoRect(
                selectedTransform.LayoutRect,
                selectedTransform.Center);

            var gizmoPoint = EditorUiCanvasElementGeometry.RotatePoint(
                pointer.Position,
                selectedTransform.Center,
                -selectedTransform.RotationDegrees);

            if (_gizmoRenderer.HitTestRotateHandle(gizmoPoint, gizmoRect))
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

            var handle = _gizmoRenderer.HitTestResizeHandle(
                gizmoPoint,
                gizmoRect);

            if (handle != ResizeHandle.None)
            {
                var documentCenter = _viewportTransform.ScreenToDocument(
                    selectedTransform.Center,
                    GetCanvasRect(),
                    scale);

                _transformTool.BeginResize(
                    selected,
                    handle,
                    pointer,
                    scale,
                    selectedTransform.ParentRotationDegrees,
                    selectedTransform.RotationDegrees,
                    selectedTransform.Center,
                    documentCenter);

                pointer.Handled = true;
                return;
            }
        }

        var canvasRect = GetCanvasRect();

        if (!canvasRect.Contains(pointer.Position))
        {
            return;
        }

        var documentPosition = _viewportTransform.ScreenToDocument(
            pointer.Position,
            canvasRect,
            scale);

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
            var documentStartPosition = hitTransform.DocumentOrigin;

            _transformTool.BeginMove(
                hit,
                pointer,
                scale,
                hitTransform.ParentRotationDegrees,
                hitTransform.RotationDegrees,
                hitTransform.Center,
                documentStartPosition);
        }

        pointer.Handled = true;
    }

    protected override void OnPointerMove(
    UiPointerEvent pointer)
    {
        if (_viewportTransform.UpdatePan(pointer.Position))
        {
            pointer.Handled = true;
            return;
        }

        _transformTool.HandlePointerMove(pointer);
    }

    protected override void OnPointerUp(
    UiPointerEvent pointer)
    {
        if (pointer.Button == InputMouseButton.Middle &&
            _viewportTransform.EndPan())
        {
            pointer.ReleaseCapture();
            pointer.Handled = true;
            return;
        }

        _transformTool.HandlePointerUp(pointer);
    }

    private EditorUiLayout GetEffectiveLayout(
        EditorUiElement element)
    {
        return _transformTool.GetEffectiveLayout(element);
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
    out EditorUiCanvasElementScreenTransform transform)
    {
        return EditorUiCanvasElementGeometry.TryGetScreenTransform(
            _document,
            element,
            GetCanvasRect(),
            scale,
            GetEffectiveLayout,
            out transform);
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

        _gizmoRenderer.Render(
            context,
            transform.LayoutRect,
            transform.Center,
            transform.RotationDegrees);
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