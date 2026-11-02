
using Engine.Core.Math;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Authoring;

public enum EditorUiAlignment
{
    Left,
    Right,
    Top,
    Bottom,
    HorizontalCenter,
    VerticalCenter
}

public enum EditorUiDistributionAxis
{
    Horizontal,
    Vertical
}

public static class EditorUiLayoutAlignment
{
    private readonly record struct Item(
        EditorUiElement Element,
        Vector2 Center,
        float Left,
        float Right,
        float Top,
        float Bottom,
        float ParentRotationDegrees);

    public static bool Align(
        EditorUiDocument document,
        EditorUiAlignment alignment)
    {
        ArgumentNullException.ThrowIfNull(document);

        var canvasSize = document.Root.Layout.Size;

        var canvasRect = new UiRect(
            0.0f,
            0.0f,
            canvasSize.X,
            canvasSize.Y);

        var items = new List<Item>();

        foreach (var id in document.Selection.Items)
        {
            var element = document.GetElement(id);

            if (ReferenceEquals(element, document.Root))
            {
                continue;
            }

            if (!EditorUiCanvasElementGeometry.TryGetScreenTransform(
                    document,
                    element,
                    canvasRect,
                    1.0f,
                    static current => current.Layout,
                    out var transform))
            {
                continue;
            }

            var radians =
                transform.RotationDegrees *
                (MathF.PI / 180.0f);

            var cos = MathF.Abs(MathF.Cos(radians));
            var sin = MathF.Abs(MathF.Sin(radians));

            var width =
                element.Layout.Size.X * cos +
                element.Layout.Size.Y * sin;

            var height =
                element.Layout.Size.X * sin +
                element.Layout.Size.Y * cos;

            items.Add(new Item(
                element,
                transform.Center,
                transform.Center.X - width * 0.5f,
                transform.Center.X + width * 0.5f,
                transform.Center.Y - height * 0.5f,
                transform.Center.Y + height * 0.5f,
                transform.ParentRotationDegrees));
        }

        if (items.Count < 2)
        {
            return false;
        }

        var targetLeft = items.Min(item => item.Left);
        var targetRight = items.Max(item => item.Right);
        var targetTop = items.Min(item => item.Top);
        var targetBottom = items.Max(item => item.Bottom);

        var targetCenterX =
            (targetLeft + targetRight) * 0.5f;

        var targetCenterY =
            (targetTop + targetBottom) * 0.5f;

        var documentDeltas = new Dictionary<Guid, Vector2>();

        foreach (var item in items)
        {
            var delta = alignment switch
            {
                EditorUiAlignment.Left =>
                    new Vector2(
                        targetLeft - item.Left,
                        0.0f),

                EditorUiAlignment.Right =>
                    new Vector2(
                        targetRight - item.Right,
                        0.0f),

                EditorUiAlignment.Top =>
                    new Vector2(
                        0.0f,
                        targetTop - item.Top),

                EditorUiAlignment.Bottom =>
                    new Vector2(
                        0.0f,
                        targetBottom - item.Bottom),

                EditorUiAlignment.HorizontalCenter =>
                    new Vector2(
                        targetCenterX - item.Center.X,
                        0.0f),

                EditorUiAlignment.VerticalCenter =>
                    new Vector2(
                        0.0f,
                        targetCenterY - item.Center.Y),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(alignment))
            };

            documentDeltas.Add(
                item.Element.Id,
                delta);
        }

        var newLayouts =
            new Dictionary<Guid, EditorUiLayout>();

        foreach (var item in items)
        {
            var ancestorDelta = Vector2.Zero;

            // Перемещение выбранного родителя также
            // перемещает его дочерние элементы.
            for (var parent = item.Element.Parent;
                 parent is not null;
                 parent = parent.Parent)
            {
                if (documentDeltas.TryGetValue(
                        parent.Id,
                        out var delta))
                {
                    ancestorDelta = new Vector2(
                        ancestorDelta.X + delta.X,
                        ancestorDelta.Y + delta.Y);
                }
            }

            var ownDelta = new Vector2(
                documentDeltas[item.Element.Id].X -
                ancestorDelta.X,
                documentDeltas[item.Element.Id].Y -
                ancestorDelta.Y);

            var localDelta =
                EditorUiCanvasElementGeometry.RotatePoint(
                    ownDelta,
                    Vector2.Zero,
                    -item.ParentRotationDegrees);

            var oldLayout = item.Element.Layout;

            var newLayout = oldLayout with
            {
                Offset = new Vector2(
                    oldLayout.Offset.X + localDelta.X,
                    oldLayout.Offset.Y + localDelta.Y)
            };

            newLayouts.Add(
                item.Element.Id,
                newLayout);
        }

        if (newLayouts.All(pair =>
                document.GetElement(pair.Key).Layout ==
                pair.Value))
        {
            return false;
        }

