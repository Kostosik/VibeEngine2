using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Authoring;

internal sealed class EditorUiCanvasSmartGuideRenderer
{
    private const float AlignmentTolerance = 5.0f;

    private static readonly UiColor GuideColor =
        new(70, 210, 230, 230);
    private const float SnapTolerance = 8.0f;
    public void Render(
        UiRenderContext context,
        UiRect canvasRect,
        EditorUiCanvasElementScreenTransform transform,
        IReadOnlyList<EditorUiCanvasElementScreenTransform> references)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(references);

        var movingBounds = GetBounds(transform);

        // Выравнивание относительно холста.
        DrawVerticalIfAligned(
            context, canvasRect, canvasRect.X,
            movingBounds.X,
            movingBounds.X + movingBounds.Width * 0.5f,
            movingBounds.X + movingBounds.Width);

        DrawVerticalIfAligned(
            context, canvasRect,
            canvasRect.X + canvasRect.Width * 0.5f,
            movingBounds.X,
            movingBounds.X + movingBounds.Width * 0.5f,
            movingBounds.X + movingBounds.Width);

        DrawVerticalIfAligned(
            context, canvasRect,
            canvasRect.X + canvasRect.Width,
            movingBounds.X,
            movingBounds.X + movingBounds.Width * 0.5f,
            movingBounds.X + movingBounds.Width);

        DrawHorizontalIfAligned(
            context, canvasRect, canvasRect.Y,
            movingBounds.Y,
            movingBounds.Y + movingBounds.Height * 0.5f,
            movingBounds.Y + movingBounds.Height);

        DrawHorizontalIfAligned(
            context, canvasRect,
            canvasRect.Y + canvasRect.Height * 0.5f,
            movingBounds.Y,
            movingBounds.Y + movingBounds.Height * 0.5f,
            movingBounds.Y + movingBounds.Height);

        DrawHorizontalIfAligned(
            context, canvasRect,
            canvasRect.Y + canvasRect.Height,
            movingBounds.Y,
            movingBounds.Y + movingBounds.Height * 0.5f,
            movingBounds.Y + movingBounds.Height);

        // Выравнивание относительно остальных элементов.
        var movingX = new[]
        {
            movingBounds.X,
            movingBounds.X + movingBounds.Width * 0.5f,
            movingBounds.X + movingBounds.Width
        };

        var movingY = new[]
        {
            movingBounds.Y,
            movingBounds.Y + movingBounds.Height * 0.5f,
            movingBounds.Y + movingBounds.Height
        };

        var verticalGuides = new List<float>();
        var horizontalGuides = new List<float>();

        foreach (var reference in references)
        {
            var referenceBounds = GetBounds(reference);

            var referenceX = new[]
            {
                referenceBounds.X,
                referenceBounds.X + referenceBounds.Width * 0.5f,
                referenceBounds.X + referenceBounds.Width
            };

            var referenceY = new[]
            {
                referenceBounds.Y,
                referenceBounds.Y + referenceBounds.Height * 0.5f,
                referenceBounds.Y + referenceBounds.Height
            };

            CollectAlignedCoordinates(
                movingX,
                referenceX,
                verticalGuides);

            CollectAlignedCoordinates(
                movingY,
                referenceY,
                horizontalGuides);
        }

        foreach (var x in verticalGuides)
        {
            context.DrawRectangle(
                new UiRect(
                    x,
                    canvasRect.Y,
                    1.0f,
                    canvasRect.Height),
                GuideColor,
                filled: true,
                layer: 20);
        }

