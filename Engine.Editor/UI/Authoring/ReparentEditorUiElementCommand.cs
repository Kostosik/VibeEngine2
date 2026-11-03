
using Engine.Core.Math;
using Engine.Editor.Commands;

namespace Engine.Editor.UI.Authoring;

public sealed class ReparentEditorUiElementCommand : IEditorCommand
{
    private readonly EditorUiElement _element;
    private readonly EditorUiElement _oldParent;
    private readonly EditorUiElement _newParent;

    private readonly int _oldIndex;
    private readonly int _newIndex;

    private readonly EditorUiLayout _oldLayout;
    private readonly EditorUiLayout _newLayout;

    public ReparentEditorUiElementCommand(
        EditorUiDocument document,
        Guid elementId,
        Guid newParentId)
    {
        ArgumentNullException.ThrowIfNull(document);

        _element = document.GetElement(elementId);
        _newParent = document.GetElement(newParentId);

        if (ReferenceEquals(_element, document.Root))
        {
            throw new ArgumentException(
                "The root UI element cannot be moved.",
                nameof(elementId));
        }

        if (ReferenceEquals(_element, _newParent))
        {
            throw new ArgumentException(
                "An element cannot be its own parent.",
                nameof(newParentId));
        }

        EditorUiHierarchyRules.EnsureCanAcceptChild(
    _newParent,
    _element.Type);

        for (var current = _newParent;
             current is not null;
             current = current.Parent)
        {
            if (ReferenceEquals(current, _element))
            {
                throw new ArgumentException(
                    "An element cannot become a child of its descendant.",
                    nameof(newParentId));
            }
        }

        _oldParent = _element.Parent
            ?? throw new InvalidOperationException(
                "The element has no parent.");

        if (ReferenceEquals(_oldParent, _newParent))
        {
            throw new ArgumentException(
                "The element already belongs to this parent.",
                nameof(newParentId));
        }

        _oldIndex = _oldParent.IndexOf(_element);
        _newIndex = _newParent.Children.Count;

        _oldLayout = _element.Layout;

        var elementTransform = CalculateWorldTransform(
            document,
            _element);

        var parentTransform = CalculateWorldTransform(
            document,
            _newParent);

        var contentArea = GetChildContentArea(
            _newParent,
            _newParent.Layout.Size);

        var relativePosition = Rotate(
            new Vector2(
                elementTransform.Origin.X - parentTransform.Center.X,
                elementTransform.Origin.Y - parentTransform.Center.Y),
            -parentTransform.RotationDegrees);

        var localOrigin = new Vector2(
            _newParent.Layout.Size.X * 0.5f +
                relativePosition.X -
                contentArea.Origin.X,

            _newParent.Layout.Size.Y * 0.5f +
                relativePosition.Y -
                contentArea.Origin.Y);

        var listInsertionOffset =
    _newParent.Type == EditorUiElementType.List
        ? GetListInsertionOffset(
            _newParent,
            _newIndex)
        : 0.0f;

        var anchor = _oldLayout.Anchor;

        var offset = new Vector2(
    localOrigin.X -
        contentArea.Size.X * anchor.X +
        _oldLayout.Size.X * anchor.X,

    _newParent.Type == EditorUiElementType.List
        ? localOrigin.Y - listInsertionOffset
        : localOrigin.Y -
          contentArea.Size.Y * anchor.Y +
          _oldLayout.Size.Y * anchor.Y);

        _newLayout = _oldLayout with
        {
            Offset = offset,
            RotationDegrees =
                elementTransform.RotationDegrees -
                parentTransform.RotationDegrees
        };
    }

    private const float ListItemSpacing = 4.0f;

    private static float GetListInsertionOffset(
        EditorUiElement list,
        int index)
    {
        var offset = 0.0f;

        for (var i = 0; i < index; i++)
        {
            offset += GetListItemHeight(list.Children[i]) +
                      ListItemSpacing;
        }

        return offset;
    }

    private static float GetListItemHeight(
        EditorUiElement element)
    {
        if (element.Type != EditorUiElementType.Expander)
        {
            return element.Layout.Size.Y;
        }

        if (!element.ExpanderIsExpanded ||
            element.Children.Count == 0)
        {
            return 32.0f;
        }

        return 32.0f +
               4.0f +
               16.0f +
               GetListItemHeight(element.Children[0]);
    }

