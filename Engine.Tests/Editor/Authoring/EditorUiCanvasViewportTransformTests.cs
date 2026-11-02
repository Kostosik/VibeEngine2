using Engine.Core.Math;
using Engine.Editor.UI.Authoring;
using Engine.UI.Layout;

namespace Engine.Tests.Editor.Authoring;

public sealed class EditorUiCanvasViewportTransformTests
{
    [Fact]
    public void GetScale_FitsCanvasIntoViewport()
    {
        var transform = new EditorUiCanvasViewportTransform();

        var scale = transform.GetScale(
            new UiRect(0, 0, 500, 300),
            new Vector2(1000, 500));

        Assert.Equal(0.468f, scale, 3);
    }

    [Fact]
    public void GetCanvasRect_CentersCanvas()
    {
        var transform = new EditorUiCanvasViewportTransform();

        var rect = transform.GetCanvasRect(
            new UiRect(0, 0, 500, 300),
            new Vector2(1000, 500));

        Assert.Equal(16.0f, rect.X, 3);
        Assert.Equal(33.0f, rect.Y, 3);
        Assert.Equal(468.0f, rect.Width, 3);
        Assert.Equal(234.0f, rect.Height, 3);
    }

    [Fact]
    public void ZoomAt_KeepsDocumentPointUnderCursor()
    {
        var transform = new EditorUiCanvasViewportTransform();

        var viewport = new UiRect(0, 0, 500, 300);
        var canvasSize = new Vector2(1000, 500);
        var pointer = new Vector2(100, 100);

        var oldRect = transform.GetCanvasRect(
            viewport,
            canvasSize);

        var oldScale = transform.GetScale(
            viewport,
            canvasSize);

        var documentPoint = new Vector2(
            (pointer.X - oldRect.X) / oldScale,
            (pointer.Y - oldRect.Y) / oldScale);

        Assert.True(transform.ZoomAt(
            viewport,
            canvasSize,
            pointer,
            1.0f));

        var newRect = transform.GetCanvasRect(
            viewport,
            canvasSize);

        var newScale = transform.GetScale(
            viewport,
            canvasSize);

        var restoredPointer = new Vector2(
            newRect.X + documentPoint.X * newScale,
            newRect.Y + documentPoint.Y * newScale);

        Assert.True(
            MathF.Abs(restoredPointer.X - pointer.X) < 0.001f);

        Assert.True(
            MathF.Abs(restoredPointer.Y - pointer.Y) < 0.001f);
    }
}