
using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Authoring;

internal sealed class EditorUiCanvasGridRenderer
{
    private const float GridStep = EditorUiCanvasGrid.Step;
    private const float MajorGridStep = GridStep * 5.0f;
    private const float MinimumScreenSpacing = 8.0f;

    public void Render(
        UiRenderContext context,
        UiRect canvasRect,
        Vector2 canvasSize,
        float scale)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (scale <= 0.0f ||
            canvasSize.X <= 0.0f ||
            canvasSize.Y <= 0.0f)
        {
            return;
        }

        var showMinorGrid =
            GridStep * scale >= MinimumScreenSpacing;

        var showMajorGrid =
            MajorGridStep * scale >= MinimumScreenSpacing;

        var lineCountX = (int)(canvasSize.X / GridStep);
        var lineCountY = (int)(canvasSize.Y / GridStep);

        for (var i = 1; i <= lineCountX; i++)
        {
            var isMajor = i % 5 == 0;

            if (isMajor ? !showMajorGrid : !showMinorGrid)
            {
                continue;
            }

            var x = canvasRect.X + i * GridStep * scale;

            context.DrawRectangle(
                new UiRect(
                    x,
                    canvasRect.Y,
                    1.0f,
                    canvasRect.Height),
                GetGridColor(isMajor),
                filled: true);
        }

        for (var i = 1; i <= lineCountY; i++)
        {
            var isMajor = i % 5 == 0;

            if (isMajor ? !showMajorGrid : !showMinorGrid)
            {
                continue;
            }

            var y = canvasRect.Y + i * GridStep * scale;

            context.DrawRectangle(
                new UiRect(
                    canvasRect.X,
                    y,
                    canvasRect.Width,
                    1.0f),
                GetGridColor(isMajor),
                filled: true);
        }
    }

    private static UiColor GetGridColor(bool major)
    {
        return major
            ? new UiColor(58, 58, 58, 255)
            : new UiColor(48, 48, 48, 255);
    }
}
