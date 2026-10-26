using Engine.Core.Math;
using Engine.Graphics.Fonts;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;
using Engine.UI.Styling;

namespace Engine.Tests.UI.Controls;

public sealed class UiExpanderTests
{
    [Fact]
    public void IsExpanded_RaisesEventOnlyWhenStateChanges()
    {
        var expander = new UiExpander("Details");
        var changes = new List<bool>();

        expander.ExpandedChanged += changes.Add;

        Assert.False(expander.IsExpanded);

        expander.IsExpanded = true;
        expander.IsExpanded = true;
        expander.IsExpanded = false;

        Assert.Equal(
            new[] { true, false },
            changes);
    }

    [Fact]
    public void Measure_ExpandingMovesFollowingSiblingDown()
    {
        var root = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 0.0f
        };

        var expander = new UiExpander("Details")
        {
            HeaderHeight = 30.0f,
            ContentSpacing = 0.0f,
            ContentPadding = new UiThickness(0.0f)
        };

        expander.SetContent(
            new FixedSizeWidget(100.0f, 40.0f));

        var followingWidget =
            new FixedSizeWidget(100.0f, 20.0f);

        root.AddChild(expander);
        root.AddChild(followingWidget);

        var context = new UiLayoutContext(
            new EmptyFontManager(),
            new UiTheme());

        root.Measure(
            context,
            new Vector2(300.0f, float.PositiveInfinity));

        root.Arrange(
            new UiRect(0.0f, 0.0f, 300.0f, 300.0f));

        Assert.Equal(30.0f, expander.DesiredSize.Y);
        Assert.Equal(30.0f, followingWidget.Bounds.Y);

        expander.IsExpanded = true;

        root.Measure(
            context,
            new Vector2(300.0f, float.PositiveInfinity));

        root.Arrange(
            new UiRect(0.0f, 0.0f, 300.0f, 300.0f));

        Assert.Equal(70.0f, expander.DesiredSize.Y);
        Assert.Equal(70.0f, followingWidget.Bounds.Y);
    }

    [Fact]
    public void SetContent_SelfThrowsWithoutLosingExistingContent()
    {
        var expander = new UiExpander("Details");
        var content = new UiPanel();

        expander.SetContent(content);

        Assert.Throws<InvalidOperationException>(
            () => expander.SetContent(expander));

        Assert.Same(content, expander.Content);
    }

    private sealed class FixedSizeWidget : UiWidget
    {
        private readonly Vector2 _size;

        public FixedSizeWidget(float width, float height)
        {
            _size = new Vector2(width, height);
        }

        protected override Vector2 MeasureCore(
            UiLayoutContext context,
            Vector2 availableSize)
        {
            return _size;
        }
    }

    private sealed class EmptyFontManager : IFontManager
    {
        public FontHandle DefaultFont => default;

        public FontHandle Create(
            ReadOnlyMemory<byte> data,
            FontDescription description)
        {
            throw new NotSupportedException();
        }

        public bool Exists(FontHandle font) => false;

        public FontGlyph GetGlyph(
            FontHandle font,
            int codepoint)
        {
            throw new NotSupportedException();
        }

        public float GetKerning(
            FontHandle font,
            int leftCodepoint,
            int rightCodepoint) => 0.0f;

        public FontMetrics GetMetrics(FontHandle font)
        {
            throw new NotSupportedException();
        }

        public void Destroy(FontHandle font)
        {
        }

        public FontTextMetrics MeasureText(
            FontHandle font,
            string text)
        {
            throw new NotSupportedException();
        }
    }
}