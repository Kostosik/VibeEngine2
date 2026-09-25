using Engine.Input;
using Engine.Input.Cursors;
using Engine.UI.Core;

namespace Engine.UI.Input;

public sealed class UiInputRouter
{
    private readonly UiRoot _root;
    private readonly IPointerInput _pointer;
    private readonly ITextInput _textInput;
    private readonly UiFocusManager _focus;
    private readonly ICursorService? _cursor;

    private UiWidget? _hoveredWidget;
    private UiWidget? _pressedWidget;
    private UiWidget? _capturedWidget;

    public UiInputRouter(
        UiRoot root,
        IPointerInput pointer,
        ITextInput textInput,
        UiFocusManager focus,
        ICursorService? cursor = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(pointer);
        ArgumentNullException.ThrowIfNull(textInput);
        ArgumentNullException.ThrowIfNull(focus);

        _root = root;
        _pointer = pointer;
        _textInput = textInput;
        _focus = focus;
        _cursor = cursor;

        _focus.SetRoot(
            root);
    }

    public void Update()
    {
        UpdatePointer();

        _focus.ValidateFocus();

        UpdateNavigation();

        _focus.ValidateFocus();

        UpdateTextInput();
    }

    private void UpdatePointer()
    {
        var position =
            _pointer.Position;

        ValidatePointerTargets();

        var hovered =
            _root.HitTest(
                position);

        UpdateHoveredWidget(
            hovered,
            position);

        var pointerEvent =
            new UiPointerEvent(
                position,
                _pointer.ScrollDelta);

        if (_pointer.IsPressed(
                InputMouseButton.Left))
        {
            _pressedWidget =
                hovered;

            if (_pressedWidget is not null)
            {
                RoutePointerEvent(
                    _pressedWidget,
                    pointerEvent,
                    static (
                        widget,
                        @event) =>
                    {
                        widget.RaisePointerDown(
                            @event);
                    });

                if (_pressedWidget is not null &&
                    _pressedWidget.Focusable)
                {
                    _focus.SetFocus(
                        _pressedWidget);
                }

                ApplyCaptureRequest(
                    pointerEvent);
            }
        }

        pointerEvent =
            new UiPointerEvent(
                position,
                _pointer.ScrollDelta);

        if (_capturedWidget is not null)
        {
            RoutePointerEvent(
                _capturedWidget,
                pointerEvent,
                static (
                    widget,
                    @event) =>
                {
                    widget.RaisePointerMove(
                        @event);
                });

            ApplyCaptureRequest(
                pointerEvent);
        }
        else if (_pressedWidget is not null)
        {
            _pressedWidget.RaisePointerMove(
                pointerEvent);
        }
        else if (_hoveredWidget is not null)
        {
            _hoveredWidget.RaisePointerMove(
                pointerEvent);
        }

        if (_pointer.IsReleased(
                InputMouseButton.Left))
        {
            var releaseTarget =
                _capturedWidget ??
                _pressedWidget;

            if (releaseTarget is not null &&
                IsAttachedToRoot(releaseTarget))
            {
                pointerEvent =
                    new UiPointerEvent(
                        position,
                        _pointer.ScrollDelta);

                RoutePointerEvent(
                    releaseTarget,
                    pointerEvent,
                    static (
                        widget,
                        @event) =>
                    {
                        widget.RaisePointerUp(
                            @event);
                    });

                ApplyCaptureRequest(
                    pointerEvent);
            }

            if (_pressedWidget is not null)
            {
                _pressedWidget.IsPressed = false;
            }

            _pressedWidget =
                null;

            _capturedWidget =
                null;
        }

        if (_hoveredWidget is not null &&
            MathF.Abs(
                _pointer.ScrollDelta) >
            0.0001f)
        {
            pointerEvent =
                new UiPointerEvent(
                    position,
                    _pointer.ScrollDelta);

            RoutePointerEvent(
                _hoveredWidget,
                pointerEvent,
                static (
                    widget,
                    @event) =>
                {
                    widget.RaisePointerWheel(
                        @event);
                });
        }

        UpdateCursor();
    }

    private void ValidatePointerTargets()
    {
        if (_hoveredWidget is not null &&
            !IsAttachedToRoot(_hoveredWidget))
        {
            _hoveredWidget.IsHovered = false;
            _hoveredWidget = null;
        }

        if (_pressedWidget is not null &&
            !IsAttachedToRoot(_pressedWidget))
        {
            _pressedWidget.IsPressed = false;
            _pressedWidget = null;
        }

        if (_capturedWidget is not null &&
            !IsAttachedToRoot(_capturedWidget))
        {
            _capturedWidget = null;
        }

        if (_capturedWidget is not null &&
            (!_capturedWidget.Visible ||
             !_capturedWidget.Enabled))
        {
            _capturedWidget = null;
        }

        if (_pressedWidget is not null &&
            (!_pressedWidget.Visible ||
             !_pressedWidget.Enabled))
        {
            _pressedWidget.IsPressed = false;
            _pressedWidget = null;
        }
    }

