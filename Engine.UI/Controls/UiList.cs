using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.UI.Controls;

public sealed class UiList : UiPanel
{
    private readonly UiScrollView _scrollView;
    private readonly UiStackPanel _items;

    public UiList()
    {
        Padding =
            UiThickness.Zero;

        _scrollView =
            new UiScrollView
            {
                VerticalScrolling = true,
                HorizontalScrolling = false,
                ScrollSpeed = 32.0f
            };

        _items =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical,
                Spacing = 4.0f
            };

        _scrollView.SetContent(
            _items);

        AddChild(
            _scrollView);
    }

    public void AddItem(
        UiWidget item)
    {
        _items.AddChild(
            item);
    }

    public bool RemoveItem(
        UiWidget item)
    {
        return _items.RemoveChild(
            item);
    }

    public void ClearItems()
    {
        _items.ClearChildren();
    }
}