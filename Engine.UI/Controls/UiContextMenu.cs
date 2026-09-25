using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.UI.Controls;

public sealed class UiContextMenu : UiCanvas
{
    private readonly UiOverlayLayer _overlay;
    private readonly UiPanel _menuPanel;
    private readonly UiStackPanel _itemsPanel;

    public UiContextMenu(
        UiOverlayLayer overlay)
    {
        ArgumentNullException.ThrowIfNull(overlay);

        _overlay = overlay;

        HorizontalAlignment =
            UiHorizontalAlignment.Stretch;

        VerticalAlignment =
            UiVerticalAlignment.Stretch;

        IsHitTestVisible = true;

        ZIndex = 15000;

        _menuPanel =
            new UiPanel
            {
                Width = 200.0f,

                Padding =
                    new UiThickness(
                        4.0f),

                Background =
                    new UiColor(
                        25,
                        25,
                        25,
                        250),

                HorizontalAlignment =
                    UiHorizontalAlignment.Left,

                VerticalAlignment =
                    UiVerticalAlignment.Top
            };

        _itemsPanel =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical,

                Spacing = 2.0f
            };

        _menuPanel.AddChild(
            _itemsPanel);

        AddChild(
            _menuPanel);

        _overlay.AddChild(
            this);

        Visible = false;
    }

    public UiColor Background
    {
        get => _menuPanel.Background;
        set => _menuPanel.Background = value;
    }

    public float Width
    {
        get => _menuPanel.Width ?? 200.0f;
        set => _menuPanel.Width = value;
    }

    public bool IsOpen =>
        Visible;

    public void AddItem(
        string text,
        Action action)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(action);

        var button =
            new UiButton(text)
            {
                Width =
                    Width - 8.0f,

                Height = 34.0f,

                FontSize = 14.0f
            };

        button.Clicked +=
            () =>
            {
                action();
                Close();
            };

        _itemsPanel.AddChild(
            button);
    }

    public void ClearItems()
    {
        _itemsPanel.ClearChildren();
    }

    public void Open(
        Vector2 position)
    {
        if (_itemsPanel.Children.Count == 0)
        {
            return;
        }

        _overlay.SetPosition(
            this,
            Vector2.Zero);

        SetPosition(
            _menuPanel,
            position);

        _menuPanel.Visible = true;
        _menuPanel.IsHitTestVisible = true;

        Visible = true;
    }

    public void Close()
    {
        _menuPanel.Visible = false;
        _menuPanel.IsHitTestVisible = false;

        Visible = false;
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

        for (var i =
                 Children.Count - 1;
             i >= 0;
             i--)
        {
            var hit =
                Children[i].HitTest(
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

    protected override void OnPointerDown(
        UiPointerEvent pointer)
    {
        pointer.Handled = true;

        if (!_menuPanel.Bounds.Contains(
                pointer.Position))
        {
            Close();
        }
    }

    protected override void OnKeyEvent(
        UiKeyEvent keyEvent)
    {
        if (keyEvent.Key !=
            Engine.Input.TextInputKey.Escape)
        {
            return;
        }

        Close();

        keyEvent.Handled = true;
    }
}