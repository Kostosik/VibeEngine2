using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Editor.Documents;
using Engine.Graphics.Commands;
using Engine.Graphics.Tilemaps;
using Engine.Graphics2D.Sprites;
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
        var ecs = document.World.EcsWorld;

        foreach (var entity in ecs.Inspector.GetEntities())
        {
            if (!document.World.SpatialEntities.Contains(entity))
            {
                continue;
            }
            if (ecs.Has<TilemapComponent>(entity) &&
    ecs.Has<WorldTransform2D>(entity) &&
    !string.IsNullOrWhiteSpace(
        ecs.Get<TilemapComponent>(entity).TilemapPath))
            {
                continue;
            }
            if (TryGetRenderableSprite(
                    document,
                    entity,
                    out _,
                    out _))
            {
                continue;
            }

            var position = GetWorldPosition(
                document,
                entity);

            var localScreen =
                document.Viewport.Transform.WorldToScreen(position);

            var screen = new Vector2(
                bounds.X + localScreen.X,
                bounds.Y + localScreen.Y);

            var scaleX = 1.0f;
            var scaleY = 1.0f;
            var rotation = 0.0f;

            if (ecs.Has<WorldTransform2D>(entity))
            {
                var transform =
                    ecs.Get<WorldTransform2D>(entity);

                scaleX = transform.Scale.X.ToFloat();
                scaleY = transform.Scale.Y.ToFloat();
                rotation = transform.Rotation.ToFloat();
            }

            DrawTransformedMarker(
                context,
                screen,
                document.Viewport.State.Zoom,
                EntityMarkerWorldSize,
                scaleX,
                scaleY,
                rotation,
                new UiColor(90, 160, 230, 255),
                showDirection: true,
                layer: 0);
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

            var selectionColor =
                new UiColor(255, 220, 80, 255);

            if (TryGetRenderableSprite(
                    document,
                    entity,
                    out var sprite,
                    out var spriteTransform))
            {
                DrawSpriteSelectionOutline(
                    context,
                    document,
                    bounds,
                    sprite,
                    spriteTransform,
                    selectionColor);

                continue;
            }

            var ecs = document.World.EcsWorld;

            if (ecs.Has<TilemapComponent>(entity) &&
                !string.IsNullOrWhiteSpace(
                    ecs.Get<TilemapComponent>(entity).TilemapPath))
            {
                continue;
            }

            var position = GetWorldPosition(
                document,
                entity);

            var localScreen =
                document.Viewport.Transform.WorldToScreen(position);

            var screen = new Vector2(
                bounds.X + localScreen.X,
                bounds.Y + localScreen.Y);

            var scaleX = 1.0f;
            var scaleY = 1.0f;
            var rotation = 0.0f;

            if (document.World.EcsWorld.Has<WorldTransform2D>(entity))
            {
                var transform =
                    document.World.EcsWorld.Get<WorldTransform2D>(entity);

                scaleX = transform.Scale.X.ToFloat();
                scaleY = transform.Scale.Y.ToFloat();
                rotation = transform.Rotation.ToFloat();
            }

            DrawTransformedMarker(
                context,
                screen,
                document.Viewport.State.Zoom,
                SelectionOutlineWorldSize,
                scaleX,
                scaleY,
                rotation,
                selectionColor,
                showDirection: false,
                layer: 1);
        }
    }

    private static bool TryGetRenderableSprite(
    EditorDocument document,
    EntityId entity,
    out SpriteComponent sprite,
    out WorldTransform2D transform)
    {
        var ecs = document.World.EcsWorld;

        sprite = default;
        transform = default;

        if (!ecs.Has<SpriteComponent>(entity) ||
            !ecs.Has<WorldTransform2D>(entity))
        {
            return false;
        }

        sprite = ecs.Get<SpriteComponent>(entity);
        transform = ecs.Get<WorldTransform2D>(entity);

        if (string.IsNullOrWhiteSpace(sprite.TexturePath) ||
            !float.IsFinite(sprite.Width) ||
            !float.IsFinite(sprite.Height) ||
            sprite.Width <= 0.0f ||
            sprite.Height <= 0.0f)
        {
            return false;
        }

        var width =
            sprite.Width *
            MathF.Abs(transform.Scale.X.ToFloat());

        var height =
            sprite.Height *
            MathF.Abs(transform.Scale.Y.ToFloat());

        return float.IsFinite(width) &&
               float.IsFinite(height) &&
               width > 0.0f &&
               height > 0.0f;
    }

    private static Vector2 GetWorldPosition(
        EditorDocument document,
        EntityId entity)
    {
        var ecs = document.World.EcsWorld;

        if (ecs.Has<WorldTransform2D>(entity))
        {
            var transform =
                ecs.Get<WorldTransform2D>(entity);

            return new Vector2(
                transform.Position.X.ToFloat(),
                transform.Position.Y.ToFloat());
        }

        var position =
            document.World.SpatialEntities.GetPosition(entity);

        return new Vector2(position.X, position.Y);
    }

    private static void DrawSpriteSelectionOutline(
        UiRenderContext context,
        EditorDocument document,
        UiRect bounds,
        SpriteComponent sprite,
        WorldTransform2D transform,
        UiColor color)
    {
        var width =
            sprite.Width *
            MathF.Abs(transform.Scale.X.ToFloat());

        var height =
            sprite.Height *
            MathF.Abs(transform.Scale.Y.ToFloat());

        if (!float.IsFinite(width) ||
            !float.IsFinite(height) ||
            width <= 0.0f ||
            height <= 0.0f)
        {
            return;
        }

        var halfWidth = width * 0.5f;
        var halfHeight = height * 0.5f;

        var worldX = transform.Position.X.ToFloat();
        var worldY = transform.Position.Y.ToFloat();

        var rotation = transform.Rotation.ToFloat();
        var cosine = MathF.Cos(rotation);
        var sine = MathF.Sin(rotation);

        var localCorners = new[]
        {
        new Vector2(-halfWidth, -halfHeight),
        new Vector2( halfWidth, -halfHeight),
        new Vector2( halfWidth,  halfHeight),
        new Vector2(-halfWidth,  halfHeight)
    };

        var screenCorners = new Vector2[localCorners.Length];

        for (var i = 0; i < localCorners.Length; i++)
        {
            var corner = localCorners[i];

            var worldCorner = new Vector2(
                worldX + corner.X * cosine - corner.Y * sine,
                worldY + corner.X * sine + corner.Y * cosine);

            var localScreen =
                document.Viewport.Transform.WorldToScreen(worldCorner);

            screenCorners[i] = new Vector2(
                bounds.X + localScreen.X,
                bounds.Y + localScreen.Y);
        }

        DrawClosedOutline(
            context,
            screenCorners,
            color,
            document.Viewport.State.Zoom,
            Math.Max(sprite.Layer + 1, 2));
    }

    private static void DrawTransformedMarker(
        UiRenderContext context,
        Vector2 center,
        float zoom,
        float sizeWorld,
        float scaleX,
        float scaleY,
        float rotation,
        UiColor color,
        bool showDirection,
        int layer)
    {
        var halfWidth = sizeWorld * 0.5f;
        var halfHeight = sizeWorld * 0.32f;

        var corners = new[]
        {
            TransformLocalPoint(
                center, zoom, scaleX, scaleY, rotation,
                -halfWidth, -halfHeight),

            TransformLocalPoint(
                center, zoom, scaleX, scaleY, rotation, halfWidth, -halfHeight),

            TransformLocalPoint(
                center, zoom, scaleX, scaleY, rotation, halfWidth, halfHeight),

            TransformLocalPoint(
                center, zoom, scaleX, scaleY, rotation, -halfWidth, halfHeight)
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
                scaleX, scaleY, rotation,
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
        float scaleX,
        float scaleY,
        float rotation,
        float localX,
        float localY)
    {
        var scaledX = localX * scaleX;
        var scaledY = localY * scaleY;

        var cosine = MathF.Cos(rotation);
        var sine = MathF.Sin(rotation);

        var rotatedX =
            scaledX * cosine - scaledY * sine;

        var rotatedY =
            scaledX * sine + scaledY * cosine;

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

        if (!float.IsFinite(length) ||
            length <= 0.01f)
        {
            return;
        }

        var thickness = Math.Clamp(
            zoom * 1.25f,
            1.25f,
            4.0f);

        var center = new Vector2(
            (start.X + end.X) * 0.5f,
            (start.Y + end.Y) * 0.5f);

        var angleDegrees =
            MathF.Atan2(delta.Y, delta.X) *
            (180.0f / MathF.PI);

        context.PushRotation(
            center,
            angleDegrees);

        try
        {
            context.DrawRectangle(
                new UiRect(
                    center.X - length * 0.5f,
                    center.Y - thickness * 0.5f,
                    length,
                    thickness),
                color,
                filled: true,
                layer: layer);
        }
        finally
        {
            context.PopRotation();
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