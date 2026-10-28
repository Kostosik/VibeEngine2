using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Editor.Commands;
using Engine.Editor.Viewport.Gizmos;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Sprites;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;
using Engine.Worlds.Spatial;

namespace Engine.Editor.UI.Workspace;

public sealed class ViewportPanelView :
    UiPanel
{
    private enum TransformTool
    {
        Move,
        Rotate,
        Scale
    }
    private readonly ViewportProjectTextureCache
    _projectTextureCache;
    private readonly ViewportSpriteRenderer _projectSpriteRenderer;

    private TransformTool _activeTool = TransformTool.Move;
    private bool _translationUsesLocalTransform;
    private LocalTransform2D _translationStartLocalTransform;
    private readonly UiPanel _toolBar;
    private readonly UiStackPanel _toolButtons;

    private readonly UiButton _moveToolButton;
    private readonly UiButton _rotateToolButton;
    private readonly UiButton _scaleToolButton;

    private readonly EditorTranslationGizmo _translationGizmo =
    new();

    private bool _isRotating;
    private bool _isScaling;

    private EntityId _transformEntity;
    private LocalTransform2D _transformStartValue;
    private EditorGizmoAxis _transformAxis;

    private Vector2 _transformStartMousePosition;
    private Vector2 _transformCenterScreen;

    private float _rotationStartMouseAngle;

    private bool _isTranslating;
    private Editor.Documents.EditorDocument? _interactionDocument;
    private EditorGizmoAxis _translationAxis;
    private EntityId _translationEntity;
    private Vector2 _translationStartWorld;
    private WorldPosition _translationStartPosition;
    private bool _translationUsesWorldTransform;
    private WorldTransform2D _translationStartWorldTransform;

    private const float MinZoom = 0.1f;
    private const float MaxZoom = 64.0f;
    private const float ZoomStep = 1.15f;
    private bool _isPanning;
    private Vector2 _lastPanPosition;

    public ViewportPanelView(
        EditorContext editor,
        ITextureManager? projectTextureManager = null)
    {
        ArgumentNullException.ThrowIfNull(editor);

        Editor = editor;
        _projectTextureCache =
    new ViewportProjectTextureCache(
        projectTextureManager);
        _projectSpriteRenderer =
    new ViewportSpriteRenderer(
        _projectTextureCache);
        Background =
            new UiColor(
                18,
                18,
                18,
                255);

        _toolButtons = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 4.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        _moveToolButton = CreateToolButton("Move");
        _rotateToolButton = CreateToolButton("Rotate");
        _scaleToolButton = CreateToolButton("Scale");

        _moveToolButton.Clicked +=
            () => SetActiveTool(TransformTool.Move);

        _rotateToolButton.Clicked +=
            () => SetActiveTool(TransformTool.Rotate);

        _scaleToolButton.Clicked +=
            () => SetActiveTool(TransformTool.Scale);

        _toolButtons.AddChild(_moveToolButton);
        _toolButtons.AddChild(_rotateToolButton);
        _toolButtons.AddChild(_scaleToolButton);

        _toolBar = new UiPanel
        {
            Width = 274.0f,
            Height = 38.0f,
            Margin = new UiThickness(8.0f),
            Padding = new UiThickness(4.0f),
            Background = new UiColor(32, 32, 32, 230),
            HorizontalAlignment = UiHorizontalAlignment.Left,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        _toolBar.AddChild(_toolButtons);
        AddChild(_toolBar);

        RefreshToolButtons();
    }

    private static UiButton CreateToolButton(string text)
    {
        return new UiButton(text)
        {
            Width = 86.0f,
            Height = 30.0f,
            Focusable = false
        };
    }

    private void SetActiveTool(TransformTool tool)
    {
        if (_activeTool == tool)
        {
            return;
        }

        _activeTool = tool;
        RefreshToolButtons();
    }

    private void RefreshToolButtons()
    {
        _moveToolButton.Text =
            _activeTool == TransformTool.Move
                ? "> Move"
                : "Move";

        _rotateToolButton.Text =
            _activeTool == TransformTool.Rotate
                ? "> Rotate"
                : "Rotate";

        _scaleToolButton.Text =
            _activeTool == TransformTool.Scale
                ? "> Scale"
                : "Scale";
    }

    protected override void OnPointerWheel(
    UiPointerEvent pointer)
    {
        base.OnPointerWheel(
            pointer);

        var document =
            Editor.ActiveDocument;

        if (document is null ||
            MathF.Abs(pointer.ScrollDelta) < 0.0001f)
        {
            return;
        }

        var localPosition =
            new Vector2(
                pointer.Position.X -
                Bounds.X,

                pointer.Position.Y -
                Bounds.Y);

        if (!ContainsLocalPosition(
                localPosition))
        {
            return;
        }

        var worldBefore =
            document.Viewport.Transform.ScreenToWorld(
                localPosition);

        var factor =
            MathF.Pow(
                ZoomStep,
                pointer.ScrollDelta);

        var zoom =
            Math.Clamp(
                document.Viewport.State.Zoom * factor,
                MinZoom,
                MaxZoom);

        if (MathF.Abs(
                zoom -
                document.Viewport.State.Zoom) <
            0.000001f)
        {
            return;
        }

        document.Viewport.State.SetZoom(
            zoom);

        var worldAfter =
            document.Viewport.Transform.ScreenToWorld(
                localPosition);

        document.Viewport.State.Pan(
            worldBefore -
            worldAfter);

        pointer.Handled = true;
    }

    public EditorContext Editor { get; }

    public void Refresh()
    {
        var document = Editor.ActiveDocument;

        _toolBar.Visible = document is not null;

        if (document is null)
        {
            return;
        }

        if (Bounds.Width <= 0.0f ||
            Bounds.Height <= 0.0f)
        {
            return;
        }

        document.Viewport.State.SetSize(
            new Vector2(
                Bounds.Width,
                Bounds.Height));
    }

    protected override void OnRender(
     UiRenderContext context)
    {
        base.OnRender(
            context);

        var document =
            Editor.ActiveDocument;


        if (document is null)
        {
            context.DrawText(
                "No document",
                new Vector2(
                    Bounds.X + 12.0f,
                    Bounds.Y + 12.0f),
                13.0f,
                new UiColor(
                    150,
                    150,
                    150,
                    255));

            return;
        }

        if (Bounds.Width <= 0.0f ||
            Bounds.Height <= 0.0f)
        {
            return;
        }

        document.Viewport.State.SetSize(
            new Vector2(
                Bounds.Width,
                Bounds.Height));

        context.PushClip(
            Bounds);

        try
        {
            ViewportGridRenderer.Draw(
                context,
                document,
                Bounds);
            DrawSprites(context, document);
            ViewportEntityRenderer.DrawEntities(
                context,
                document,
                Bounds);

            ViewportEntityRenderer.DrawSelection(
                context,
                document,
                Bounds);

            DrawActiveGizmo(context, document);

            DrawInfo(
                context,
                document);
        }
        finally
        {
            context.PopClip();
        }
    }

    private void DrawActiveGizmo(
        UiRenderContext context,
        Engine.Editor.Documents.EditorDocument document)
    {
        switch (_activeTool)
        {
            case TransformTool.Move:
                ViewportGizmoRenderer.DrawTranslation(
                    context,
                    document,
                    Bounds,
                    _translationGizmo.ArmLength);
                break;

            case TransformTool.Rotate:
                ViewportGizmoRenderer.DrawRotation(
                    context,
                    document,
                    Bounds);
                break;

            case TransformTool.Scale:
                ViewportGizmoRenderer.DrawScale(
                    context,
                    document,
                    Bounds);
                break;
        }
    }

    private void DrawSprites(
        UiRenderContext context,
        Engine.Editor.Documents.EditorDocument document)
    {
        _projectSpriteRenderer.Draw(
            context,
            document,
            Editor.ActiveGameProject?.AssetsDirectoryPath,
            Bounds);
    }

    public void ReleaseResources()
    {
        _projectTextureCache.ReleaseResources();
    }

    private static void DrawSpriteTexturePlaceholder(
        UiRenderContext context,
        UiRect rect,
        int layer)
    {
        if (rect.Width < 2.0f || rect.Height < 2.0f)
        {
            return;
        }

        context.DrawRectangle(
            rect,
            new UiColor(190, 65, 65, 220),
            filled: false,
            layer: layer);

        if (rect.Width >= 80.0f && rect.Height >= 20.0f)
        {
            context.DrawText(
                "Texture unavailable",
                new Vector2(rect.X + 4.0f, rect.Y + 4.0f),
                10.0f,
                new UiColor(235, 150, 150, 255),
                layer: layer);
        }
    }

    private bool TryGetSelectedScreenPosition(
    Engine.Editor.Documents.EditorDocument document,
    out Vector2 screen)
    {
        screen = default;

        if (!document.Inspector.TryGetSelectedEntity(
                document.EntitySelection,
                out var entity) ||
            !document.World.SpatialEntities.Contains(entity))
        {
            return false;
        }

        var position =
            document.World.SpatialEntities.GetPosition(entity);

        var localScreen =
            document.Viewport.Transform.WorldToScreen(
                new Vector2(position.X, position.Y));

        screen = new Vector2(
            Bounds.X + localScreen.X,
            Bounds.Y + localScreen.Y);

        return true;
    }

    protected override void OnPointerDown(
    UiPointerEvent pointer)
    {
        if (pointer.Button ==
     Engine.Input.InputMouseButton.Middle)
        {
            _interactionDocument =
                Editor.ActiveDocument;

            _isPanning = true;
            _lastPanPosition =
                pointer.Position;

            pointer.RequestCapture();
            pointer.Handled = true;

            return;
        }

        var document =
            Editor.ActiveDocument;

        if (document is null)
        {
            return;
        }

        var localPosition =
            new Vector2(
                pointer.Position.X - Bounds.X,
                pointer.Position.Y - Bounds.Y);

        if (!ContainsLocalPosition(
                localPosition))
        {
            return;
        }

        if (document.Inspector.TryGetSelectedEntity(
                document.EntitySelection,
                out var selectedEntity) &&
            document.World.SpatialEntities.Contains(
                selectedEntity))
        {
            var selectedPosition =
                document.World.SpatialEntities.GetPosition(
                    selectedEntity);

            var selectedScreen =
                document.Viewport.Transform.WorldToScreen(
                    new Vector2(
                        selectedPosition.X,
                        selectedPosition.Y));

            if (_activeTool == TransformTool.Move)
            {
                var axis =
                    _translationGizmo.HitTest(
                        localPosition,
                        selectedScreen);

                if (axis != EditorGizmoAxis.None)
                {
                    var ecs =
                        document.World.EcsWorld;

                    var usesLocalTransform =
                        ecs.Has<LocalTransform2D>(
                            selectedEntity);

                    var usesWorldTransform =
                        !usesLocalTransform &&
                        ecs.Has<WorldTransform2D>(
                            selectedEntity);

                    // Spatial-only сущности не перемещаем через gizmo.
                    if (!usesLocalTransform &&
                        !usesWorldTransform)
                    {
                        pointer.Handled = true;
                        return;
                    }

                    _isTranslating = true;
                    _interactionDocument = document;
                    _translationAxis = axis;
                    _translationEntity = selectedEntity;

                    _translationStartPosition =
                        selectedPosition;

                    _translationUsesLocalTransform =
                        usesLocalTransform;

                    _translationUsesWorldTransform =
                        usesWorldTransform;

                    if (_translationUsesLocalTransform)
                    {
                        _translationStartLocalTransform =
                            ecs.Get<LocalTransform2D>(
                                selectedEntity);
                    }
                    else
                    {
                        _translationStartWorldTransform =
                            ecs.Get<WorldTransform2D>(
                                selectedEntity);
                    }

                    _translationStartWorld =
                        document.Viewport.Transform.ScreenToWorld(
                            localPosition);

                    pointer.RequestCapture();
                    pointer.Handled = true;
                    return;
                }
            }
            else if (document.World.EcsWorld.Has<LocalTransform2D>(
                         selectedEntity))
            {
                var hitRotation =
                    _activeTool == TransformTool.Rotate &&
                    IsNearRotationRing(
                        localPosition,
                        selectedScreen);

                var scaleAxis =
                    _activeTool == TransformTool.Scale
                        ? HitTestScaleHandle(
                            localPosition,
                            selectedScreen)
                        : EditorGizmoAxis.None;

                if (hitRotation ||
                    scaleAxis != EditorGizmoAxis.None)
                {
                    _interactionDocument = document;
                    _transformEntity = selectedEntity;

                    _transformStartValue =
                        document.World.EcsWorld.Get<LocalTransform2D>(
                            selectedEntity);

                    _transformStartMousePosition =
                        localPosition;

                    _transformCenterScreen =
                        selectedScreen;

                    _isRotating = hitRotation;
                    _isScaling = !hitRotation;
                    _transformAxis = scaleAxis;

                    _rotationStartMouseAngle =
                        MathF.Atan2(
                            localPosition.Y - selectedScreen.Y,
                            localPosition.X - selectedScreen.X);

                    pointer.RequestCapture();
                    pointer.Handled = true;
                    return;
                }
            }
        }

        var worldPosition =
            document.Viewport.Transform.ScreenToWorld(
                localPosition);

        var entity =
            FindEntityAt(
                document,
                worldPosition);

        if (entity.HasValue)
        {
            document.EntitySelection.Set(
                entity.Value);
        }
        else
        {
            document.EntitySelection.Clear();
        }

        pointer.Handled = true;
    }

    protected override void OnPointerMove(
    UiPointerEvent pointer)
    {
        base.OnPointerMove(
            pointer);

        if ((_isTranslating ||
             _isRotating ||
             _isScaling ||
             _isPanning) &&
            !ReferenceEquals(
                _interactionDocument,
                Editor.ActiveDocument))
        {
            _isTranslating = false;
            _isPanning = false;
            _isRotating = false;
            _isScaling = false;

            _translationUsesWorldTransform = false;
            _translationUsesLocalTransform = false;

            _transformAxis = EditorGizmoAxis.None;
            _transformEntity = EntityId.Invalid;

            _translationAxis =
                EditorGizmoAxis.None;

            _translationEntity =
                EntityId.Invalid;

            _interactionDocument =
                null;

            return;
        }

        if (_isTranslating &&
            _translationAxis !=
            EditorGizmoAxis.None)
        {
            var document =
                _interactionDocument;

            if (document is null ||
                !document.World.EcsWorld.Exists(
                    _translationEntity))
            {
                return;
            }

            var localPosition =
                new Vector2(
                    pointer.Position.X -
                    Bounds.X,
                    pointer.Position.Y -
                    Bounds.Y);

            var worldPosition =
                document.Viewport.Transform.ScreenToWorld(
                    localPosition);

            var deltaPos =
                worldPosition -
                _translationStartWorld;

            var x =
                _translationStartPosition.X;

            var y =
                _translationStartPosition.Y;

            if (_translationAxis ==
                EditorGizmoAxis.X)
            {
                x =
                    (int)MathF.Round(
                        _translationStartPosition.X +
                        deltaPos.X,
                        MidpointRounding.AwayFromZero);
            }
            else if (_translationAxis ==
                     EditorGizmoAxis.Y)
            {
                y =
                    (int)MathF.Round(
                        _translationStartPosition.Y +
                        deltaPos.Y,
                        MidpointRounding.AwayFromZero);
            }

            if (_translationUsesLocalTransform &&
                document.World.EcsWorld.Has<LocalTransform2D>(
                    _translationEntity))
            {
                var worldDelta = new FixedVector2(
                    Fixed32.FromInt(
                        x - _translationStartPosition.X),
                    Fixed32.FromInt(
                        y - _translationStartPosition.Y));

                if (TryConvertWorldDeltaToLocal(
                        document,
                        _translationEntity,
                        worldDelta,
                        out var localDelta))
                {
                    ref var transform =
                        ref document.World.EcsWorld.Get<LocalTransform2D>(
                            _translationEntity);

                    transform.Position =
                        _translationStartLocalTransform.Position +
                        localDelta;
                }
            }
            else if (_translationUsesWorldTransform &&
         document.World.EcsWorld.Has<WorldTransform2D>(
             _translationEntity))
            {
                var worldDelta =
                    new FixedVector2(
                        Fixed32.FromInt(
                            x - _translationStartPosition.X),
                        Fixed32.FromInt(
                            y - _translationStartPosition.Y));

                ref var transform =
                    ref document.World.EcsWorld.Get<WorldTransform2D>(
                        _translationEntity);

                transform.Position =
                    _translationStartWorldTransform.Position +
                    worldDelta;
            }
            document.RefreshTransforms();
            pointer.Handled = true;

            return;
        }

        if ((_isRotating || _isScaling) &&
    _interactionDocument is { } transformDocument)
        {
            if (!transformDocument.World.EcsWorld.Exists(
                    _transformEntity) ||
                !transformDocument.World.EcsWorld.Has<LocalTransform2D>(
                    _transformEntity))
            {
                return;
            }

            var currentMousePosition =
                new Vector2(
                    pointer.Position.X - Bounds.X,
                    pointer.Position.Y - Bounds.Y);

            var value =
                _transformStartValue;

            if (_isRotating)
            {
                var currentAngle =
                    MathF.Atan2(
                        currentMousePosition.Y -
                            _transformCenterScreen.Y,
                        currentMousePosition.X -
                            _transformCenterScreen.X);

                var deltaAngle =
                    NormalizeAngle(
                        currentAngle -
                        _rotationStartMouseAngle);

                // В экранных координатах ось Y направлена вниз,
                // поэтому знак угла инвертируется.
                value.Rotation =
                    _transformStartValue.Rotation +
                    Fixed32.FromFloat(-deltaAngle);
            }
            else if (_isScaling)
            {
                var deltaX =
                    (currentMousePosition.X -
                     _transformStartMousePosition.X) /
                    _translationGizmo.ArmLength;

                var deltaY =
                    (_transformStartMousePosition.Y -
                     currentMousePosition.Y) /
                    _translationGizmo.ArmLength;

                var startScale =
                    _transformStartValue.Scale;

                var scaleX =
                    startScale.X.ToFloat();

                var scaleY =
                    startScale.Y.ToFloat();

                if (_transformAxis == EditorGizmoAxis.X)
                {
                    scaleX =
                        ClampScale(
                            scaleX + deltaX,
                            startScale.X.ToFloat());
                }
                else if (_transformAxis == EditorGizmoAxis.Y)
                {
                    scaleY =
                        ClampScale(
                            scaleY + deltaY,
                            startScale.Y.ToFloat());
                }

                value.Scale =
                    new FixedVector2(
                        Fixed32.FromFloat(scaleX),
                        Fixed32.FromFloat(scaleY));
            }

            transformDocument.World.EcsWorld
                .Get<LocalTransform2D>(_transformEntity) = value;
            transformDocument.RefreshTransforms();
            pointer.Handled = true;
            return;
        }

        if (!_isPanning ||
            pointer.Button !=
            Engine.Input.InputMouseButton.Middle)
        {
            return;
        }

        var delta =
            new Vector2(
                pointer.Position.X -
                _lastPanPosition.X,
                pointer.Position.Y -
                _lastPanPosition.Y);

        _lastPanPosition =
            pointer.Position;

        var documentForPan =
            _interactionDocument;

        if (documentForPan is null)
        {
            return;
        }

        var zoom =
            documentForPan.Viewport.State.Zoom;

        if (zoom <= 0.0f)
        {
            return;
        }

        documentForPan.Viewport.State.Pan(
            new Vector2(
                -delta.X / zoom,
                delta.Y / zoom));

        pointer.Handled = true;
    }

    private static bool TryConvertWorldDeltaToLocal(
    Editor.Documents.EditorDocument document,
    EntityId entity,
    FixedVector2 worldDelta,
    out FixedVector2 localDelta)
    {
        var ecs = document.World.EcsWorld;

        localDelta = worldDelta;

        if (!ecs.Has<TransformParent2D>(entity))
        {
            return true;
        }

        var parentEntity =
            ecs.Get<TransformParent2D>(entity).Parent;

        if (!ecs.Exists(parentEntity) ||
            !ecs.Has<WorldTransform2D>(parentEntity))
        {
            return false;
        }

        var parentTransform =
            ecs.Get<WorldTransform2D>(parentEntity);

        if (parentTransform.Scale.X == Fixed32.Zero ||
            parentTransform.Scale.Y == Fixed32.Zero)
        {
            return false;
        }

        var cosine =
            Fixed32.Cos(parentTransform.Rotation);

        var sine =
            Fixed32.Sin(parentTransform.Rotation);

        // Обратное вращение относительно родителя.
        var unrotatedX =
            worldDelta.X * cosine +
            worldDelta.Y * sine;

        var unrotatedY =
            worldDelta.Y * cosine -
            worldDelta.X * sine;

        // Учитываем масштаб родителя.
        localDelta = new FixedVector2(
            unrotatedX / parentTransform.Scale.X,
            unrotatedY / parentTransform.Scale.Y);

        return true;
    }

    protected override void OnPointerUp(
    UiPointerEvent pointer)
    {
        base.OnPointerUp(
            pointer);

        if ((_isRotating || _isScaling) &&
    pointer.Button ==
        Engine.Input.InputMouseButton.Left)
        {
            var document =
                _interactionDocument;

            if (document is not null &&
                document.World.EcsWorld.Exists(
                    _transformEntity) &&
                document.World.EcsWorld.Has<LocalTransform2D>(
                    _transformEntity))
            {
                var finalValue =
                    document.World.EcsWorld.Get<LocalTransform2D>(
                        _transformEntity);

                if (!AreTransformValuesEqual(
                        _transformStartValue,
                        finalValue))
                {
                    document.Execute(
                        new SetLocalTransform2DCommand(
                            document,
                            document.GetEntityReference(
                                _transformEntity),
                            _transformStartValue,
                            finalValue));
                }
            }

            _isRotating = false;
            _isScaling = false;

            _transformAxis =
                EditorGizmoAxis.None;

            _transformEntity =
                EntityId.Invalid;

            _interactionDocument = null;

            pointer.ReleaseCapture();
            pointer.Handled = true;
            return;
        }

        if (_isTranslating &&
            pointer.Button ==
            Engine.Input.InputMouseButton.Left)
        {
            var document =
                _interactionDocument;

            if (document is not null &&
    document.World.EcsWorld.Exists(
        _translationEntity))
            {
                var reference =
                    document.GetEntityReference(
                        _translationEntity);

                if (_translationUsesLocalTransform &&
                    document.World.EcsWorld.Has<LocalTransform2D>(
                        _translationEntity))
                {
                    var finalTransform =
                        document.World.EcsWorld.Get<LocalTransform2D>(
                            _translationEntity);

                    if (finalTransform.Position !=
                        _translationStartLocalTransform.Position)
                    {
                        document.Execute(
                            new SetLocalTransform2DCommand(
                                document,
                                reference,
                                _translationStartLocalTransform,
                                finalTransform));
                    }
                }
                else if (_translationUsesWorldTransform &&
                         document.World.EcsWorld.Has<WorldTransform2D>(
                             _translationEntity))
                {
                    var finalTransform =
                        document.World.EcsWorld.Get<WorldTransform2D>(
                            _translationEntity);

                    if (finalTransform.Position !=
                        _translationStartWorldTransform.Position)
                    {
                        document.Execute(
                            new SetWorldTransform2DCommand(
                                document.World,
                                reference,
                                _translationStartWorldTransform,
                                finalTransform));
                    }
                }
            }
            _translationUsesLocalTransform = false;
            _isTranslating = false;
            _translationUsesWorldTransform = false;

            _translationAxis =
                EditorGizmoAxis.None;

            _translationEntity =
                EntityId.Invalid;

            _interactionDocument =
                null;

            pointer.ReleaseCapture();
            pointer.Handled = true;

            return;
        }

        if (pointer.Button ==
            Engine.Input.InputMouseButton.Middle)
        {
            _isPanning = false;

            _interactionDocument =
                null;

            pointer.ReleaseCapture();
            pointer.Handled = true;

            return;
        }
    }

    private bool IsNearRotationRing(
    Vector2 position,
    Vector2 center)
    {
        const float radius = 32.0f;
        const float tolerance = 8.0f;

        var dx = position.X - center.X;
        var dy = position.Y - center.Y;

        var distance =
            MathF.Sqrt(dx * dx + dy * dy);

        return MathF.Abs(distance - radius) <= tolerance;
    }

    private EditorGizmoAxis HitTestScaleHandle(
        Vector2 position,
        Vector2 center)
    {
        var arm =
            _translationGizmo.ArmLength;

        var xHandle =
            new Vector2(
                center.X + arm,
                center.Y);

        var yHandle =
            new Vector2(
                center.X,
                center.Y - arm);

        var xDistance =
            DistanceSquared(position, xHandle);

        var yDistance =
            DistanceSquared(position, yHandle);

        const float hitRadius = 10.0f;
        var hitRadiusSquared = hitRadius * hitRadius;

        var hitX = xDistance <= hitRadiusSquared;
        var hitY = yDistance <= hitRadiusSquared;

        if (!hitX && !hitY)
            return EditorGizmoAxis.None;

        if (hitX && hitY)
        {
            return xDistance <= yDistance
                ? EditorGizmoAxis.X
                : EditorGizmoAxis.Y;
        }

        return hitX
            ? EditorGizmoAxis.X
            : EditorGizmoAxis.Y;
    }

    private static float DistanceSquared(
        Vector2 left,
        Vector2 right)
    {
        var dx = left.X - right.X;
        var dy = left.Y - right.Y;

        return dx * dx + dy * dy;
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI)
            angle -= 2.0f * MathF.PI;

        while (angle < -MathF.PI)
            angle += 2.0f * MathF.PI;

        return angle;
    }

    private static float ClampScale(
        float value,
        float originalValue)
    {
        const float minimumMagnitude = 0.01f;

        if (MathF.Abs(value) >= minimumMagnitude)
            return value;

        return originalValue < 0.0f
            ? -minimumMagnitude
            : minimumMagnitude;
    }

    private static bool AreTransformValuesEqual(
        LocalTransform2D left,
        LocalTransform2D right)
    {
        return left.Position == right.Position &&
               left.Rotation == right.Rotation &&
               left.Scale == right.Scale;
    }

    private void DrawInfo(
        UiRenderContext context,
        Editor.Documents.EditorDocument document)
    {
        var selectedCount =
            document.EntitySelection.Count;

        var text =
            $"Entities: " +
            $"{document.World.EcsWorld.EntityCount}  " +
            $"Selected: " +
            $"{selectedCount}  " +
            $"Zoom: " +
            $"{document.Viewport.State.Zoom:0.00}";

        context.DrawText(
            text,
            new Vector2(
                Bounds.X + 8.0f,
                Bounds.Bottom - 22.0f),
            12.0f,
            new UiColor(
                170,
                170,
                170,
                255));
    }

    private EntityId? FindEntityAt(
        Editor.Documents.EditorDocument document,
        Vector2 worldPosition)
    {
        var bestEntity =
            EntityId.Invalid;

        var bestDistanceSquared =
            float.MaxValue;

        var hitRadius =
            ViewportEntityRenderer.EntityMarkerWorldSize *
            0.5f;

        var hitRadiusSquared =
            hitRadius *
            hitRadius;

        foreach (var entity in
                 document.World
                     .EcsWorld
                     .Inspector
                     .GetEntities())
        {
            if (!document.World.SpatialEntities.Contains(
                    entity))
            {
                continue;
            }

            var position =
                document.World.SpatialEntities.GetPosition(
                    entity);

            var dx =
                position.X -
                worldPosition.X;

            var dy =
                position.Y -
                worldPosition.Y;

            var distanceSquared =
                dx * dx +
                dy * dy;

            if (distanceSquared >
                hitRadiusSquared)
            {
                continue;
            }

            if (distanceSquared >=
                bestDistanceSquared)
            {
                continue;
            }

            bestDistanceSquared =
                distanceSquared;

            bestEntity =
                entity;
        }

        return bestEntity.IsValid
            ? bestEntity
            : null;
    }

    private bool ContainsLocalPosition(
        Vector2 position)
    {
        return position.X >= 0.0f &&
               position.Y >= 0.0f &&
               position.X <= Bounds.Width &&
               position.Y <= Bounds.Height;
    }
}