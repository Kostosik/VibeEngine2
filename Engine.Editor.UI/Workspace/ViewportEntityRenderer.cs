using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.Editor.Documents;
using Engine.Graphics.Commands;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

internal static class ViewportEntityRenderer
{
    internal const float EntityMarkerWorldSize = 10.0f;

    private const float SelectionOutlineWorldSize = 10.5f;

    public static void DrawEntities(
        UiRenderContext context,
        EditorDocument document,
        UiRect bounds)
    {
        foreach (var entity in
                 document.World.EcsWorld.Inspector.GetEntities())
        {
            if (!document.World.SpatialEntities.Contains(entity))
            {
                continue;
            }

            var position =
                document.World.SpatialEntities.GetPosition(entity);

            var localScreen =
                document.Viewport.Transform.WorldToScreen(
                    new Vector2(position.X, position.Y));

            var screen = new Vector2(
                bounds.X + localScreen.X,
                bounds.Y + localScreen.Y);

            var zoom = document.Viewport.State.Zoom;
            var color = new UiColor(90, 160, 230, 255);

            if (document.World.EcsWorld.Has<LocalTransform2D>(entity))
            {
                var transform =
                    document.World.EcsWorld.Get<LocalTransform2D>(entity);

                DrawTransformedMarker(
                    context,
                    screen,
                    zoom,
                    EntityMarkerWorldSize,
                    transform,
                    color,
                    showDirection: true,
                    layer: 0);
            }
            else
            {
                context.DrawRectangle(
                    CreateScreenRect(
                        screen,
                        EntityMarkerWorldSize * zoom),
                    color,
                    filled: true);
            }
        }
    }

    public static void DrawSelection(
        UiRenderContext context,
        EditorDocument document,
        UiRect bounds)
    {
        foreach (var entity in document.EntitySelection.Items)
        {
            if (!document.World.EcsWorld.Exists(entity) ||
                !document.World.SpatialEntities.Contains(entity))
            {
                continue;
            }

            var position =
                document.World.SpatialEntities.GetPosition(entity);

            var localScreen =
                document.Viewport.Transform.WorldToScreen(
                    new Vector2(position.X, position.Y));

            var screen = new Vector2(
                bounds.X + localScreen.X,
                bounds.Y + localScreen.Y);

            var zoom = document.Viewport.State.Zoom;
            var selectionColor = new UiColor(255, 220, 80, 255);

            if (document.World.EcsWorld.Has<LocalTransform2D>(entity))
            {
                var transform =
                    document.World.EcsWorld.Get<LocalTransform2D>(entity);

                DrawTransformedMarker(
                    context,
                    screen,
                    zoom,
                    SelectionOutlineWorldSize,
                    transform,
                    selectionColor,
                    showDirection: false,
                    layer: 1);
            }
            else
            {
                context.DrawRectangle(
                    CreateScreenRect(
                        screen,
                        SelectionOutlineWorldSize * zoom),
                    selectionColor,
                    filled: false,
                    layer: 1);
            }
        }
    }

    private static void DrawTransformedMarker(
        UiRenderContext context,
        Vector2 center,
        float zoom,
        float sizeWorld,
        LocalTransform2D transform,
        UiColor color,
        bool showDirection,
        int layer)
    {
        var halfWidth = sizeWorld * 0.5f;
        var halfHeight = sizeWorld * 0.32f;

        var corners = new[]
        {
            TransformLocalPoint(
                center, zoom, transform, -halfWidth, -halfHeight),

            TransformLocalPoint(
                center, zoom, transform, halfWidth, -halfHeight),

            TransformLocalPoint(
                center, zoom, transform, halfWidth, halfHeight),

            TransformLocalPoint(
                center, zoom, transform, -halfWidth, halfHeight)
        };

        DrawClosedOutline(
            context,
            corners,
            color,
            zoom,
            layer);

        if (!showDirection)
        {
            return;
        }

        var directionEnd =
            TransformLocalPoint(
                center,
                zoom,
                transform,
                halfWidth + 2.0f,
                0.0f);

        DrawScreenLine(
            context,
            center,
            directionEnd,
            new UiColor(210, 230, 255, 255),
            zoom,
            layer + 1);

        var centerSize = Math.Clamp(
            2.5f * zoom,
            2.0f,
            5.0f);

        context.DrawRectangle(
            CreateScreenRect(center, centerSize),
            color,
            filled: true,
            layer: layer + 1);
    }

    private static Vector2 TransformLocalPoint(
        Vector2 center,
        float zoom,
        LocalTransform2D transform,
        float localX,
        float localY)
    {
        var scaledX = localX * transform.Scale.X.ToFloat();
        var scaledY = localY * transform.Scale.Y.ToFloat();

        var rotation = transform.Rotation.ToFloat();
        var cosine = MathF.Cos(rotation);
        var sine = MathF.Sin(rotation);

        var rotatedX = scaledX * cosine - scaledY * sine;
        var rotatedY = scaledX * sine + scaledY * cosine;

        return new Vector2(
            center.X + rotatedX * zoom,
            center.Y - rotatedY * zoom);
    }

    private static void DrawClosedOutline(
        UiRenderContext context,
        IReadOnlyList<Vector2> points,
        UiColor color,
        float zoom,
        int layer)
    {
        for (var i = 0; i < points.Count; i++)
        {
            var next = (i + 1) % points.Count;

            DrawScreenLine(
                context,
                points[i],
                points[next],
                color,
                zoom,
                layer);
        }
    }

    private static void DrawScreenLine(
        UiRenderContext context,
        Vector2 start,
        Vector2 end,
        UiColor color,
        float zoom,
        int layer)
    {
        var delta = end - start;

        var length = MathF.Sqrt(
            delta.X * delta.X +
            delta.Y * delta.Y);

        var thickness = Math.Clamp(
            zoom * 1.25f,
            1.25f,
            4.0f);

        var sampleSpacing = MathF.Max(
            thickness * 1.25f,
            1.5f);

        var steps = Math.Clamp(
            (int)MathF.Ceiling(length / sampleSpacing),
            1,
            256);

        for (var i = 0; i <= steps; i++)
        {
            var point = start + delta * (i / (float)steps);

            context.DrawRectangle(
                new UiRect(
                    point.X - thickness * 0.5f,
                    point.Y - thickness * 0.5f,
                    thickness,
                    thickness),
                color,
                filled: true,
                layer: layer);
        }
    }

    private static UiRect CreateScreenRect(
        Vector2 center,
        float size)
    {
        var half = size * 0.5f;

        return new UiRect(
            center.X - half,
            center.Y - half,
            size,
            size);
    }
}