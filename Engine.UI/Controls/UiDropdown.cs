using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.UI.Controls;

public sealed class UiDropdown : UiContainer
{
    private readonly UiOverlayLayer? _overlay;
    private readonly UiButton _button;
    private readonly UiPanel _popup;
    private readonly UiScrollView _scrollView;
    private readonly UiList _list;

    private readonly List<UiButton> _options = new();

    private int _selectedIndex = -1;

    public UiDropdown(
        UiOverlayLayer? overlay = null)
    {
        _overlay =
            overlay;

        Width = 220.0f;
        Height = 42.0f;

        HorizontalAlignment =
            UiHorizontalAlignment.Left;

        VerticalAlignment =
            UiVerticalAlignment.Top;

        _button =
            new UiButton("SELECT")
            {
                Width = 220.0f,
                Height = 42.0f
            };

        _button.Clicked +=
            TogglePopup;

        _popup =
            new UiPanel
            {
                Width = 220.0f,
                Height = 180.0f,

                Padding =
                    new UiThickness(4.0f),

                Background =
                    new UiColor(
                        25,
                        25,
                        25,
                        250),

                ZIndex = 100
            };

        _scrollView =
            new UiScrollView
            {
                Width = 212.0f,
                Height = 172.0f,

                VerticalScrolling = true,
                HorizontalScrolling = false,
                ScrollSpeed = 32.0f
            };

        _list =
            new UiList
            {
                Width = 204.0f
            };

        _scrollView.SetContent(
            _list);

        _popup.AddChild(
            _scrollView);

        _popup.Visible = false;
        _popup.IsHitTestVisible = false;

        AddChild(
            _button);
    }

    public bool IsOpen =>
        _popup.Visible;

    public int SelectedIndex =>
        _selectedIndex;

    public string? SelectedText =>
        _selectedIndex >= 0 &&
        _selectedIndex < _options.Count
            ? _options[_selectedIndex].Text
            : null;

    public event Action<
        int,
        string>? SelectionChanged;

    public void AddOption(
        string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var index =
            _options.Count;

        var option =
            new UiButton(text)
            {
                Width = 200.0f,
                Height = 36.0f,
                FontSize = 14.0f
            };

        option.Clicked +=
            () => Select(index);

        _options.Add(
            option);

        _list.AddItem(
            option);
    }

    public void ClearOptions()
    {
        _options.Clear();

        _list.ClearItems();

        _selectedIndex = -1;

        _button.Text =
            "SELECT";
    }

    public void SetSelectedIndex(
        int index)
    {
        if (index < 0 ||
            index >= _options.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index));
        }

        Select(
            index);
    }

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        _button.Measure(
            context,
            new Vector2(
                Width ?? 220.0f,
                Height ?? 42.0f));

        _popup.Measure(
            context,
            new Vector2(
                Width ?? 220.0f,
                180.0f));

        return new Vector2(
            Width ?? 220.0f,
            Height ?? 42.0f);
    }

    protected override void ArrangeCore(
        UiRect finalRect)
    {
        _button.Arrange(
            new UiRect(
                finalRect.X,
                finalRect.Y,
                finalRect.Width,
                finalRect.Height));

        var popupHeight =
            _popup.Height ??
            180.0f;

        _popup.Arrange(
            new UiRect(
                finalRect.X,
                finalRect.Bottom,
                finalRect.Width,
                popupHeight));
    }

    private void TogglePopup()
    {
        if (_popup.Visible)
        {
            ClosePopup();
        }
        else
        {
            OpenPopup();
        }
    }

    private void OpenPopup()
    {
        if (_options.Count == 0)
        {
            return;
        }

        if (_overlay is null)
        {
            AddChild(
                _popup);
        }
        else
        {
            var position =
                new Vector2(
                    Bounds.X,
                    Bounds.Bottom);

            _overlay.Show(
                _popup,
                position);
        }

        _popup.Visible =
            true;

        _popup.IsHitTestVisible =
            true;
    }

    private void ClosePopup()
    {
        _popup.Visible =
            false;

        _popup.IsHitTestVisible =
            false;

        if (_overlay is not null &&
            ReferenceEquals(
                _popup.Parent,
                _overlay))
        {
            _overlay.Hide(
                _popup);
        }
        else if (ReferenceEquals(
                     _popup.Parent,
                     this))
        {
            RemoveChild(
                _popup);
        }
    }

    private void Select(
        int index)
    {
        if (index < 0 ||
            index >= _options.Count)
        {
            return;
        }

        _selectedIndex =
            index;

        _button.Text =
            _options[index].Text;

        ClosePopup();

        SelectionChanged?.Invoke(
            index,
            _options[index].Text);
    }
}