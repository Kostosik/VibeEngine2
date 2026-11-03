
using System;
using Engine.Core.Math;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Authoring;

internal readonly record struct EditorUiCanvasElementScreenTransform(
    UiRect LayoutRect,
    Vector2 Center,
    float RotationDegrees,
    float ParentRotationDegrees,
    Vector2 DocumentOrigin);

internal static class EditorUiCanvasElementGeometry
{
    public static bool TryGetScreenTransform(
        EditorUiDocument document,
        EditorUiElement element,
        UiRect canvasRect,
        float scale,
        Func<EditorUiElement, EditorUiLayout> layoutResolver,
        out EditorUiCanvasElementScreenTransform transform)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(layoutResolver);

        if (scale <= 0.0f)
        {
            transform = default;
            return false;
        }

        var path = new List<EditorUiElement>();

        for (EditorUiElement? current = element;
             current is not null;
             current = current.Parent)
        {
            path.Add(current);
        }

        path.Reverse();

        if (path.Count == 0)
        {
            transform = default;
            return false;
        }

        var rotationCenters = new List<Vector2>(path.Count);
        var rotations = new List<float>(path.Count);

        var parentSize = layoutResolver(document.Root).Size;
        var parentOrigin = Vector2.Zero;

        var elementOrigin = Vector2.Zero;
        var elementSize = Vector2.Zero;

        for (var i = 0; i < path.Count; i++)
        {
            var current = path[i];

            if (!current.Visible)
            {
                transform = default;
                return false;
            }

            var layout = layoutResolver(current);

            var parentElement = i > 0
                ? path[i - 1]
                : null;

            var localRect = ResolveElementRect(
                current,
                layout,
                parentElement,
                parentSize);

            var origin = new Vector2(
                parentOrigin.X + localRect.X,
                parentOrigin.Y + localRect.Y);

            var center = new Vector2(
                origin.X + localRect.Width * 0.5f,
                origin.Y + localRect.Height * 0.5f);

            rotationCenters.Add(center);
            rotations.Add(layout.RotationDegrees);

            if (i == path.Count - 1)
            {
                elementOrigin = origin;
                elementSize = new Vector2(
                    localRect.Width,
                    localRect.Height);
            }

            var contentArea = GetChildContentArea(
                current,
                new Vector2(
                    localRect.Width,
                    localRect.Height));

            parentOrigin = origin + contentArea.Origin;
            parentSize = contentArea.Size;
        }

        var documentCenter = rotationCenters[^1];
        var parentRotation = 0.0f;

        // Применяем вращение родителей от ближайшего
        // родителя к корню, как это делает UiRenderContext.
        for (var i = path.Count - 2; i >= 0; i--)
        {
            documentCenter = RotatePoint(
                documentCenter,
                rotationCenters[i],
                rotations[i]);

            elementOrigin = RotatePoint(
                elementOrigin,
                rotationCenters[i],
                rotations[i]);

            parentRotation += rotations[i];
        }

        var totalRotation =
            parentRotation + rotations[^1];

        var screenCenter = new Vector2(
            canvasRect.X + documentCenter.X * scale,
            canvasRect.Y + documentCenter.Y * scale);

        var screenWidth = elementSize.X * scale;
        var screenHeight = elementSize.Y * scale;

        var screenRect = new UiRect(
            screenCenter.X - screenWidth * 0.5f,
            screenCenter.Y - screenHeight * 0.5f,
            screenWidth,
            screenHeight);

        transform = new EditorUiCanvasElementScreenTransform(
            screenRect,
            screenCenter,
            totalRotation,
            parentRotation,
            elementOrigin);

        return true;
    }

    private const float ListItemSpacing = 4.0f;

    internal static Rectangle ResolveElementRect(
        EditorUiElement element,
        EditorUiLayout layout,
        EditorUiElement? parent,
        Vector2 parentSize)
    {
        if (parent?.Type != EditorUiElementType.List)
        {
            return layout.Resolve(parentSize);
        }

        var rowOffset = 0.0f;

        foreach (var sibling in parent.Children)
        {
            if (ReferenceEquals(sibling, element))
            {
                break;
            }

            rowOffset += GetListItemHeight(sibling) +
                         ListItemSpacing;
        }

        var x =
            parentSize.X * layout.Anchor.X +
            layout.Offset.X -
            layout.Size.X * layout.Anchor.X;

        var y = rowOffset + layout.Offset.Y;

        return new Rectangle(
            x,
            y,
            layout.Size.X,
            GetListItemHeight(element, layout));
    }

    private static float GetListItemHeight(
        EditorUiElement element)
    {
        return GetListItemHeight(
            element,
            element.Layout);
    }

    private static float GetListItemHeight(
        EditorUiElement element,
        EditorUiLayout layout)
    {
        if (element.Type != EditorUiElementType.Expander)
        {
            return layout.Size.Y;
        }

        const float headerHeight = 32.0f;
        const float contentSpacing = 4.0f;
        const float contentPadding = 16.0f;

        if (!element.ExpanderIsExpanded ||
            element.Children.Count == 0)
        {
            return headerHeight;
        }

        return headerHeight +
               contentSpacing +
               contentPadding +
               GetListItemHeight(element.Children[0]);
    }
    internal static (Vector2 Origin, Vector2 Size) GetChildContentArea(
    EditorUiElement element,
    Vector2 elementSize)
    {
        if (element.Type != EditorUiElementType.Expander)
        {
            return (Vector2.Zero, elementSize);
        }

        const float horizontalPadding = 8.0f;
        const float headerHeight = 32.0f;
        const float contentSpacing = 4.0f;
        const float contentPadding = 8.0f;

        return (
            new Vector2(
                horizontalPadding,
                headerHeight + contentSpacing + contentPadding),

            new Vector2(
                MathF.Max(0.0f, elementSize.X - horizontalPadding * 2.0f),
                MathF.Max(
                    0.0f,
                    elementSize.Y -
                    headerHeight -
                    contentSpacing -
                    contentPadding * 2.0f)));
    }
    public static Vector2 RotatePoint(
    Vector2 point,
    Vector2 origin,
    float rotationDegrees)
    {
        if (MathF.Abs(rotationDegrees) < 0.0001f)
            return point;

        var radians = rotationDegrees * (MathF.PI / 180.0f);
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);

        var dx = point.X - origin.X;
        var dy = point.Y - origin.Y;

        return new Vector2(
            origin.X + dx * cos - dy * sin,
            origin.Y + dx * sin + dy * cos);
    }
}