        foreach (var y in horizontalGuides)
        {
            context.DrawRectangle(
                new UiRect(
                    canvasRect.X,
                    y,
                    canvasRect.Width,
                    1.0f),
                GuideColor,
                filled: true,
                layer: 20);
        }
    }

    private static UiRect GetBounds(
        EditorUiCanvasElementScreenTransform transform)
    {
        var radians =
            transform.RotationDegrees * (MathF.PI / 180.0f);

        var cos = MathF.Abs(MathF.Cos(radians));
        var sin = MathF.Abs(MathF.Sin(radians));

        var halfWidth = transform.LayoutRect.Width * 0.5f;
        var halfHeight = transform.LayoutRect.Height * 0.5f;

        var extentX = cos * halfWidth + sin * halfHeight;
        var extentY = sin * halfWidth + cos * halfHeight;

        return new UiRect(
            transform.Center.X - extentX,
            transform.Center.Y - extentY,
            extentX * 2.0f,
            extentY * 2.0f);
    }

    public Vector2 GetSnapCorrection(
    UiRect canvasRect,
    EditorUiCanvasElementScreenTransform transform,
    IReadOnlyList<EditorUiCanvasElementScreenTransform> references)
    {
        var bounds = GetBounds(transform);

        var movingX = new[]
        {
        bounds.X,
        bounds.X + bounds.Width * 0.5f,
        bounds.X + bounds.Width
    };

        var movingY = new[]
        {
        bounds.Y,
        bounds.Y + bounds.Height * 0.5f,
        bounds.Y + bounds.Height
    };

        var targetX = new List<float>
    {
        canvasRect.X,
        canvasRect.X + canvasRect.Width * 0.5f,
        canvasRect.X + canvasRect.Width
    };

        var targetY = new List<float>
    {
        canvasRect.Y,
        canvasRect.Y + canvasRect.Height * 0.5f,
        canvasRect.Y + canvasRect.Height
    };

        foreach (var reference in references)
        {
            var referenceBounds = GetBounds(reference);

            targetX.Add(referenceBounds.X);
            targetX.Add(referenceBounds.X + referenceBounds.Width * 0.5f);
            targetX.Add(referenceBounds.X + referenceBounds.Width);

            targetY.Add(referenceBounds.Y);
            targetY.Add(referenceBounds.Y + referenceBounds.Height * 0.5f);
            targetY.Add(referenceBounds.Y + referenceBounds.Height);
        }

        return new Vector2(
            FindBestCorrection(movingX, targetX),
            FindBestCorrection(movingY, targetY));
    }

    private static float FindBestCorrection(
        float[] moving,
        List<float> targets)
    {
        var bestCorrection = 0.0f;
        var bestDistance = SnapTolerance;
        var found = false;

        foreach (var target in targets)
        {
            foreach (var coordinate in moving)
            {
                var correction = target - coordinate;
                var distance = MathF.Abs(correction);

                if (distance > bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestCorrection = correction;
                found = true;
            }
        }

        return found ? bestCorrection : 0.0f;
    }
    private static void CollectAlignedCoordinates(
        float[] moving,
        float[] reference,
        List<float> guides)
    {
        foreach (var referenceCoordinate in reference)
        {
            foreach (var movingCoordinate in moving)
            {
                if (MathF.Abs(
                        movingCoordinate - referenceCoordinate) >
                    AlignmentTolerance)
                {
                    continue;
                }

                if (guides.Exists(
                        value => MathF.Abs(
                            value - referenceCoordinate) < 1.0f))
                {
                    continue;
                }

                guides.Add(referenceCoordinate);
                break;
            }
        }
    }

    private static void DrawVerticalIfAligned(
        UiRenderContext context,
        UiRect canvas,
        float target,
        float left,
        float center,
        float right)
    {
        if (!IsAligned(target, left, center, right))
        {
            return;
        }

        context.DrawRectangle(
            new UiRect(target, canvas.Y, 1.0f, canvas.Height),
            GuideColor,
            filled: true,
            layer: 20);
    }

    private static void DrawHorizontalIfAligned(
        UiRenderContext context,
        UiRect canvas,
        float target,
        float top,
        float center,
        float bottom)
    {
        if (!IsAligned(target, top, center, bottom))
        {
            return;
        }

        context.DrawRectangle(
            new UiRect(canvas.X, target, canvas.Width, 1.0f),
            GuideColor,
            filled: true,
            layer: 20);
    }

    private static bool IsAligned(
        float target,
        float first,
        float second,
        float third)
    {
        return MathF.Abs(target - first) <= AlignmentTolerance ||
               MathF.Abs(target - second) <= AlignmentTolerance ||
               MathF.Abs(target - third) <= AlignmentTolerance;
    }
}