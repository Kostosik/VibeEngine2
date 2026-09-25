using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Input;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.UI.Controls;

public sealed class UiModal : UiContainer
{
    private readonly UiOverlayLayer _overlay;
    private readonly UiFocusManager _focus;

    private UiWidget? _previousFocus;

    public UiModal(
        UiOverlayLayer overlay,
        UiFocusManager focus)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        ArgumentNullException.ThrowIfNull(focus);

        _overlay = overlay;
        _focus = focus;

        HorizontalAlignment =
            UiHorizontalAlignment.Stretch;

        VerticalAlignment =
            UiVerticalAlignment.Stretch;

        IsHitTestVisible = true;

        ZIndex = 1000;

        ContentHost =
            new UiPanel
            {
                Width = 480.0f,
                Height = 260.0f,

                Padding =
                    new UiThickness(20.0f),

                Background =
                    new UiColor(
                        25,
                        25,
                        25,
                        255),

                HorizontalAlignment =
                    UiHorizontalAlignment.Center,

                VerticalAlignment =
                    UiVerticalAlignment.Center
            };

        AddChild(
            ContentHost);
    }

    public UiPanel ContentHost { get; }

    public UiColor BackdropColor { get; set; } =
        new(
            0,
            0,
            0,
            150);

    public bool CloseOnEscape { get; set; } = true;

    public bool IsOpen { get; private set; }

    public event Action? Closed;

    public void SetContent(
        UiWidget content)
    {
        ArgumentNullException.ThrowIfNull(
            content);

        if (ContentHost.Children.Count > 0 &&
            ReferenceEquals(
                ContentHost.Children[0],
                content))
        {
            return;
        }

        if (content.Parent is not null)
        {
            throw new InvalidOperationException(
                "The content widget already has a parent.");
        }

        ContentHost.ClearChildren();

        ContentHost.AddChild(
            content);
    }

    public void SetSize(
        float width,
        float height)
    {
        ContentHost.Width =
            MathF.Max(
                1.0f,
                width);

        ContentHost.Height =
            MathF.Max(
                1.0f,
                height);
    }

    public void Show(
        UiWidget? initialFocus = null)
    {
        if (IsOpen)
        {
            return;
        }

        _previousFocus =
            _focus.FocusedWidget;

        _overlay.Show(
            this,
            Vector2.Zero);

        IsOpen = true;
        Visible = true;

        _focus.ClearFocus();

        if (initialFocus is not null &&
            initialFocus.Focusable)
        {
            _focus.SetFocus(
                initialFocus);
        }
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;

        _overlay.Hide(
            this);

        _focus.ClearFocus();

        if (_previousFocus is not null &&
            _previousFocus.Visible &&
            _previousFocus.Enabled &&
            _previousFocus.Focusable)
        {
            _focus.SetFocus(
                _previousFocus);
        }

        _previousFocus =
            null;

        Closed?.Invoke();
    }

    protected override void OnRender(
        UiRenderContext context)
    {
        context.DrawRectangle(
            Bounds,
            BackdropColor,
            filled: true);
    }

    protected override void OnPointerDown(
        UiPointerEvent pointer)
    {
        pointer.Handled = true;
    }

    protected override void OnKeyEvent(
        UiKeyEvent keyEvent)
    {
        if (keyEvent.Key !=
            TextInputKey.Escape)
        {
            return;
        }

        if (!CloseOnEscape)
        {
            return;
        }

        Close();

        keyEvent.Handled =
            true;
    }
}