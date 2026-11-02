
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

        var relativePosition = Rotate(
            new Vector2(
                elementTransform.Origin.X - parentTransform.Center.X,
                elementTransform.Origin.Y - parentTransform.Center.Y),
            -parentTransform.RotationDegrees);

        var localOrigin = new Vector2(
            _newParent.Layout.Size.X * 0.5f + relativePosition.X,
            _newParent.Layout.Size.Y * 0.5f + relativePosition.Y);

        var anchor = _oldLayout.Anchor;

        var offset = new Vector2(
            localOrigin.X -
                _newParent.Layout.Size.X * anchor.X +
                _oldLayout.Size.X * anchor.X,

            localOrigin.Y -
                _newParent.Layout.Size.Y * anchor.Y +
                _oldLayout.Size.Y * anchor.Y);

        _newLayout = _oldLayout with
        {
            Offset = offset,
            RotationDegrees =
                elementTransform.RotationDegrees -
                parentTransform.RotationDegrees
        };
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

        foreach (var current in path)
        {
            var layout = current.Layout;
            var rect = layout.Resolve(parentSize);

            origin = new Vector2(
                parentOrigin.X + rect.X,
                parentOrigin.Y + rect.Y);

            center = new Vector2(
                origin.X + rect.Width * 0.5f,
                origin.Y + rect.Height * 0.5f);

            centers.Add(center);
            rotations.Add(layout.RotationDegrees);

            parentOrigin = origin;
            parentSize = layout.Size;
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
