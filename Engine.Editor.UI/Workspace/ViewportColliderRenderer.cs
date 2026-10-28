using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.Editor.Documents;
using Engine.Graphics.Commands;
using Engine.Physics;
using Engine.Physics.Components;
using Engine.Physics.Shapes;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

internal static class ViewportColliderRenderer
{
    private const int ColliderLayer = 2;
    private const int CircleSegments = 48;

    private const float LineThickness = 1.75f;

    private static readonly UiColor ColliderColor =
        new(65, 220, 220, 255);

    private static readonly UiColor TriggerColor =
        new(205, 125, 255, 255);

    public static void Draw(
        UiRenderContext context,
        EditorDocument document,
        UiRect viewportBounds)
    {
        var ecs = document.World.EcsWorld;
        var zoom = document.Viewport.State.Zoom;

        if (zoom <= 0.0f)
        {
            return;
        }

        foreach (var item in ecs.Query<PhysicsBody2D>())
        {
            var entity = item.Entity;

            if (!ecs.Has<Collider2D>(entity) ||
                !ecs.Has<WorldTransform2D>(entity))
            {
                continue;
            }

            var collider = ecs.Get<Collider2D>(entity);

            if (!collider.Enabled)
            {
                continue;
            }

            var transform =
                ecs.Get<WorldTransform2D>(entity);

            // Use exactly the same scaling rule as PhysicsSystem2D.
            if (!PhysicsColliderScale2D.TryApplyWorldScale(
                    ref collider,
                    transform.Scale))
            {
                continue;
            }

            var color = collider.IsTrigger
                ? TriggerColor
                : ColliderColor;

            switch (collider.Shape.Type)
            {
                case PhysicsShapeType.Aabb:
                    DrawAabb(
                        context,
                        document,
                        viewportBounds,
                        collider,
                        transform,
                        color);
                    break;

                case PhysicsShapeType.Circle:
                    DrawCircle(
                        context,
                        document,
                        viewportBounds,
                        collider,
                        transform,
                        zoom,
                        color);
                    break;

                case PhysicsShapeType.Polygon:
                    DrawPolygon(
                        context,
                        document,
                        viewportBounds,
                        collider,
                        transform,
                        color);
                    break;
            }
        }
    }

    private static void DrawAabb(
        UiRenderContext context,
        EditorDocument document,
        UiRect bounds,
        Collider2D collider,
        WorldTransform2D transform,
        UiColor color)
    {
        // AABB remains axis-aligned in the physics implementation.
        // GetWorldBounds also accounts for the rotated collider offset.
        var worldBounds =
            collider.GetWorldBounds(
                transform.Position,
                transform.Rotation);

        Vector2[] corners =
        [
            ToScreen(document, bounds, worldBounds.Min),

            ToScreen(
                document,
                bounds,
                new Vector2(
                    worldBounds.Max.X.ToFloat(),
                    worldBounds.Min.Y.ToFloat())),

            ToScreen(document, bounds, worldBounds.Max),

            ToScreen(
                document,
                bounds,
                new Vector2(
                    worldBounds.Min.X.ToFloat(),
                    worldBounds.Max.Y.ToFloat()))
        ];

        DrawClosedOutline(
            context,
            corners,
            color);
    }

    private static void DrawCircle(
        UiRenderContext context,
        EditorDocument document,
        UiRect bounds,
        Collider2D collider,
        WorldTransform2D transform,
        float zoom,
        UiColor color)
    {
        var worldCenter =
            collider.GetWorldPosition(
                transform.Position,
                transform.Rotation);

        var center =
            ToScreen(
                document,
                bounds,
                new Vector2(
                    worldCenter.X.ToFloat(),
                    worldCenter.Y.ToFloat()));

        var radius =
            collider.Shape.Circle.Radius.ToFloat() * zoom;

        if (!float.IsFinite(radius) ||
            radius <= 0.0f)
        {
            return;
        }

        var points =
            new Vector2[CircleSegments];

        for (var i = 0; i < CircleSegments; i++)
        {
            var angle =
                MathF.Tau * i / CircleSegments;

            points[i] =
                new Vector2(
                    center.X + MathF.Cos(angle) * radius,
                    center.Y + MathF.Sin(angle) * radius);
        }

        DrawClosedOutline(
            context,
            points,
            color);
    }

    private static void DrawPolygon(
        UiRenderContext context,
        EditorDocument document,
        UiRect bounds,
        Collider2D collider,
        WorldTransform2D transform,
        UiColor color)
    {
        var shape = collider.Shape.Polygon;

        var worldCenter =
            collider.GetWorldPosition(
                transform.Position,
                transform.Rotation);

        var center = new Vector2(
            worldCenter.X.ToFloat(),
            worldCenter.Y.ToFloat());

        var rotation =
            transform.Rotation.ToFloat();

        var cosine = MathF.Cos(rotation);
        var sine = MathF.Sin(rotation);

        var points =
            new Vector2[shape.VertexCount];

        for (var i = 0; i < shape.VertexCount; i++)
        {
            var vertex = shape.GetVertex(i);

            // Shape vertices have already been scaled by the
            // shared helper. Rotate and translate like physics does.
            var localX = vertex.X.ToFloat();
            var localY = vertex.Y.ToFloat();

            var worldVertex = new Vector2(
                center.X + localX * cosine - localY * sine,
                center.Y + localX * sine + localY * cosine);

            points[i] =
                ToScreen(
                    document,
                    bounds,
                    worldVertex);
        }

        DrawClosedOutline(
            context,
            points,
            color);
    }

    private static void DrawClosedOutline(
        UiRenderContext context,
        IReadOnlyList<Vector2> points,
        UiColor color)
    {
        if (points.Count < 2)
        {
            return;
        }

        for (var i = 0; i < points.Count; i++)
        {
            var next = (i + 1) % points.Count;

            DrawLine(
                context,
                points[i],
                points[next],
                color);
        }
    }

    private static void DrawLine(
        UiRenderContext context,
        Vector2 start,
        Vector2 end,
        UiColor color)
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
                    center.Y - LineThickness * 0.5f,
                    length,
                    LineThickness),
                color,
                filled: true,
                layer: ColliderLayer);
        }
        finally
        {
            context.PopRotation();
        }
    }

    private static Vector2 ToScreen(
        EditorDocument document,
        UiRect bounds,
        FixedVector2 worldPosition)
    {
        return ToScreen(
            document,
            bounds,
            new Vector2(
                worldPosition.X.ToFloat(),
                worldPosition.Y.ToFloat()));
    }

    private static Vector2 ToScreen(
        EditorDocument document,
        UiRect bounds,
        Vector2 worldPosition)
    {
        var localScreen =
            document.Viewport.Transform.WorldToScreen(
                worldPosition);

        return new Vector2(
            bounds.X + localScreen.X,
            bounds.Y + localScreen.Y);
    }
}