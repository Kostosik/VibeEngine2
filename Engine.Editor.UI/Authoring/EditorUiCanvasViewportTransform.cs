using Engine.Core.Math;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Authoring;

public sealed class EditorUiCanvasViewportTransform
{
    private const float CanvasPadding = 16.0f;
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 8.0f;
    private const float ZoomStep = 1.15f;

    public float Zoom { get; private set; } = 1.0f;

    public Vector2 Pan { get; private set; } =
        Vector2.Zero;

    public void SetPan(Vector2 pan)
    {
        Pan = pan;
    }

    public float GetScale(
        UiRect viewportBounds,
        Vector2 canvasSize)
    {
        if (canvasSize.X <= 0.0f ||
            canvasSize.Y <= 0.0f)
        {
            return 0.0f;
        }

        var availableWidth = MathF.Max(
            0.0f,
            viewportBounds.Width - CanvasPadding * 2.0f);

        var availableHeight = MathF.Max(
            0.0f,
            viewportBounds.Height - CanvasPadding * 2.0f);

        return Zoom * MathF.Min(
            availableWidth / canvasSize.X,
            availableHeight / canvasSize.Y);
    }

    public UiRect GetCanvasRect(
        UiRect viewportBounds,
        Vector2 canvasSize)
    {
        if (canvasSize.X <= 0.0f ||
            canvasSize.Y <= 0.0f)
        {
            return new UiRect(
                viewportBounds.X,
                viewportBounds.Y,
                0.0f,
                0.0f);
        }

        var scale = GetScale(
            viewportBounds,
            canvasSize);

        var width = canvasSize.X * scale;
        var height = canvasSize.Y * scale;

        return new UiRect(
            viewportBounds.X +
            (viewportBounds.Width - width) * 0.5f +
            Pan.X,

            viewportBounds.Y +
            (viewportBounds.Height - height) * 0.5f +
            Pan.Y,

            width,
            height);
    }

    public bool ZoomAt(
        UiRect viewportBounds,
        Vector2 canvasSize,
        Vector2 pointerPosition,
        float scrollDelta)
    {
        if (MathF.Abs(scrollDelta) < 0.0001f)
        {
            return false;
        }

        var oldScale = GetScale(
            viewportBounds,
            canvasSize);

        if (oldScale <= 0.0f)
        {
            return false;
        }

        var oldCanvas = GetCanvasRect(
            viewportBounds,
            canvasSize);

        var documentPoint = new Vector2(
            (pointerPosition.X - oldCanvas.X) / oldScale,
            (pointerPosition.Y - oldCanvas.Y) / oldScale);

        var newZoom = Math.Clamp(
            Zoom * MathF.Pow(
                ZoomStep,
                scrollDelta),
            MinZoom,
            MaxZoom);

        if (MathF.Abs(newZoom - Zoom) < 0.0001f)
        {
            return true;
        }

        Zoom = newZoom;

        var newScale = GetScale(
            viewportBounds,
            canvasSize);

        var centeredOrigin = new Vector2(
            viewportBounds.X +
            (viewportBounds.Width - canvasSize.X * newScale) * 0.5f,

            viewportBounds.Y +
            (viewportBounds.Height - canvasSize.Y * newScale) * 0.5f);

        Pan = new Vector2(
            pointerPosition.X -
            documentPoint.X * newScale -
            centeredOrigin.X,

            pointerPosition.Y -
            documentPoint.Y * newScale -
            centeredOrigin.Y);

        return true;
    }
}