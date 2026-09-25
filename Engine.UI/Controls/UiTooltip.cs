using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.UI.Controls;

public sealed class UiTooltip : UiPanel
{
    private readonly UiOverlayLayer _overlay;
    private readonly UiWidget _target;
    private readonly UiLabel _label;

    private double _hoverTime;

    public UiTooltip(
        UiOverlayLayer overlay,
        UiWidget target,
        string text)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(text);

        _overlay = overlay;
        _target = target;

        Padding =
            new UiThickness(
                8.0f,
                5.0f,
                8.0f,
                5.0f);

        Background =
            new UiColor(
                20,
                20,
                20,
                240);

        HorizontalAlignment =
            UiHorizontalAlignment.Left;

        VerticalAlignment =
            UiVerticalAlignment.Top;

        IsHitTestVisible = false;

        ZIndex = 20000;

        _label =
            new UiLabel(text)
            {
                FontSize = 13.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Left,

                VerticalAlignment =
                    UiVerticalAlignment.Center
            };

        AddChild(
            _label);

        _overlay.AddChild(
            this);

        Visible = false;
    }

    public string Text
    {
        get => _label.Text;
        set => _label.Text = value;
    }

    public double ShowDelay { get; set; } = 0.5;

    public Vector2 Offset { get; set; } =
        new(
            8.0f,
            8.0f);

    public void Hide()
    {
        _hoverTime = 0.0;
        Visible = false;
    }

    public override void Update(
        double deltaSeconds)
    {
        if (!_target.Visible ||
            !_target.Enabled ||
            !_target.IsHovered)
        {
            Hide();
            return;
        }

        _hoverTime +=
            Math.Max(
                0.0,
                deltaSeconds);

        if (!Visible &&
            _hoverTime >= ShowDelay)
        {
            Show();
        }

        if (Visible)
        {
            UpdatePosition();
        }

        base.Update(
            deltaSeconds);
    }

    private void Show()
    {
        Visible = true;

        UpdatePosition();
    }

    private void UpdatePosition()
    {
        var targetBounds =
            _target.Bounds;

        var overlayBounds =
            _overlay.Bounds;

        var size =
            DesiredSize;

        var x =
            targetBounds.X +
            Offset.X;

        var y =
            targetBounds.Bottom +
            Offset.Y;

        if (size.X > 0.0f &&
            x + size.X >
            overlayBounds.Right)
        {
            x =
                targetBounds.Right -
                size.X -
                Offset.X;
        }

        if (size.Y > 0.0f &&
            y + size.Y >
            overlayBounds.Bottom)
        {
            y =
                targetBounds.Y -
                size.Y -
                Offset.Y;
        }

        x =
            MathF.Max(
                overlayBounds.X,
                MathF.Min(
                    x,
                    overlayBounds.Right -
                    size.X));

        y =
            MathF.Max(
                overlayBounds.Y,
                MathF.Min(
                    y,
                    overlayBounds.Bottom -
                    size.Y));

        _overlay.SetPosition(
            this,
            new Vector2(
                x,
                y));
    }
}