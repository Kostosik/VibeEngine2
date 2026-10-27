using Engine.Core.Math;
using Engine.Editor.Documents;
using Engine.Graphics.Commands;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

internal static class ViewportGizmoRenderer
{
    public static void DrawRotation(
        UiRenderContext context,
        EditorDocument document,
        UiRect bounds)
    {
        if (!TryGetSelectedScreenPosition(
                document,
                bounds,
                out var center))
        {
            return;
        }

        const int segments = 32;
        const float radius = 32.0f;
        const float segmentSize = 5.0f;

        var color = new UiColor(235, 185, 75, 255);

        for (var i = 0; i < segments; i++)
        {
            var angle = MathF.Tau * i / segments;

            var x = center.X + MathF.Cos(angle) * radius;
            var y = center.Y + MathF.Sin(angle) * radius;

            context.DrawRectangle(
                new UiRect(
                    x - segmentSize * 0.5f,
                    y - segmentSize * 0.5f,
                    segmentSize,
                    segmentSize),
                color,
                filled: true,
                layer: 3);
        }
    }

    public static void DrawScale(
        UiRenderContext context,
        EditorDocument document,
        UiRect bounds)
    {
        if (!TryGetSelectedScreenPosition(
                document,
                bounds,
                out var center))
        {
            return;
        }

        const float arm = 48.0f;
        const float thickness = 3.0f;
        const float handleSize = 10.0f;

        var xColor = new UiColor(220, 80, 80, 255);
        var yColor = new UiColor(80, 220, 120, 255);

        context.DrawRectangle(
            new UiRect(
                center.X,
                center.Y - thickness * 0.5f,
                arm,
                thickness),
            xColor,
            filled: true,
            layer: 3);

        context.DrawRectangle(
            new UiRect(
                center.X - thickness * 0.5f,
                center.Y - arm,
                thickness,
                arm),
            yColor,
            filled: true,
            layer: 3);

        context.DrawRectangle(
            new UiRect(
                center.X + arm - handleSize * 0.5f,
                center.Y - handleSize * 0.5f,
                handleSize,
                handleSize),
            xColor,
            filled: true,
            layer: 3);

        context.DrawRectangle(
            new UiRect(
                center.X - handleSize * 0.5f,
                center.Y - arm - handleSize * 0.5f,
                handleSize,
                handleSize),
            yColor,
            filled: true,
            layer: 3);
    }

    public static void DrawTranslation(
        UiRenderContext context,
        EditorDocument document,
        UiRect bounds,
        float arm)
    {
        if (!TryGetSelectedScreenPosition(
                document,
                bounds,
                out var screen))
        {
            return;
        }

        const float thickness = 3.0f;

        var xColor = new UiColor(220, 80, 80, 255);
        var yColor = new UiColor(80, 220, 120, 255);

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

        var xTip = screen.X + arm;
        var yTip = screen.Y - arm;

        // X: arrow pointing right.
        context.DrawRectangle(
            new UiRect(xTip - 8.0f, screen.Y - 7.0f, 2.0f, 2.0f),
            xColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(xTip - 8.0f, screen.Y - 5.0f, 4.0f, 2.0f),
            xColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(xTip - 8.0f, screen.Y - 3.0f, 6.0f, 2.0f),
            xColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(xTip - 8.0f, screen.Y - 1.0f, 8.0f, 2.0f),
            xColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(xTip - 8.0f, screen.Y + 1.0f, 6.0f, 2.0f),
            xColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(xTip - 8.0f, screen.Y + 3.0f, 4.0f, 2.0f),
            xColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(xTip - 8.0f, screen.Y + 5.0f, 2.0f, 2.0f),
            xColor, filled: true, layer: 3);

        // Y: arrow pointing up.
        context.DrawRectangle(
            new UiRect(screen.X - 7.0f, yTip, 2.0f, 2.0f),
            yColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(screen.X - 5.0f, yTip, 2.0f, 4.0f),
            yColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(screen.X - 3.0f, yTip, 2.0f, 6.0f),
            yColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(screen.X - 1.0f, yTip, 2.0f, 8.0f),
            yColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(screen.X + 1.0f, yTip, 2.0f, 6.0f),
            yColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(screen.X + 3.0f, yTip, 2.0f, 4.0f),
            yColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(screen.X + 5.0f, yTip, 2.0f, 2.0f),
            yColor, filled: true, layer: 3);
    }

    private static bool TryGetSelectedScreenPosition(
        EditorDocument document,
        UiRect bounds,
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
            bounds.X + localScreen.X,
            bounds.Y + localScreen.Y);

        return true;
    }
}