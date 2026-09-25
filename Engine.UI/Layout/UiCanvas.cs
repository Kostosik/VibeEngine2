using Engine.Core.Math;
using Engine.UI.Core;

namespace Engine.UI.Layout;

public class UiCanvas : UiContainer
{
    private readonly Dictionary<
        UiWidget,
        Vector2> _positions = new();

    public void SetPosition(
        UiWidget child,
        Vector2 position)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (!ReferenceEquals(
                child.Parent,
                this))
        {
            throw new InvalidOperationException(
                "The widget is not a child of this canvas.");
        }

        _positions[child] =
            position;
    }

    public Vector2 GetPosition(
        UiWidget child)
    {
        ArgumentNullException.ThrowIfNull(child);

        return _positions.TryGetValue(
            child,
            out var position)
            ? position
            : Vector2.Zero;
    }

    public override bool RemoveChild(
        UiWidget child)
    {
        var removed =
            base.RemoveChild(
                child);

        if (removed)
        {
            _positions.Remove(
                child);
        }

        return removed;
    }

    public override void ClearChildren()
    {
        base.ClearChildren();

        _positions.Clear();
    }

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        foreach (var child in Children)
        {
            child.Measure(
                context,
                availableSize);
        }

        return availableSize;
    }

    protected override void ArrangeCore(
        UiRect finalRect)
    {
        foreach (var child in Children)
        {
            var position =
                GetPosition(
                    child);

            child.Arrange(
                new UiRect(
                    finalRect.X + position.X,
                    finalRect.Y + position.Y,
                    finalRect.Width - position.X,
                    finalRect.Height - position.Y));
        }
    }
}