    private static Rectangle ResolveElementRect(
        EditorUiElement element,
        EditorUiElement? parent,
        Vector2 parentSize)
    {
        if (parent?.Type != EditorUiElementType.List)
        {
            return element.Layout.Resolve(parentSize);
        }

        var rowOffset = GetListInsertionOffset(
            parent,
            parent.IndexOf(element));

        var layout = element.Layout;

        return new Rectangle(
            parentSize.X * layout.Anchor.X +
            layout.Offset.X -
            layout.Size.X * layout.Anchor.X,

            rowOffset + layout.Offset.Y,

            layout.Size.X,
            GetListItemHeight(element));
    }

    public void Execute()
    {
        if (!ReferenceEquals(_element.Parent, _oldParent))
        {
            throw new InvalidOperationException(
                "The element is not attached to its original parent.");
        }

        EditorUiHierarchyRules.EnsureCanAcceptChild(
    _newParent,
    _element.Type);

        if (!_oldParent.RemoveChild(_element))
        {
            throw new InvalidOperationException(
                "The element could not be detached.");
        }

        _newParent.AddChild(
            _element,
            _newIndex);

        _element.Layout = _newLayout;
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

    public void Undo()
    {
        if (!ReferenceEquals(_element.Parent, _newParent))
        {
            throw new InvalidOperationException(
                "The element is not attached to its new parent.");
        }

        EditorUiHierarchyRules.EnsureCanAcceptChild(
    _oldParent,
    _element.Type);

        if (!_newParent.RemoveChild(_element))
        {
            throw new InvalidOperationException(
                "The element could not be detached.");
        }

        _oldParent.AddChild(
            _element,
            _oldIndex);

        _element.Layout = _oldLayout;
    }

    private readonly record struct WorldTransform(
        Vector2 Origin,
        Vector2 Center,
        float RotationDegrees);

    private static WorldTransform CalculateWorldTransform(
        EditorUiDocument document,
        EditorUiElement element)
    {
        var path = new List<EditorUiElement>();

        for (var current = element;
             current is not null;
             current = current.Parent)
        {
            path.Add(current);
        }

        path.Reverse();

        var centers = new List<Vector2>(path.Count);
        var rotations = new List<float>(path.Count);

        var parentSize = document.Root.Layout.Size;
        var parentOrigin = Vector2.Zero;

        var origin = Vector2.Zero;
        var center = Vector2.Zero;

        for (var i = 0; i < path.Count; i++)
        {
            var current = path[i];

            var parentElement = i > 0
                ? path[i - 1]
                : null;

            var rect = ResolveElementRect(
                current,
                parentElement,
                parentSize);

            origin = new Vector2(
                parentOrigin.X + rect.X,
                parentOrigin.Y + rect.Y);

            center = new Vector2(
                origin.X + rect.Width * 0.5f,
                origin.Y + rect.Height * 0.5f);

            centers.Add(center);
            rotations.Add(current.Layout.RotationDegrees);

            var contentArea = GetChildContentArea(
                current,
                new Vector2(
                    rect.Width,
                    rect.Height));

            parentOrigin = origin + contentArea.Origin;
            parentSize = contentArea.Size;
        }

        var worldOrigin = origin;
        var worldCenter = center;
        var totalRotation = 0.0f;

        for (var i = path.Count - 2; i >= 0; i--)
        {
            worldOrigin = RotateAround(
                worldOrigin,
                centers[i],
                rotations[i]);

            worldCenter = RotateAround(
                worldCenter,
                centers[i],
                rotations[i]);

            totalRotation += rotations[i];
        }

        totalRotation += path[^1].Layout.RotationDegrees;

        return new WorldTransform(
            worldOrigin,
            worldCenter,
            totalRotation);
    }


    private static Vector2 RotateAround(
        Vector2 point,
        Vector2 center,
        float degrees)
    {
        return Rotate(
            new Vector2(
                point.X - center.X,
                point.Y - center.Y),
            degrees) is var offset
                ? new Vector2(
                    center.X + offset.X,
                    center.Y + offset.Y)
                : point;
    }

    private static Vector2 Rotate(
        Vector2 point,
        float degrees)
    {
        var radians = degrees * (MathF.PI / 180.0f);
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);

        return new Vector2(
            point.X * cos - point.Y * sin,
            point.X * sin + point.Y * cos);
    }
}
