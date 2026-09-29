using Engine.Core.Math;
using Engine.ECS.Entities;
using Engine.Editor;
using Engine.Editor.Commands;
using Engine.Editor.Viewport.Gizmos;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;
using Engine.Worlds.Spatial;

namespace Engine.Editor.UI.Workspace;

public sealed class ViewportPanelView :
    UiPanel
{
    private int _debugRenderCounter;

    private readonly EditorTranslationGizmo _translationGizmo =
    new();

    private bool _isTranslating;
    private Editor.Documents.EditorDocument? _interactionDocument;
    private EditorGizmoAxis _translationAxis;
    private EntityId _translationEntity;
    private Vector2 _translationStartWorld;
    private WorldPosition _translationStartPosition;

    private const float GridMinScreenSpacing = 12.0f;
    private const float GridMaxScreenSpacing = 48.0f;
    private const float GridLineThickness = 1.0f;
    private const float AxisLineThickness = 2.0f;

    private const float MinZoom = 0.1f;
    private const float MaxZoom = 64.0f;
    private const float ZoomStep = 1.15f;
    private bool _isPanning;
    private Vector2 _lastPanPosition;
    private const float EntityMarkerSize = 12.0f;
    private const float SelectionOutlineSize = 18.0f;

    private Vector2 _translationStartMouseWorld;

    public ViewportPanelView(
        EditorContext editor)
    {
        ArgumentNullException.ThrowIfNull(
            editor);

        Editor = editor;

        Background =
            new UiColor(
                18,
                18,
                18,
                255);
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
        var document =
            Editor.ActiveDocument;

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
            DrawGrid(
                context,
                document);

            DrawEntities(
                context,
                document);

            DrawSelection(
                context,
                document);

            DrawTranslationGizmo(
    context,
    document);

            DrawInfo(
                context,
                document);
        }
        finally
        {
            context.PopClip();
        }
    }

    private void DrawTranslationGizmo(
    UiRenderContext context,
    Editor.Documents.EditorDocument document)
    {
        if (!document.Inspector.TryGetSelectedEntity(
                document.EntitySelection,
                out var entity))
        {
            return;
        }

        if (!document.World.SpatialEntities.Contains(
                entity))
        {
            return;
        }

        var position =
            document.World.SpatialEntities.GetPosition(
                entity);

        var screen =
            document.Viewport.Transform.WorldToScreen(
                new Vector2(
                    position.X,
                    position.Y));

        var arm =
            _translationGizmo.ArmLength;

        const float thickness = 3.0f;
        const float arrowSize = 8.0f;

        var xColor =
            new UiColor(
                220,
                80,
                80,
                255);

        var yColor =
            new UiColor(
                80,
                220,
                120,
                255);

        context.DrawRectangle(
            new UiRect(
                screen.X,
                screen.Y - thickness * 0.5f,
                arm,
                thickness),
            xColor,
            filled: true,
            layer: 3);

        context.DrawRectangle(
            new UiRect(
                screen.X - thickness * 0.5f,
                screen.Y - arm,
                thickness,
                arm),
            yColor,
            filled: true,
            layer: 3);

        context.DrawRectangle(
            new UiRect(
                screen.X + arm - arrowSize,
                screen.Y - arrowSize * 0.5f,
                arrowSize,
                arrowSize),
            xColor,
            filled: true,
            layer: 3);

        context.DrawRectangle(
            new UiRect(
                screen.X - arrowSize * 0.5f,
                screen.Y - arm,
                arrowSize,
                arrowSize),
            yColor,
            filled: true,
            layer: 3);
    }

    private void DrawGrid(
    UiRenderContext context,
    Editor.Documents.EditorDocument document)
    {
        var viewport =
            document.Viewport;

        var zoom =
            viewport.State.Zoom;

        if (zoom <= 0.0f)
        {
            return;
        }

        var spacing =
            CalculateGridSpacing(
                zoom);

        var topLeft =
            viewport.Transform.ScreenToWorld(
                Vector2.Zero);

        var bottomRight =
            viewport.Transform.ScreenToWorld(
                new Vector2(
                    Bounds.Width,
                    Bounds.Height));

        var minX =
            MathF.Min(
                topLeft.X,
                bottomRight.X);

        var maxX =
            MathF.Max(
                topLeft.X,
                bottomRight.X);

        var minY =
            MathF.Min(
                topLeft.Y,
                bottomRight.Y);

        var maxY =
            MathF.Max(
                topLeft.Y,
                bottomRight.Y);

        var startX =
            MathF.Floor(
                minX / spacing) *
            spacing;

        var startY =
            MathF.Floor(
                minY / spacing) *
            spacing;

        var gridColor =
            new UiColor(
                35,
                35,
                35,
                255);

        var axisColor =
            new UiColor(
                70,
                70,
                70,
                255);

        for (
            var worldX = startX;
            worldX <= maxX;
            worldX += spacing)
        {
            var localScreen =
                viewport.Transform.WorldToScreen(
                    new Vector2(
                        worldX,
                        0.0f));

            var screenX =
                Bounds.X +
                localScreen.X;

            var thickness =
                MathF.Abs(worldX) <
                0.0001f
                    ? AxisLineThickness
                    : GridLineThickness;

            var color =
                MathF.Abs(worldX) <
                0.0001f
                    ? axisColor
                    : gridColor;

            context.DrawRectangle(
                new UiRect(
                    screenX -
                    thickness * 0.5f,

                    Bounds.Y,

                    thickness,

                    Bounds.Height),

                color,
                filled: true,
                layer: -10);
        }

        for (
            var worldY = startY;
            worldY <= maxY;
            worldY += spacing)
        {
            var localScreen =
                viewport.Transform.WorldToScreen(
                    new Vector2(
                        0.0f,
                        worldY));

            var screenY =
                Bounds.Y +
                localScreen.Y;

            var thickness =
                MathF.Abs(worldY) <
                0.0001f
                    ? AxisLineThickness
                    : GridLineThickness;

            var color =
                MathF.Abs(worldY) <
                0.0001f
                    ? axisColor
                    : gridColor;

            context.DrawRectangle(
                new UiRect(
                    Bounds.X,

                    screenY -
                    thickness * 0.5f,

                    Bounds.Width,

                    thickness),

                color,
                filled: true,
                layer: -10);
        }
    }

    private static float CalculateGridSpacing(
    float zoom)
    {
        var targetWorldSpacing =
            GridMinScreenSpacing /
            zoom;

        if (targetWorldSpacing <= 0.0f)
        {
            return 1.0f;
        }

        var exponent =
            MathF.Floor(
                MathF.Log10(
                    targetWorldSpacing));

        var magnitude =
            MathF.Pow(
                10.0f,
                exponent);

        var normalized =
            targetWorldSpacing /
            magnitude;

        var step =
            normalized <= 1.0f
                ? 1.0f
                : normalized <= 2.0f
                    ? 2.0f
                    : normalized <= 5.0f
                        ? 5.0f
                        : 10.0f;

        return step * magnitude;
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

            var axis =
                _translationGizmo.HitTest(
                    localPosition,
                    selectedScreen);

            if (axis != EditorGizmoAxis.None)
            {
                _isTranslating = true;
                _interactionDocument = document;
                _translationAxis = axis;
                _translationEntity = selectedEntity;
                _translationStartPosition = selectedPosition;
                _translationStartWorld =
                    document.Viewport.Transform.ScreenToWorld(
                        localPosition);

                pointer.RequestCapture();
                pointer.Handled = true;

                return;
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

        if ((_isTranslating || _isPanning) &&
            !ReferenceEquals(
                _interactionDocument,
                Editor.ActiveDocument))
        {
            _isTranslating = false;
            _isPanning = false;

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

            document.World.SpatialEntities.SetPosition(
                _translationEntity,
                new WorldPosition(
                    x,
                    y));

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

    protected override void OnPointerUp(
    UiPointerEvent pointer)
    {
        base.OnPointerUp(
            pointer);

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
                var finalPosition =
                    document.World.SpatialEntities.GetPosition(
                        _translationEntity);

                if (finalPosition !=
                    _translationStartPosition)
                {
                    var reference =
                        document.GetEntityReference(
                            _translationEntity);

                    document.Execute(
                        new SetWorldPositionCommand(
                            document.World,
                            reference,
                            _translationStartPosition,
                            finalPosition));
                }
            }

            _isTranslating = false;

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

    private void DrawEntities(
    UiRenderContext context,
    Editor.Documents.EditorDocument document)
    {
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

            var localScreen =
                document.Viewport.Transform.WorldToScreen(
                    new Vector2(
                        position.X,
                        position.Y));

            var screen =
                new Vector2(
                    Bounds.X +
                    localScreen.X,

                    Bounds.Y +
                    localScreen.Y);

            var rect =
                CreateScreenRect(
                    screen,
                    EntityMarkerSize);

            context.DrawRectangle(
                rect,
                new UiColor(
                    90,
                    160,
                    230,
                    255),
                filled: true);
        }
    }

    private void DrawSelection(
    UiRenderContext context,
    Editor.Documents.EditorDocument document)
    {
        foreach (var entity in
                 document.EntitySelection.Items)
        {
            if (!document.World.EcsWorld.Exists(
                    entity))
            {
                continue;
            }

            if (!document.World.SpatialEntities.Contains(
                    entity))
            {
                continue;
            }

            var position =
                document.World.SpatialEntities.GetPosition(
                    entity);

            var localScreen =
                document.Viewport.Transform.WorldToScreen(
                    new Vector2(
                        position.X,
                        position.Y));

            var screen =
                new Vector2(
                    Bounds.X +
                    localScreen.X,

                    Bounds.Y +
                    localScreen.Y);

            var rect =
                CreateScreenRect(
                    screen,
                    SelectionOutlineSize);

            context.DrawRectangle(
                rect,
                new UiColor(
                    255,
                    220,
                    80,
                    255),
                filled: false,
                layer: 1);
        }
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
            MathF.Max(
                0.5f,
                8.0f /
                document.Viewport.State.Zoom);

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

    private static UiRect CreateScreenRect(
        Vector2 center,
        float size)
    {
        var half =
            size * 0.5f;

        return new UiRect(
            center.X - half,
            center.Y - half,
            size,
            size);
    }

    private bool ContainsLocalPosition(
        Vector2 position)
    {
        return position.X >= 0.0f &&
               position.Y >= 0.0f &&
               position.X <= Bounds.Width &&
               position.Y <= Bounds.Height;
    }

    public void DebugDump()
    {
        var document =
            Editor.ActiveDocument;

        Console.WriteLine();
        Console.WriteLine(
            "========== VIEWPORT DEBUG ==========");

        Console.WriteLine(
            $"Viewport Bounds: " +
            $"X={Bounds.X}, " +
            $"Y={Bounds.Y}, " +
            $"W={Bounds.Width}, " +
            $"H={Bounds.Height}");

        if (document is null)
        {
            Console.WriteLine(
                "Document: NULL");

            Console.WriteLine(
                "====================================");

            return;
        }

        Console.WriteLine(
            $"World entities: " +
            $"{document.World.EcsWorld.EntityCount}");

        Console.WriteLine(
            $"Spatial entities: " +
            $"{document.World.SpatialIndex.EntityCount}");

        Console.WriteLine(
            $"Selected entities: " +
            $"{document.EntitySelection.Count}");

        Console.WriteLine(
            $"Viewport size: " +
            $"{document.Viewport.State.Size.X} x " +
            $"{document.Viewport.State.Size.Y}");

        Console.WriteLine(
            $"Viewport center: " +
            $"{document.Viewport.State.Center}");

        Console.WriteLine(
            $"Viewport zoom: " +
            $"{document.Viewport.State.Zoom}");

        foreach (var entity in
                 document.World
                     .EcsWorld
                     .Inspector
                     .GetEntities())
        {
            Console.WriteLine(
                $"Entity: {entity}");

            var hasPosition =
                document.World.SpatialEntities.Contains(
                    entity);

            Console.WriteLine(
                $"  Has WorldPosition: {hasPosition}");

            if (hasPosition)
            {
                Console.WriteLine(
                    $"  WorldPosition: " +
                    $"{document.World.SpatialEntities.GetPosition(entity)}");
            }

            Console.WriteLine(
                $"  Selected: " +
                $"{document.EntitySelection.Contains(entity)}");
        }

        Console.WriteLine(
            "====================================");
    }
}