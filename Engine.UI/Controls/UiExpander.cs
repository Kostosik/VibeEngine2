using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.UI.Controls;

public sealed class UiExpander : UiContainer
{
    private readonly UiButton _headerButton;
    private readonly UiPanel _contentHost;

    private string _headerText;
    private bool _isExpanded;

    private float _headerHeight = 32.0f;
    private float _contentSpacing = 4.0f;

    public UiExpander(string headerText)
    {
        ArgumentNullException.ThrowIfNull(headerText);

        _headerText = headerText;

        _headerButton = new UiButton(
            BuildHeaderText())
        {
            Height = _headerHeight,
            FontSize = 14.0f,
            HorizontalAlignment =
                UiHorizontalAlignment.Stretch,
            VerticalAlignment =
                UiVerticalAlignment.Stretch,
            HorizontalTextAlignment =
                UiTextHorizontalAlignment.Left,
            VerticalTextAlignment =
                UiTextVerticalAlignment.Center,
            Padding = new UiThickness(
                10.0f,
                6.0f,
                10.0f,
                6.0f)
        };

        _headerButton.Clicked += ToggleExpanded;

        _contentHost = new UiPanel
        {
            Background = UiColor.Transparent,
            Padding = new UiThickness(8.0f),
            HorizontalAlignment =
                UiHorizontalAlignment.Stretch,
            VerticalAlignment =
                UiVerticalAlignment.Stretch,
            Visible = false
        };

        base.AddChild(_headerButton);
        base.AddChild(_contentHost);
    }

    public string HeaderText
    {
        get => _headerText;

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (_headerText == value)
            {
                return;
            }

            _headerText = value;
            UpdateHeaderText();
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;

        set
        {
            if (_isExpanded == value)
            {
                return;
            }

            _isExpanded = value;

            _contentHost.Visible =
                _isExpanded && Content is not null;

            UpdateHeaderText();

            ExpandedChanged?.Invoke(_isExpanded);
        }
    }

    public float HeaderHeight
    {
        get => _headerHeight;

        set
        {
            if (!float.IsFinite(value) || value <= 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value));
            }

            _headerHeight = value;
            _headerButton.Height = value;
        }
    }

    public float ContentSpacing
    {
        get => _contentSpacing;

        set
        {
            if (!float.IsFinite(value) || value < 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value));
            }

            _contentSpacing = value;
        }
    }

    public UiThickness ContentPadding
    {
        get => _contentHost.Padding;
        set => _contentHost.Padding = value;
    }

    public UiWidget? Content =>
        _contentHost.Children.Count > 0
            ? _contentHost.Children[0]
            : null;

    public event Action<bool>? ExpandedChanged;

    public void SetContent(UiWidget content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (ReferenceEquals(content, this))
        {
            throw new InvalidOperationException(
                "An expander cannot contain itself.");
        }

        if (ReferenceEquals(Content, content))
        {
            return;
        }

        if (content.Parent is not null)
        {
            throw new InvalidOperationException(
                "The content widget already has a parent.");
        }

        _contentHost.ClearChildren();
        _contentHost.AddChild(content);

        _contentHost.Visible = _isExpanded;
    }

    public void ClearContent()
    {
        _contentHost.ClearChildren();
        _contentHost.Visible = false;
    }

    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }

    private string BuildHeaderText() =>
        $"{(_isExpanded ? "▼" : "►")} {_headerText}";

    private void UpdateHeaderText()
    {
        _headerButton.Text = BuildHeaderText();
    }

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        _headerButton.Measure(
            context,
            new Vector2(
                availableSize.X,
                _headerHeight));

        var contentSize = Vector2.Zero;

        // Измеряем содержимое, даже когда оно свёрнуто,
        // но не включаем его высоту в размер контрола.
        if (Content is not null)
        {
            _contentHost.Measure(
                context,
                new Vector2(
                    availableSize.X,
                    float.PositiveInfinity));

            contentSize = _contentHost.DesiredSize;
        }

        var desiredHeight = _headerHeight;

        if (_isExpanded && Content is not null)
        {
            desiredHeight +=
                _contentSpacing + contentSize.Y;
        }

        return new Vector2(
            MathF.Max(
                _headerButton.DesiredSize.X,
                contentSize.X),
            desiredHeight);
    }

    protected override void ArrangeCore(
        UiRect finalRect)
    {
        var headerHeight = MathF.Min(
            _headerHeight,
            finalRect.Height);

        _headerButton.Arrange(
            new UiRect(
                finalRect.X,
                finalRect.Y,
                finalRect.Width,
                headerHeight));

        if (!_isExpanded || Content is null)
        {
            _contentHost.Arrange(
                new UiRect(
                    finalRect.X,
                    finalRect.Bottom,
                    finalRect.Width,
                    0.0f));

            return;
        }

        var contentY =
            finalRect.Y +
            _headerHeight +
            _contentSpacing;

        var contentHeight = MathF.Max(
            0.0f,
            finalRect.Bottom - contentY);

        _contentHost.Arrange(
            new UiRect(
                finalRect.X,
                contentY,
                finalRect.Width,
                contentHeight));
    }
}