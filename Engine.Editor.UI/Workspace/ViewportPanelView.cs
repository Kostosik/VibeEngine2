using Engine.Core.Math;
using Engine.ECS.Entities;
using Engine.Editor;
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
        base.OnRender(context);

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

        DrawGrid(
            context,
            document);

        DrawEntities(
            context,
            document);

        DrawSelection(
            context,
            document);

        DrawInfo(
            context,
            document);
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
            var screen =
                viewport.Transform.WorldToScreen(
                    new Vector2(
                        worldX,
                        0.0f));

            var thickness =
                MathF.Abs(
                    worldX) < 0.0001f
                        ? AxisLineThickness
                        : GridLineThickness;

            var color =
                MathF.Abs(
                        worldX) <
                    0.0001f
                        ? axisColor
                        : gridColor;

            context.DrawRectangle(
                new UiRect(
                    screen.X -
                    thickness * 0.5f,

                    0.0f,

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
            var screen =
                viewport.Transform.WorldToScreen(
                    new Vector2(
                        0.0f,
                        worldY));

            var thickness =
                MathF.Abs(
                    worldY) < 0.0001f
                        ? AxisLineThickness
                        : GridLineThickness;

            var color =
                MathF.Abs(
                        worldY) <
                    0.0001f
                        ? axisColor
                        : gridColor;

            context.DrawRectangle(
                new UiRect(
                    0.0f,

                    screen.Y -
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
            _isPanning = true;
            _lastPanPosition = pointer.Position;

            pointer.RequestCapture();
            pointer.Handled = true;

            return;
        }

        base.OnPointerDown(
            pointer);

        var document =
            Editor.ActiveDocument;

        if (document is null)
        {
            pointer.Handled = true;
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

        var document =
            Editor.ActiveDocument;

        if (document is null)
        {
            return;
        }

        var zoom =
            document.Viewport.State.Zoom;

        if (zoom <= 0.0f)
        {
            return;
        }

        document.Viewport.State.Pan(
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

        if (pointer.Button !=
            Engine.Input.InputMouseButton.Middle)
        {
            return;
        }

        _isPanning = false;

        pointer.ReleaseCapture();
        pointer.Handled = true;
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

            var screen =
                document.Viewport.Transform.WorldToScreen(
                    new Vector2(
                        position.X,
                        position.Y));

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

            var screen =
                document.Viewport.Transform.WorldToScreen(
                    new Vector2(
                        position.X,
                        position.Y));

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
}