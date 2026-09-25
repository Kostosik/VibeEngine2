using Engine.Core.Math;
using Engine.Graphics;
using Engine.Input;
using Engine.Input.Cursors;
using Engine.UI.Input;
using Engine.UI.Layout;
using Engine.UI.Screens;
using Engine.UI.Styling;

namespace Engine.UI.Core;

public sealed class UiSystem
{
    private readonly UiRenderContext _renderContext;
    public UiOverlayLayer Overlays { get; }
    private int _width;
    private int _height;
    private readonly UiLayoutContext _layoutContext;
    public UiScreenManager Screens { get; }
    private readonly UiInputRouter? _inputRouter;
    public UiFocusManager Focus { get; }

    private void Layout()
    {
        var viewport =
            new Vector2(
                _width,
                _height);

        Root.Measure(
            _layoutContext,
            viewport);

        Root.Arrange(
            new UiRect(
                0.0f,
                0.0f,
                _width,
                _height));
    }
    public UiSystem(
        IGraphicsDevice graphics,
        int width,
        int height,
        IPointerInput? pointer = null,
        ITextInput? textInput = null,
        ICursorService? cursor = null,
        UiTheme? theme = null)
    {
        ArgumentNullException.ThrowIfNull(graphics);

        if (width <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(height));

        _width = width;
        _height = height;

        Theme =
            theme ??
            new UiTheme();
        _layoutContext =
    new UiLayoutContext(
        graphics.Fonts,
        Theme);

        if (!Theme.DefaultFont.IsValid)
        {
            Theme.DefaultFont =
                graphics.Fonts.DefaultFont;
        }

        Root =
            new UiRoot();

        Overlays =
    new UiOverlayLayer();

        Root.AddChild(
            Overlays);

        Focus =
            new UiFocusManager();

        Screens =
            new UiScreenManager(
                Focus);

        Root.AddChild(Screens.Root);

        _renderContext =
            new UiRenderContext(
                graphics,
                Theme);

        if (pointer is not null &&
            textInput is not null)
        {
            _inputRouter =
                new UiInputRouter(
                    Root,
                    pointer,
                    textInput,
                    Focus,
                    cursor);
        }
    }

    public UiRoot Root { get; }

    public UiTheme Theme { get; }

    public void ShowOverlay(
    UiWidget widget,
    Vector2 position)
    {
        Overlays.Show(
            widget,
            position);
    }

    public bool ConsumesKeyboardInput
    {
        get
        {
            var focused =
                Focus.FocusedWidget;

            if (focused is null ||
                !focused.Visible ||
                !focused.Enabled ||
                !focused.ConsumesKeyboardInput)
            {
                return false;
            }

            return IsInRoot(
                focused);
        }
    }
    private bool IsInRoot(
    UiWidget widget)
    {
        var current =
            widget;

        while (current.Parent is not null)
        {
            current =
                current.Parent;
        }

        return ReferenceEquals(
            current,
            Root);
    }
    public void HideOverlay(
        UiWidget widget)
    {
        Overlays.Hide(
            widget);
    }

    public void AddOverlay(
    UiWidget widget)
    {
        ArgumentNullException.ThrowIfNull(
            widget);

        Overlays.AddChild(
            widget);
    }

    public bool RemoveOverlay(
        UiWidget widget)
    {
        ArgumentNullException.ThrowIfNull(
            widget);

        return Overlays.RemoveChild(
            widget);
    }

    public void ClearOverlays()
    {
        Overlays.ClearChildren();
    }

    public void Resize(
        int width,
        int height)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(height));

        _width = width;
        _height = height;
    }

    public void Update(
        double deltaSeconds)
    {
        Layout();

        _inputRouter?.Update();

        Root.Update(
            deltaSeconds);
    }

    public void Render()
    {
        Layout();

        Root.Render(
            _renderContext);
    }
}