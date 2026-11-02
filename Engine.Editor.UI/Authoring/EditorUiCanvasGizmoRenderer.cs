
using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Core;
using Engine.UI.Layout;
using ResizeHandle =
    Engine.Editor.UI.Authoring.EditorUiCanvasTransformTool.ResizeHandle;

namespace Engine.Editor.UI.Authoring;

internal sealed class EditorUiCanvasGizmoRenderer
{
    private const float HandleSize = 8.0f;
    private const float HandleHitRadius = 7.0f;
    private const float RotateHandleOffset = 22.0f;

    public UiRect CreateGizmoRect(
        UiRect sourceRect,
        Vector2 center)
    {
        return new UiRect(
            center.X - sourceRect.Width * 0.5f,
            center.Y - sourceRect.Height * 0.5f,
            sourceRect.Width,
            sourceRect.Height);
    }

    public bool HitTestRotateHandle(
        Vector2 pointer,
        UiRect rect)
    {
        var centerX = rect.X + rect.Width * 0.5f;

        var handleCenter = new Vector2(
            centerX,
            rect.Y - RotateHandleOffset);

        return MathF.Abs(pointer.X - handleCenter.X) <= HandleHitRadius &&
               MathF.Abs(pointer.Y - handleCenter.Y) <= HandleHitRadius;
    }

    public ResizeHandle HitTestResizeHandle(
        Vector2 pointer,
        UiRect rect)
    {
        foreach (var (handle, center) in GetResizeHandles(rect))
        {
            if (MathF.Abs(pointer.X - center.X) <= HandleHitRadius &&
                MathF.Abs(pointer.Y - center.Y) <= HandleHitRadius)
            {
                return handle;
            }
        }

        return ResizeHandle.None;
    }

    public void Render(
        UiRenderContext context,
        UiRect elementRect,
        Vector2 center,
        float rotationDegrees)
    {
        var rect = CreateGizmoRect(elementRect, center);

        context.PushRotation(center, rotationDegrees);

        try
        {
            context.DrawRectangle(
                rect,
                new UiColor(45, 155, 255, 255),
                filled: false,
                layer: 10);

            foreach (var (_, handleCenter) in GetResizeHandles(rect))
            {
                DrawHandle(context, handleCenter, 11);
            }

            var centerX = rect.X + rect.Width * 0.5f;

            var rotateHandleCenter = new Vector2(
                centerX,
                rect.Y - RotateHandleOffset);

            var stemTop =
                rotateHandleCenter.Y + HandleSize * 0.5f;

            context.DrawRectangle(
                new UiRect(
                    centerX - 1.0f,
                    stemTop,
                    2.0f,
                    MathF.Max(0.0f, rect.Y - stemTop)),
                new UiColor(45, 155, 255, 255),
                filled: true,
                layer: 10);

            DrawHandle(context, rotateHandleCenter, 11);
        }
        finally
        {
            context.PopRotation();
        }
    }

    private static void DrawHandle(
        UiRenderContext context,
        Vector2 center,
        int layer)
    {
        var halfSize = HandleSize * 0.5f;

        var handleRect = new UiRect(
            center.X - halfSize,
            center.Y - halfSize,
            HandleSize,
            HandleSize);

        context.DrawRectangle(
            handleRect,
            new UiColor(45, 155, 255, 255),
            filled: true,
            layer: layer);

        context.DrawRectangle(
            handleRect,
            new UiColor(245, 245, 245, 255),
            filled: false,
            layer: layer + 1);
    }

    private static (
        ResizeHandle Handle,
        Vector2 Center)[] GetResizeHandles(UiRect rect)
    {
        var left = rect.X;
        var top = rect.Y;
        var right = rect.X + rect.Width;
        var bottom = rect.Y + rect.Height;

        var centerX = (left + right) * 0.5f;
        var centerY = (top + bottom) * 0.5f;

        return new[]
        {
            (ResizeHandle.TopLeft, new Vector2(left, top)),
            (ResizeHandle.TopRight, new Vector2(right, top)),
            (ResizeHandle.BottomLeft, new Vector2(left, bottom)),
            (ResizeHandle.BottomRight, new Vector2(right, bottom)),
            (ResizeHandle.Left, new Vector2(left, centerY)),
            (ResizeHandle.Right, new Vector2(right, centerY)),
            (ResizeHandle.Top, new Vector2(centerX, top)),
            (ResizeHandle.Bottom, new Vector2(centerX, bottom))
        };
    }
}
