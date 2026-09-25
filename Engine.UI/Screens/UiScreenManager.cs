using Engine.Core.Math;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.UI.Screens;

public sealed class UiScreenManager
{
    private readonly UiScreenHost _host;
    private readonly UiFocusManager _focus;

    private readonly List<UiScreen> _stack = new();

    public UiScreenManager(
        UiFocusManager focus)
    {
        ArgumentNullException.ThrowIfNull(focus);

        _focus = focus;

        _host = new UiScreenHost
        {
            HorizontalAlignment =
                UiHorizontalAlignment.Stretch,

            VerticalAlignment =
                UiVerticalAlignment.Stretch,

            ZIndex = 100
        };
    }

    public UiWidget Root =>
        _host;

    public UiScreen? CurrentScreen =>
        _stack.Count > 0
            ? _stack[^1]
            : null;

    public int Count =>
        _stack.Count;

    public void Replace(
        UiScreen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);

        if (CurrentScreen is not null)
        {
            CurrentScreen.OnExit();

            _host.RemoveChild(
                CurrentScreen.Root);
        }

        _stack.Clear();

        _focus.ClearFocus();

        _stack.Add(screen);

        _host.AddChild(
            screen.Root);

        screen.Root.Visible = true;

        screen.OnEnter(_focus);
    }

    public void Push(
        UiScreen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);

        if (CurrentScreen is not null)
        {
            CurrentScreen.Root.Visible = false;

            _host.RemoveChild(
                CurrentScreen.Root);
        }

        _focus.ClearFocus();

        _stack.Add(screen);

        _host.AddChild(
            screen.Root);

        screen.Root.Visible = true;

        screen.OnEnter(_focus);
    }

    public bool Pop()
    {
        if (_stack.Count == 0)
        {
            return false;
        }

        var current =
            _stack[^1];

        current.OnExit();

        _host.RemoveChild(
            current.Root);

        _stack.RemoveAt(
            _stack.Count - 1);

        _focus.ClearFocus();

        if (CurrentScreen is not null)
        {
            _host.AddChild(
                CurrentScreen.Root);

            CurrentScreen.Root.Visible =
                true;

            CurrentScreen.OnEnter(_focus);
        }

        return true;
    }

    public void Clear()
    {
        if (CurrentScreen is not null)
        {
            CurrentScreen.OnExit();
        }

        _host.ClearChildren();

        _stack.Clear();

        _focus.ClearFocus();
    }

    private sealed class UiScreenHost : UiCanvas
    {
        internal override UiWidget? HitTest(
            Vector2 point)
        {
            if (!Visible ||
                !Enabled)
            {
                return null;
            }

            for (var i = Children.Count - 1; i >= 0; i--)
            {
                var hit =
                    Children[i].HitTest(point);

                if (hit is not null)
                {
                    return hit;
                }
            }

            return null;
        }
    }
}