using Engine.Core.Math;

namespace Engine.Editor.UI.Authoring;

public static class EditorUiCanvasHitTester
{
    private const float DefaultMinimumHitSize = 16.0f;
    public static EditorUiElement? HitTest(
        EditorUiDocument document,
        Vector2 documentPosition,
        Func<EditorUiElement, EditorUiLayout>? layoutResolver = null)
    {
        return HitTest(
            document,
            documentPosition,
            DefaultMinimumHitSize,
            layoutResolver);
    }

    internal static EditorUiElement? HitTest(
        EditorUiDocument document,
        Vector2 documentPosition,
        float minimumHitSize,
        Func<EditorUiElement, EditorUiLayout>? layoutResolver = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!float.IsFinite(minimumHitSize) ||
            minimumHitSize < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumHitSize));
        }

        var rootSize =
            layoutResolver?.Invoke(document.Root).Size
            ?? document.Root.Layout.Size;

        return FindElementAt(
            document.Root,
            null,
            documentPosition,
            Vector2.Zero,
            rootSize,
            minimumHitSize,
            layoutResolver);
    }

    private static EditorUiElement? FindElementAt(
        EditorUiElement element,
        EditorUiElement? parentElement,
        Vector2 point,
        Vector2 parentOrigin,
        Vector2 parentSize, float minimumHitSize,
        Func<EditorUiElement, EditorUiLayout>? layoutResolver)
    {
        if (!element.Visible)
        {
            return null;
        }

        var layout =
            layoutResolver?.Invoke(element)
            ?? element.Layout;

        var localRect =
            EditorUiCanvasElementGeometry.ResolveElementRect(
                element,
                layout,
                parentElement,
                parentSize);

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

        var canHitChildren =
            element.Type != EditorUiElementType.List ||
            (unrotatedPoint.X >= elementOrigin.X &&
             unrotatedPoint.Y >= elementOrigin.Y &&
             unrotatedPoint.X <= elementOrigin.X + localRect.Width &&
             unrotatedPoint.Y <= elementOrigin.Y + localRect.Height);

        if (canHitChildren)
        {
            foreach (var child in element.Children
                         .OrderByDescending(
                             static child => child.ZIndex))
            {
                var hit = FindElementAt(
                    child,
                    element,
                    unrotatedPoint,
                    childParentOrigin,
                    contentArea.Size, minimumHitSize,
                    layoutResolver);

                if (hit is not null)
                {
                    return hit;
                }
            }
        }

        if (ContainsElement(
                element,
                unrotatedPoint,
                elementOrigin,
                localRect,
                minimumHitSize))
        {
            return element;
        }

        return null;
    }

    private static bool ContainsElement(
    EditorUiElement element,
    Vector2 point,
    Vector2 origin,
    Rectangle rect,
    float minimumHitSize)
    {
        var hitWidth = rect.Width;
        var hitHeight = rect.Height;

        if (element.Type is
            EditorUiElementType.Spacer or
            EditorUiElementType.Separator)
        {
            hitWidth = MathF.Max(hitWidth, minimumHitSize);
            hitHeight = MathF.Max(hitHeight, minimumHitSize);
        }

        var hitX =
            origin.X + (rect.Width - hitWidth) * 0.5f;

        var hitY =
            origin.Y + (rect.Height - hitHeight) * 0.5f;

        return point.X >= hitX &&
               point.Y >= hitY &&
               point.X <= hitX + hitWidth &&
               point.Y <= hitY + hitHeight;
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