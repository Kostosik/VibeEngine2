using Engine.Core.Math;

namespace Engine.Editor.UI.Authoring;

public static class EditorUiCanvasHitTester
{
    public static EditorUiElement? HitTest(
        EditorUiDocument document,
        Vector2 documentPosition,
        Func<EditorUiElement, EditorUiLayout>? layoutResolver = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        var rootSize =
    layoutResolver?.Invoke(document.Root).Size
    ?? document.Root.Layout.Size;

        return FindElementAt(
            document.Root,
            documentPosition,
            Vector2.Zero,
            rootSize,
            layoutResolver);
    }

    private static EditorUiElement? FindElementAt(
        EditorUiElement element,
        Vector2 point,
        Vector2 parentOrigin,
        Vector2 parentSize,
        Func<EditorUiElement, EditorUiLayout>? layoutResolver)
    {
        if (!element.Visible)
        {
            return null;
        }

        var layout =
            layoutResolver?.Invoke(element)
            ?? element.Layout;

        var localRect = layout.Resolve(parentSize);

        var elementOrigin = new Vector2(
            parentOrigin.X + localRect.X,
            parentOrigin.Y + localRect.Y);

        var elementCenter = new Vector2(
            elementOrigin.X + localRect.Width * 0.5f,
            elementOrigin.Y + localRect.Height * 0.5f);

        var unrotatedPoint = RotatePoint(
            point,
            elementCenter,
            -layout.RotationDegrees);

        var contentArea =
    EditorUiCanvasElementGeometry.GetChildContentArea(
        element,
        new Vector2(
            localRect.Width,
            localRect.Height));

        var childParentOrigin =
            elementOrigin + contentArea.Origin;

        foreach (var child in element.Children
                     .OrderByDescending(
                         static child => child.ZIndex))
        {
            var hit = FindElementAt(
                child,
                unrotatedPoint,
                childParentOrigin,
                contentArea.Size,
                layoutResolver);

            if (hit is not null)
            {
                return hit;
            }
        }

        if (unrotatedPoint.X >= elementOrigin.X &&
            unrotatedPoint.Y >= elementOrigin.Y &&
            unrotatedPoint.X <= elementOrigin.X + localRect.Width &&
            unrotatedPoint.Y <= elementOrigin.Y + localRect.Height)
        {
            return element;
        }

        return null;
    }

    internal static Vector2 RotatePoint(
        Vector2 point,
        Vector2 origin,
        float rotationDegrees)
    {
        if (MathF.Abs(rotationDegrees) < 0.0001f)
        {
            return point;
        }

        var radians =
            rotationDegrees * (MathF.PI / 180.0f);

        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);

        var dx = point.X - origin.X;
        var dy = point.Y - origin.Y;

        return new Vector2(
            origin.X + dx * cos - dy * sin,
            origin.Y + dx * sin + dy * cos);
    }
}