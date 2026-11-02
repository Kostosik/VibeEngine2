
using System;
using Engine.Core.Math;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Authoring;

internal readonly record struct EditorUiCanvasElementScreenTransform(
    UiRect LayoutRect,
    Vector2 Center,
    float RotationDegrees,
    float ParentRotationDegrees);

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

        var parentSize = document.Root.Layout.Size;
        var parentOrigin = Vector2.Zero;

        var elementRect = default(UiRect);
        var center = Vector2.Zero;
        var totalRotation = 0.0f;
        var parentRotation = 0.0f;

        for (var i = 0; i < path.Count; i++)
        {
            var current = path[i];

            if (!current.Visible)
            {
                transform = default;
                return false;
            }

            var layout = layoutResolver(current);
            var localRect = layout.Resolve(parentSize);

            var origin = new Vector2(
                parentOrigin.X + localRect.X,
                parentOrigin.Y + localRect.Y);

            elementRect = new UiRect(
                canvasRect.X + origin.X * scale,
                canvasRect.Y + origin.Y * scale,
                localRect.Width * scale,
                localRect.Height * scale);

            center = new Vector2(
                elementRect.X + elementRect.Width * 0.5f,
                elementRect.Y + elementRect.Height * 0.5f);

            totalRotation += layout.RotationDegrees;

            if (i < path.Count - 1)
            {
                parentRotation += layout.RotationDegrees;
            }

            parentOrigin = origin;
            parentSize = new Vector2(
                localRect.Width,
                localRect.Height);
        }

        transform = new EditorUiCanvasElementScreenTransform(
            elementRect,
            center,
            totalRotation,
            parentRotation);

        return true;
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