    private bool IsAttachedToRoot(
        UiWidget widget)
    {
        var current = widget;

        while (current is not null)
        {
            if (ReferenceEquals(
                    current,
                    _root))
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private void UpdateCursor()
    {
        if (_cursor is null)
        {
            return;
        }

        var widget =
            _capturedWidget ??
            _hoveredWidget;

        var shape =
            widget?.Cursor ??
            CursorShape.Default;

        if (_cursor.Current == shape)
        {
            return;
        }

        _cursor.Set(
            shape);
    }

    private void UpdateHoveredWidget(
        UiWidget? hovered,
        Engine.Core.Math.Vector2 position)
    {
        if (ReferenceEquals(
                hovered,
                _hoveredWidget))
        {
            return;
        }

        var pointerEvent =
            new UiPointerEvent(
                position);

        if (_hoveredWidget is not null)
        {
            _hoveredWidget.IsHovered =
                false;

            _hoveredWidget.RaisePointerLeave(
                pointerEvent);
        }

        _hoveredWidget =
            hovered;

        if (_hoveredWidget is not null)
        {
            _hoveredWidget.IsHovered =
                true;

            _hoveredWidget.RaisePointerEnter(
                pointerEvent);
        }
    }

    private void RoutePointerEvent(
        UiWidget target,
        UiPointerEvent pointer,
        Action<UiWidget, UiPointerEvent> action)
    {
        var current =
            target;

        while (current is not null)
        {
            action(
                current,
                pointer);

            if (pointer.CaptureRequested)
            {
                _capturedWidget =
                    current;

                pointer.CaptureRequested =
                    false;
            }

            if (pointer.ReleaseCaptureRequested)
            {
                if (ReferenceEquals(
                        _capturedWidget,
                        current))
                {
                    _capturedWidget =
                        null;
                }

                pointer.ReleaseCaptureRequested =
                    false;
            }

            if (pointer.Handled)
            {
                break;
            }

            current =
                current.Parent;
        }
    }

    private void ApplyCaptureRequest(
        UiPointerEvent pointer)
    {
        if (pointer.CaptureRequested)
        {
            pointer.CaptureRequested = false;
        }

        if (pointer.ReleaseCaptureRequested)
        {
            _capturedWidget = null;
            pointer.ReleaseCaptureRequested = false;
        }
    }

    private void UpdateNavigation()
    {
        if (_textInput.IsPressed(
                TextInputKey.Up))
        {
            _focus.MovePrevious();
        }

        if (_textInput.IsPressed(
                TextInputKey.Down))
        {
            _focus.MoveNext();
        }

        if (_textInput.IsPressed(
                TextInputKey.Enter))
        {
            _focus.Activate();
        }

        if (_textInput.IsPressed(
                TextInputKey.Escape))
        {
            var keyEvent =
                new UiKeyEvent(
                    TextInputKey.Escape);

            var focused =
                _focus.FocusedWidget;

            if (focused is not null)
            {
                RouteKeyEvent(
                    focused,
                    keyEvent);
            }

            if (!keyEvent.Handled)
            {
                var target =
                    _root.HitTest(
                        _pointer.Position);

                if (target is not null &&
                    !ReferenceEquals(
                        target,
                        focused))
                {
                    RouteKeyEvent(
                        target,
                        keyEvent);
                }
            }

            if (!keyEvent.Handled)
            {
                _focus.ClearFocus();
            }
        }
    }

    private void RouteKeyEvent(
        UiWidget target,
        UiKeyEvent keyEvent)
    {
        var current =
            target;

        while (current is not null)
        {
            current.RaiseKeyEvent(
                keyEvent);

            if (keyEvent.Handled)
            {
                break;
            }

            current =
                current.Parent;
        }
    }

    private void UpdateTextInput()
    {
        var focused =
            _focus.FocusedWidget;

        if (focused is null)
        {
            return;
        }

        foreach (var character in
                 _textInput.Characters)
        {
            focused.RaiseTextInput(
                character);
        }

        if (_textInput.IsPressed(
                TextInputKey.Backspace))
        {
            focused.RaiseKeyPressed(
                TextInputKey.Backspace);
        }
    }
}