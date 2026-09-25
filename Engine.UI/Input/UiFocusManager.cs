using Engine.UI.Core;

namespace Engine.UI.Input;

public sealed class UiFocusManager
{
    private UiRoot? _root;

    public UiWidget? FocusedWidget { get; private set; }

    internal void SetRoot(
        UiRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);

        _root = root;

        ValidateFocus();
    }

    public void SetFocus(
        UiWidget? widget)
    {
        if (widget is not null &&
            (!IsAttachedToRoot(widget) ||
             !widget.Visible ||
             !widget.Enabled ||
             !widget.Focusable))
        {
            return;
        }

        if (ReferenceEquals(
                FocusedWidget,
                widget))
        {
            return;
        }

        if (FocusedWidget is not null)
        {
            FocusedWidget.IsFocused = false;
        }

        FocusedWidget = widget;

        if (FocusedWidget is not null)
        {
            FocusedWidget.IsFocused = true;
        }
    }

    public void ClearFocus()
    {
        SetFocus(null);
    }

    public void MoveNext()
    {
        Move(1);
    }

    public void MovePrevious()
    {
        Move(-1);
    }

    public void Activate()
    {
        ValidateFocus();

        FocusedWidget?.RaiseSubmit();
    }

    internal void ValidateFocus()
    {
        if (FocusedWidget is null)
        {
            return;
        }

        if (!IsAttachedToRoot(FocusedWidget) ||
            !FocusedWidget.Visible ||
            !FocusedWidget.Enabled ||
            !FocusedWidget.Focusable)
        {
            ClearFocus();
        }
    }

    private void Move(
        int direction)
    {
        ValidateFocus();

        if (_root is null)
        {
            return;
        }

        var widgets =
            new List<UiWidget>();

        CollectFocusable(
            _root,
            widgets);

        if (widgets.Count == 0)
        {
            return;
        }

        if (FocusedWidget is null)
        {
            SetFocus(
                direction > 0
                    ? widgets[0]
                    : widgets[^1]);

            return;
        }

        var currentIndex =
            widgets.IndexOf(
                FocusedWidget);

        if (currentIndex < 0)
        {
            SetFocus(widgets[0]);
            return;
        }

        var nextIndex =
            currentIndex + direction;

        if (nextIndex < 0)
        {
            nextIndex =
                widgets.Count - 1;
        }
        else if (nextIndex >= widgets.Count)
        {
            nextIndex = 0;
        }

        SetFocus(
            widgets[nextIndex]);
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

    private static void CollectFocusable(
        UiWidget widget,
        List<UiWidget> result)
    {
        if (!widget.Visible ||
            !widget.Enabled)
        {
            return;
        }

        if (widget.Focusable)
        {
            result.Add(widget);
        }

        if (widget is not UiContainer container)
        {
            return;
        }

        foreach (var child in container.Children)
        {
            CollectFocusable(
                child,
                result);
        }
    }
}