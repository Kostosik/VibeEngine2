using Engine.Core.Math;
using Engine.Graphics.Fonts;
using Engine.Input;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;
using Engine.UI.Styling;

namespace Engine.Tests.UI.Controls;

public sealed class UiDropdownTests
{
    [Fact]
    public void Popup_WhenOverlappingSiblingIsPresent_ReceivesPointerInput()
    {
        var root = new UiRoot();

        var overlay = new UiOverlayLayer();
        root.AddChild(overlay);

        var content = new UiCanvas();
        root.AddChild(content);

        var dropdown = new UiDropdown();

        dropdown.AddOption("First");
        dropdown.AddOption("Second");

        content.AddChild(dropdown);
        content.SetPosition(
            dropdown,
            Vector2.Zero);

        var textBox =
            new UiTextBox("Underlying text")
            {
                Width = 220.0f,
                Height = 32.0f
            };

        var layoutContext =
    new UiLayoutContext(
        new TestFontManager(),
        new UiTheme());

        var viewportSize =
            new Vector2(400.0f, 300.0f);

        content.AddChild(textBox);
        content.SetPosition(
            textBox,
            new Vector2(0.0f, 85.0f));

        var pointer =
            new TestPointerInput();

        var focus =
            new UiFocusManager();

        var router =
            new UiInputRouter(
                root,
                pointer,
                new EmptyTextInput(),
                focus);

        root.Arrange(
            new UiRect(0.0f, 0.0f, 400.0f, 300.0f));

        root.Measure(
    layoutContext,
    viewportSize);

        root.Arrange(
            new UiRect(
                0.0f,
                0.0f,
                viewportSize.X,
                viewportSize.Y));

        Click(
            router,
            pointer,
            new Vector2(20.0f, 20.0f));

        Assert.True(dropdown.IsOpen);

        root.Arrange(
            new UiRect(0.0f, 0.0f, 400.0f, 300.0f));

        Click(
            router,
            pointer,
            new Vector2(20.0f, 100.0f));

        Assert.Equal(
            "Second",
            dropdown.SelectedText);

        Assert.False(dropdown.IsOpen);
    }

    private sealed class TestFontManager : IFontManager
    {
        public FontHandle DefaultFont =>
            FontHandle.Invalid;

        public FontHandle Create(
            ReadOnlyMemory<byte> data,
            FontDescription description) =>
            throw new NotSupportedException();

        public bool Exists(FontHandle font) =>
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

        public FontMetrics GetMetrics(FontHandle font) =>
            throw new NotSupportedException();

        public void Destroy(FontHandle font) =>
            throw new NotSupportedException();

        public FontTextMetrics MeasureText(
            FontHandle font,
            string text) =>
            throw new NotSupportedException();
    }

    private static void Click(
        UiInputRouter router,
        TestPointerInput pointer,
        Vector2 position)
    {
        pointer.Position = position;
        pointer.LeftPressed = true;
        router.Update();

        pointer.LeftPressed = false;
        pointer.LeftReleased = true;
        router.Update();
    }

    [Fact]
    public void ClearOptions_WhenPopupIsOpen_ClosesPopup()
    {
        var root =
            new UiRoot();

        var dropdown =
            new UiDropdown();

        dropdown.AddOption("First");

        root.AddChild(
            dropdown);

        dropdown.Arrange(
            new UiRect(
                0.0f,
                0.0f,
                220.0f,
                42.0f));

        var pointer =
            new TestPointerInput
            {
                Position =
                    new Vector2(
                        20.0f,
                        20.0f)
            };

        var textInput =
            new EmptyTextInput();

        var focus =
            new UiFocusManager();

        var router =
            new UiInputRouter(
                root,
                pointer,
                textInput,
                focus);

        pointer.LeftPressed = true;

        router.Update();

        pointer.LeftPressed = false;
        pointer.LeftReleased = true;

        router.Update();

        Assert.True(
            dropdown.IsOpen);

        dropdown.ClearOptions();

        Assert.False(
            dropdown.IsOpen);

        Assert.Equal(
            -1,
            dropdown.SelectedIndex);

        Assert.Null(
            dropdown.SelectedText);
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