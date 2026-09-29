using Engine.Core.Math;
using Engine.Graphics.Fonts;
using Engine.Input;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;
using Engine.UI.Styling;

namespace Engine.Tests.UI.Layout;

public sealed class UiScrollViewTests
{
    [Fact]
    public void Overflow_ShowsVerticalScrollBar()
    {
        var scrollView =
            CreateScrollView(
                viewportSize: new Vector2(100.0f, 100.0f),
                contentSize: new Vector2(100.0f, 300.0f));

        var verticalScrollBar =
            scrollView.Children
                .OfType<UiScrollBar>()
                .Single(
                    static bar =>
                        bar.Orientation ==
                        UiScrollBarOrientation.Vertical);

        Assert.True(
            verticalScrollBar.Visible);
    }

    [Fact]
    public void NoOverflow_HidesVerticalScrollBar()
    {
        var scrollView =
            CreateScrollView(
                viewportSize: new Vector2(100.0f, 100.0f),
                contentSize: new Vector2(100.0f, 80.0f));

        var verticalScrollBar =
            scrollView.Children
                .OfType<UiScrollBar>()
                .Single(
                    static bar =>
                        bar.Orientation ==
                        UiScrollBarOrientation.Vertical);

        Assert.False(
            verticalScrollBar.Visible);
    }

    [Fact]
    public void VerticalScrollBar_UsesViewportToContentRatio()
    {
        var scrollView =
            CreateScrollView(
                viewportSize: new Vector2(100.0f, 100.0f),
                contentSize: new Vector2(100.0f, 300.0f));

        var verticalScrollBar =
            scrollView.Children
                .OfType<UiScrollBar>()
                .Single(
                    static bar =>
                        bar.Orientation ==
                        UiScrollBarOrientation.Vertical);

        Assert.Equal(
            1.0f / 3.0f,
            verticalScrollBar.ViewportRatio,
            precision: 5);
    }

    [Fact]
    public void WheelScrolling_UpdatesScrollOffset()
    {
        var scrollView =
            CreateScrollView(
                viewportSize: new Vector2(100.0f, 100.0f),
                contentSize: new Vector2(100.0f, 300.0f));

        var root =
            new UiRoot();

        root.AddChild(
            scrollView);

        root.Arrange(
            new UiRect(
                0.0f,
                0.0f,
                100.0f,
                100.0f));

        var pointer =
            new TestPointerInput
            {
                Position =
                    new Vector2(
                        50.0f,
                        50.0f),

                ScrollDelta = -1.0f
            };

        var router =
            new UiInputRouter(
                root,
                pointer,
                new EmptyTextInput(),
                new UiFocusManager());

        router.Update();

        Assert.True(
            scrollView.ScrollOffset.Y > 0.0f);

        Assert.Equal(
            scrollView.ScrollOffset.Y,
            scrollView.Children
                .OfType<UiScrollBar>()
                .Single(
                    static bar =>
                        bar.Orientation ==
                        UiScrollBarOrientation.Vertical)
                .Value *
            200.0f,
            precision: 4);
    }

    private static UiScrollView CreateScrollView(
        Vector2 viewportSize,
        Vector2 contentSize)
    {
        var scrollView =
            new UiScrollView
            {
                Width = viewportSize.X,
                Height = viewportSize.Y
            };

        scrollView.SetContent(
            new TestWidget
            {
                Width = contentSize.X,
                Height = contentSize.Y
            });

        var fonts =
            new TestFontManager();

        var context =
            new UiLayoutContext(
                fonts,
                new UiTheme());

        scrollView.Measure(
            context,
            viewportSize);

        scrollView.Arrange(
            new UiRect(
                0.0f,
                0.0f,
                viewportSize.X,
                viewportSize.Y));

        return scrollView;
    }

    private sealed class TestWidget : UiWidget
    {
    }

    private sealed class TestFontManager : IFontManager
    {
        public FontHandle DefaultFont =>
            FontHandle.Invalid;

        public FontHandle Create(
            ReadOnlyMemory<byte> data,
            FontDescription description) =>
            throw new NotSupportedException();

        public bool Exists(
            FontHandle font) =>
            throw new NotSupportedException();

        public FontGlyph GetGlyph(
            FontHandle font,
            int codepoint) =>
            throw new NotSupportedException();

        public float GetKerning(
            FontHandle font,
            int leftCodepoint,
            int rightCodepoint) =>
            throw new NotSupportedException();

        public FontMetrics GetMetrics(
            FontHandle font) =>
            throw new NotSupportedException();

        public void Destroy(
            FontHandle font) =>
            throw new NotSupportedException();

        public FontTextMetrics MeasureText(
            FontHandle font,
            string text) =>
            throw new NotSupportedException();
    }

    private sealed class TestPointerInput : IPointerInput
    {
        public Vector2 Position { get; set; }

        public float ScrollDelta { get; set; }

        public bool LeftDown { get; set; }

        public bool LeftPressed { get; set; }

        public bool LeftReleased { get; set; }

        public bool IsDown(
            InputMouseButton button)
        {
            return button ==
                   InputMouseButton.Left &&
                   LeftDown;
        }

        public bool IsPressed(
            InputMouseButton button)
        {
            return button ==
                   InputMouseButton.Left &&
                   LeftPressed;
        }

        public bool IsReleased(
            InputMouseButton button)
        {
            var released =
                button ==
                InputMouseButton.Left &&
                LeftReleased;

            if (released)
            {
                LeftReleased = false;
            }

            return released;
        }
    }

    private sealed class EmptyTextInput : ITextInput
    {
        public IReadOnlyList<char> Characters =>
            Array.Empty<char>();

        public bool IsPressed(
            TextInputKey key)
        {
            return false;
        }
    }
}