        document.Execute(
            new SetEditorUiLayoutsCommand(
                document,
                newLayouts));

        return true;
    }


    public static bool Distribute(
        EditorUiDocument document,
        EditorUiDistributionAxis axis)
    {
        ArgumentNullException.ThrowIfNull(document);

        var horizontal =
            axis == EditorUiDistributionAxis.Horizontal;

        if (!horizontal &&
            axis != EditorUiDistributionAxis.Vertical)
        {
            throw new ArgumentOutOfRangeException(nameof(axis));
        }

        var canvasSize = document.Root.Layout.Size;

        var canvasRect = new UiRect(
            0.0f,
            0.0f,
            canvasSize.X,
            canvasSize.Y);

        var items = new List<Item>();

        foreach (var id in document.Selection.Items)
        {
            var element = document.GetElement(id);

            if (ReferenceEquals(element, document.Root))
            {
                continue;
            }

            if (!EditorUiCanvasElementGeometry.TryGetScreenTransform(
                    document,
                    element,
                    canvasRect,
                    1.0f,
                    static current => current.Layout,
                    out var transform))
            {
                continue;
            }

            var radians =
                transform.RotationDegrees *
                (MathF.PI / 180.0f);

            var cos = MathF.Abs(MathF.Cos(radians));
            var sin = MathF.Abs(MathF.Sin(radians));

            var width =
                element.Layout.Size.X * cos +
                element.Layout.Size.Y * sin;

            var height =
                element.Layout.Size.X * sin +
                element.Layout.Size.Y * cos;

            items.Add(new Item(
                element,
                transform.Center,
                transform.Center.X - width * 0.5f,
                transform.Center.X + width * 0.5f,
                transform.Center.Y - height * 0.5f,
                transform.Center.Y + height * 0.5f,
                transform.ParentRotationDegrees));
        }

        if (items.Count < 3)
        {
            return false;
        }

        var ordered = horizontal
            ? items
                .OrderBy(item => item.Left)
                .ThenBy(item => item.Element.Id)
                .ToArray()
            : items
                .OrderBy(item => item.Top)
                .ThenBy(item => item.Element.Id)
                .ToArray();

        var first = ordered[0];
        var last = ordered[^1];

        var firstLeading = horizontal
            ? first.Left
            : first.Top;

        var lastTrailing = horizontal
            ? last.Right
            : last.Bottom;

        var totalExtent = lastTrailing - firstLeading;

        var totalSize = ordered.Sum(item =>
            horizontal
                ? item.Right - item.Left
                : item.Bottom - item.Top);

        var gap =
            (totalExtent - totalSize) /
            (ordered.Length - 1);

        var documentDeltas =
            new Dictionary<Guid, Vector2>();

        var targetLeading = firstLeading;

        for (var i = 1; i < ordered.Length - 1; i++)
        {
            var previous = ordered[i - 1];
            var current = ordered[i];

            var previousSize = horizontal
                ? previous.Right - previous.Left
                : previous.Bottom - previous.Top;

            targetLeading += previousSize + gap;

            var currentLeading = horizontal
                ? current.Left
                : current.Top;

            var delta = targetLeading - currentLeading;

            documentDeltas.Add(
                current.Element.Id,
                horizontal
                    ? new Vector2(delta, 0.0f)
                    : new Vector2(0.0f, delta));
        }

        var newLayouts =
            new Dictionary<Guid, EditorUiLayout>();

        foreach (var item in items)
        {
            var desiredDelta =
                documentDeltas.TryGetValue(
                    item.Element.Id,
                    out var value)
                    ? value
                    : Vector2.Zero;

            var ancestorDelta = Vector2.Zero;

            for (var parent = item.Element.Parent;
                 parent is not null;
                 parent = parent.Parent)
            {
                if (documentDeltas.TryGetValue(
                        parent.Id,
                        out var parentDelta))
                {
                    ancestorDelta = new Vector2(
                        ancestorDelta.X + parentDelta.X,
                        ancestorDelta.Y + parentDelta.Y);
                }
            }

            var ownDelta = new Vector2(
                desiredDelta.X - ancestorDelta.X,
                desiredDelta.Y - ancestorDelta.Y);

            var localDelta =
                EditorUiCanvasElementGeometry.RotatePoint(
                    ownDelta,
                    Vector2.Zero,
                    -item.ParentRotationDegrees);

            var oldLayout = item.Element.Layout;

            newLayouts.Add(
                item.Element.Id,
                oldLayout with
                {
                    Offset = new Vector2(
                        oldLayout.Offset.X + localDelta.X,
                        oldLayout.Offset.Y + localDelta.Y)
                });
        }

        if (newLayouts.All(pair =>
                document.GetElement(pair.Key).Layout ==
                pair.Value))
        {
            return false;
        }

        document.Execute(
            new SetEditorUiLayoutsCommand(
                document,
                newLayouts));

        return true;
    }

}
