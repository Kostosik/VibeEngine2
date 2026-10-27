using Engine.Core.Math;
using Engine.Editor.Documents;
using Engine.Graphics.Commands;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

internal static class ViewportGridRenderer
{
    private const float GridMinScreenSpacing = 12.0f;
    private const float GridLineThickness = 1.0f;
    private const float AxisLineThickness = 2.0f;

    public static void Draw(
        UiRenderContext context,
        EditorDocument document,
        UiRect bounds)
    {
        var viewport = document.Viewport;
        var zoom = viewport.State.Zoom;

        if (zoom <= 0.0f)
        {
            return;
        }

        var spacing = CalculateGridSpacing(zoom);

        var topLeft = viewport.Transform.ScreenToWorld(
            Vector2.Zero);

        var bottomRight = viewport.Transform.ScreenToWorld(
            new Vector2(bounds.Width, bounds.Height));

        var minX = MathF.Min(topLeft.X, bottomRight.X);
        var maxX = MathF.Max(topLeft.X, bottomRight.X);
        var minY = MathF.Min(topLeft.Y, bottomRight.Y);
        var maxY = MathF.Max(topLeft.Y, bottomRight.Y);

        var startX = MathF.Floor(minX / spacing) * spacing;
        var startY = MathF.Floor(minY / spacing) * spacing;

        var gridColor = new UiColor(35, 35, 35, 255);
        var axisColor = new UiColor(70, 70, 70, 255);

        for (var worldX = startX;
             worldX <= maxX;
             worldX += spacing)
        {
            var localScreen = viewport.Transform.WorldToScreen(
                new Vector2(worldX, 0.0f));

            var screenX = bounds.X + localScreen.X;

            var isAxis = MathF.Abs(worldX) < 0.0001f;

            var thickness = isAxis
                ? AxisLineThickness
                : GridLineThickness;

            var color = isAxis
                ? axisColor
                : gridColor;

            context.DrawRectangle(
                new UiRect(
                    screenX - thickness * 0.5f,
                    bounds.Y,
                    thickness,
                    bounds.Height),
                color,
                filled: true,
                layer: 0);
        }

        for (var worldY = startY;
             worldY <= maxY;
             worldY += spacing)
        {
            var localScreen = viewport.Transform.WorldToScreen(
                new Vector2(0.0f, worldY));

            var screenY = bounds.Y + localScreen.Y;

            var isAxis = MathF.Abs(worldY) < 0.0001f;

            var thickness = isAxis
                ? AxisLineThickness
                : GridLineThickness;

            var color = isAxis
                ? axisColor
                : gridColor;

            context.DrawRectangle(
                new UiRect(
                    bounds.X,
                    screenY - thickness * 0.5f,
                    bounds.Width,
                    thickness),
                color,
                filled: true,
                layer: 0);
        }
    }

    private static float CalculateGridSpacing(float zoom)
    {
        var targetWorldSpacing = GridMinScreenSpacing / zoom;

        if (targetWorldSpacing <= 0.0f)
        {
            return 1.0f;
        }

        var exponent = MathF.Floor(
            MathF.Log10(targetWorldSpacing));

        var magnitude = MathF.Pow(10.0f, exponent);
        var normalized = targetWorldSpacing / magnitude;

        var step = normalized <= 1.0f
            ? 1.0f
            : normalized <= 2.0f
                ? 2.0f
                : normalized <= 5.0f
                    ? 5.0f
                    : 10.0f;

        return step * magnitude;
    }
}