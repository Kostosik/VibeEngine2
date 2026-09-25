using Engine.Core.Math;
using Engine.UI.Layout;

namespace Engine.UI.Core;

public abstract class UiContainer : UiWidget
{
    private readonly List<UiWidget> _children = new();

    public IReadOnlyList<UiWidget> Children =>
        _children;

    public void AddChild(
        UiWidget child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (ReferenceEquals(
                child,
                this))
        {
            throw new InvalidOperationException(
                "A widget cannot be added to itself.");
        }

        if (child.Parent is not null)
        {
            throw new InvalidOperationException(
                "The widget already has a parent.");
        }

        for (var current = this.Parent;
             current is not null;
             current = current.Parent)
        {
            if (ReferenceEquals(
                    current,
                    child))
            {
                throw new InvalidOperationException(
                    "A widget cannot be added to one of its descendants.");
            }
        }

        child.Parent = this;

        _children.Add(
            child);
    }

    public virtual bool RemoveChild(
        UiWidget child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (!_children.Remove(child))
        {
            return false;
        }

        child.Parent = null;

        return true;
    }

    public virtual void ClearChildren()
    {
        foreach (var child in _children)
        {
            child.Parent = null;
        }

        _children.Clear();
    }

    public override void Update(
        double deltaSeconds)
    {
        if (!Visible ||
            !Enabled)
        {
            return;
        }

        OnUpdate(
            deltaSeconds);

        foreach (var child in _children)
        {
            child.Update(
                deltaSeconds);
        }
    }

    public override void Render(
        UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        var previousLayer =
            context.LayerOffset;

        context.LayerOffset +=
            ZIndex;

        OnRender(
            context);

        foreach (var child in GetRenderOrder())
        {
            child.Render(
                context);
        }

        context.LayerOffset =
            previousLayer;
    }

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        var width = 0.0f;
        var height = 0.0f;

        foreach (var child in _children)
        {
            child.Measure(
                context,
                availableSize);

            width =
                MathF.Max(
                    width,
                    child.DesiredSize.X);

            height =
                MathF.Max(
                    height,
                    child.DesiredSize.Y);
        }

        return new Vector2(
            width,
            height);
    }

    protected override void ArrangeCore(
        UiRect finalRect)
    {
        foreach (var child in _children)
        {
            child.Arrange(
                finalRect);
        }
    }

    internal override UiWidget? HitTest(
        Vector2 point)
    {
        if (!Visible ||
            !Enabled ||
            !IsHitTestVisible)
        {
            return null;
        }

        foreach (var child in GetHitTestOrder())
        {
            var hit =
                child.HitTest(
                    point);

            if (hit is not null)
            {
                return hit;
            }
        }

        return Bounds.Contains(point)
            ? this
            : null;
    }

    private IEnumerable<UiWidget> GetRenderOrder()
    {
        return _children
            .OrderBy(
                static child => child.ZIndex);
    }

    private IEnumerable<UiWidget> GetHitTestOrder()
    {
        return _children
            .OrderByDescending(
                static child => child.ZIndex)
            .ThenByDescending(
                child => _children.IndexOf(child));
    }
}