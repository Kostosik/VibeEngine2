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

        const int segments = 96;
        const float radius = 32.0f;
        const float thickness = 2.0f;

        var color = new UiColor(235, 185, 75, 255);

        // Smooth circular ring.
        for (var i = 0; i < segments; i++)
        {
            var angle0 = MathF.Tau * i / segments;
            var angle1 = MathF.Tau * (i + 1) / segments;

            var start = new Vector2(
                center.X + MathF.Cos(angle0) * radius,
                center.Y + MathF.Sin(angle0) * radius);

            var end = new Vector2(
                center.X + MathF.Cos(angle1) * radius,
                center.Y + MathF.Sin(angle1) * radius);

            DrawLine(
                context,
                start,
                end,
                thickness,
                color,
                layer: 3);
        }

        // Arrow placed on the upper-right part of the ring.
        // Positive angular direction corresponds to clockwise
        // rotation in screen coordinates.
        var arrowAngle = -MathF.PI / 4.0f;

        var radial = new Vector2(
            MathF.Cos(arrowAngle),
            MathF.Sin(arrowAngle));

        var tangent = new Vector2(
            -MathF.Sin(arrowAngle),
            MathF.Cos(arrowAngle));

        var tip = center + radial * radius;
        var baseCenter = tip - tangent * 8.0f;

        var wingA = baseCenter + radial * 4.5f;
        var wingB = baseCenter - radial * 4.5f;

        DrawLine(
            context,
            tip,
            wingA,
            2.5f,
            color,
            layer: 4);

        DrawLine(
            context,
            tip,
            wingB,
            2.5f,
            color,
            layer: 4);
    }

    private static void DrawLine(
    UiRenderContext context,
    Vector2 start,
    Vector2 end,
    float thickness,
    UiColor color,
    int layer)
    {
        var delta = end - start;

        var length = MathF.Sqrt(
            delta.X * delta.X +
            delta.Y * delta.Y);

        if (length <= 0.001f)
        {
            return;
        }

        var midpoint = new Vector2(
            (start.X + end.X) * 0.5f,
            (start.Y + end.Y) * 0.5f);

        var angleDegrees =
            MathF.Atan2(delta.Y, delta.X) *
            (180.0f / MathF.PI);

        context.PushRotation(
            midpoint,
            angleDegrees);

        try
        {
            context.DrawRectangle(
                new UiRect(
                    midpoint.X - length * 0.5f,
                    midpoint.Y - thickness * 0.5f,
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
            new UiRect(
                screen.X - thickness * 0.5f,
                yTip + 8.0f,
                thickness,
                arm - 8.0f),
            yColor,
            filled: true,
            layer: 3);

        // Arrow head: narrow at the top, wider at the bottom.
        context.DrawRectangle(
            new UiRect(
                screen.X - 1.0f,
                yTip,
                2.0f,
                2.0f),
            yColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(
                screen.X - 3.0f,
                yTip + 2.0f,
                6.0f,
                2.0f),
            yColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(
                screen.X - 5.0f,
                yTip + 4.0f,
                10.0f,
                2.0f),
            yColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(
                screen.X - 7.0f,
                yTip + 6.0f,
                14.0f,
                2.0f),
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