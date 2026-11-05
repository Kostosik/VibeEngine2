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
    private readonly EditorUiCanvasSmartGuideRenderer _smartGuideRenderer = new();
    private readonly EditorUiCanvasGizmoRenderer _gizmoRenderer = new();
    private readonly EditorUiDocument _document;
    private readonly EditorUiCanvasElementRenderer _elementRenderer;
    private readonly EditorUiCanvasGridRenderer _gridRenderer = new();
    private readonly EditorUiCanvasViewportTransform _viewportTransform =
    new();
    private Vector2? _previewCanvasSize;
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

    public Vector2 CanvasPreviewSize =>
    _previewCanvasSize ?? _document.Root.Layout.Size;

    public void SetCanvasPreviewSize(Vector2? size)
    {
        if (size is { } value &&
            (!float.IsFinite(value.X) ||
             !float.IsFinite(value.Y) ||
             value.X <= 0.0f ||
             value.Y <= 0.0f))
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        _previewCanvasSize = size;
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
                CanvasPreviewSize,
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
        RenderSmartGuides(context);
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
                CanvasPreviewSize,
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
                selectedTransform.Center,
                selected.Type);

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
            16.0f / scale,
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

        if (_transformTool.HandlePointerMove(pointer))
        {
            ApplySmartGuideSnap();
        }
    }

    private void ApplySmartGuideSnap()
    {
        if (!_transformTool.IsMoving ||
            !TryGetSelectedElement(out var selected) ||
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

        var references = GetSmartGuideReferences(selected, scale);

        var correction = _smartGuideRenderer.GetSnapCorrection(
            GetCanvasRect(),
            transform,
            references);

        if (MathF.Abs(correction.X) < 0.001f &&
            MathF.Abs(correction.Y) < 0.001f)
        {
            return;
        }

        _transformTool.ApplyMoveCorrection(
            new Vector2(
                correction.X / scale,
                correction.Y / scale));
    }

    private List<EditorUiCanvasElementScreenTransform>
    GetSmartGuideReferences(
        EditorUiElement selected,
        float scale)
    {
        var references =
            new List<EditorUiCanvasElementScreenTransform>();

        foreach (var element in _document.EnumerateDepthFirst())
        {
            if (ReferenceEquals(element, _document.Root) ||
                IsInSubtree(element, selected))
            {
                continue;
            }

            if (TryGetElementScreenTransform(
                    element,
                    scale,
                    out var referenceTransform))
            {
                references.Add(referenceTransform);
            }
        }

        return references;
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
        var layout = _transformTool.GetEffectiveLayout(element);

        if (ReferenceEquals(element, _document.Root) &&
            _previewCanvasSize is { } previewSize)
        {
            return layout with { Size = previewSize };
        }

        return layout;
    }

    private void RenderSmartGuides(UiRenderContext context)
    {
        if (!_transformTool.IsMoving ||
            !TryGetSelectedElement(out var selected) ||
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

        var references = GetSmartGuideReferences(
            selected,
            scale);

        foreach (var element in _document.EnumerateDepthFirst())
        {
            // Не сравниваем элемент с самим собой
            // и с его потомками.
            if (ReferenceEquals(element, _document.Root) ||
                IsInSubtree(element, selected))
            {
                continue;
            }

            if (TryGetElementScreenTransform(
                    element,
                    scale,
                    out var referenceTransform))
            {
                references.Add(referenceTransform);
            }
        }

        _smartGuideRenderer.Render(
            context,
            GetCanvasRect(),
            transform,
            references);
    }

    private static bool IsInSubtree(
        EditorUiElement element,
        EditorUiElement ancestor)
    {
        for (var current = element;
             current is not null;
             current = current.Parent)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        return false;
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
            transform.RotationDegrees,selected.Type);
    }

    
    public void ReleaseResources()
    {
        _elementRenderer.Dispose();
    }


    private UiRect GetCanvasRect()
    {
        return _viewportTransform.GetCanvasRect(
            Bounds,
            CanvasPreviewSize);
    }

    private float GetScale()    
    {
        return _viewportTransform.GetScale(
            Bounds,
            CanvasPreviewSize);
    }
}