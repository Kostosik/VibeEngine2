using Engine.UI.Core;

namespace Engine.UI.Input;

public sealed class UiFocusManager
{
    private readonly List<FocusScopeEntry> _scopes = new();

    private UiRoot? _root;

    public UiWidget? FocusedWidget { get; private set; }

    public UiWidget? ActiveScope =>
        _scopes.Count > 0
            ? _scopes[^1].Scope
            : null;

    internal void SetRoot(
        UiRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);

        _root =
            root;

        root.FocusManager =
            this;

        ValidateFocus();
    }



    public void SetFocus(
        UiWidget? widget)
    {
        if (widget is null)
        {
            if (FocusedWidget is not null)
            {
                FocusedWidget.IsFocused =
                    false;
            }

            FocusedWidget =
                null;

            return;
        }

        if (!IsAttachedToRoot(widget) ||
            !IsEffectivelyAvailable(widget) ||
            !widget.Focusable ||
            !IsInsideActiveScope(widget))
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
            FocusedWidget.IsFocused =
                false;
        }

        FocusedWidget =
            widget;

        FocusedWidget.IsFocused =
            true;
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

    public void EnterScope(
        UiWidget scope,
        UiWidget? initialFocus = null)
    {
        ArgumentNullException.ThrowIfNull(
            scope);

        if (!IsAttachedToRoot(scope))
        {
            throw new InvalidOperationException(
                "The focus scope must be attached to the UI root.");
        }

        if (!IsEffectivelyAvailable(scope))
        {
            throw new InvalidOperationException(
                "The focus scope must be visible and enabled.");
        }

        if (_scopes.Count > 0 &&
            ReferenceEquals(
                _scopes[^1].Scope,
                scope))
        {
            if (initialFocus is not null)
            {
                SetFocus(
                    initialFocus);
            }
            else if (FocusedWidget is null)
            {
                SetFocus(
                    FindFirstFocusable(scope));
            }

            return;
        }

        _scopes.Add(
            new FocusScopeEntry(
                scope,
                FocusedWidget));

        var target =
            initialFocus ??
            FindFirstFocusable(scope);

        SetFocus(
            target);
    }

    public void ExitScope(
        UiWidget scope)
    {
        ArgumentNullException.ThrowIfNull(
            scope);

        var index =
            FindScopeIndex(scope);

        if (index < 0)
        {
            return;
        }

        var entry =
            _scopes[index];

        _scopes.RemoveAt(
            index);

        if (index !=
            _scopes.Count)
        {
            return;
        }

        if (entry.PreviousFocus is not null &&
            IsAttachedToRoot(entry.PreviousFocus) &&
            IsEffectivelyAvailable(entry.PreviousFocus) &&
            entry.PreviousFocus.Focusable &&
            IsInsideActiveScope(entry.PreviousFocus))
        {
            SetFocus(
                entry.PreviousFocus);

            return;
        }

        ClearFocus();

        if (ActiveScope is not null)
        {
            SetFocus(
                FindFirstFocusable(
                    ActiveScope));
        }
    }

    public void ValidateFocus()
    {
        PruneUnavailableScopes();

        if (FocusedWidget is null)
        {
            return;
        }

        if (!IsAttachedToRoot(FocusedWidget) ||
            !IsEffectivelyAvailable(FocusedWidget) ||
            !FocusedWidget.Focusable ||
            !IsInsideActiveScope(FocusedWidget))
        {
            ClearFocus();

            if (ActiveScope is not null)
            {
                SetFocus(
                    FindFirstFocusable(
                        ActiveScope));
            }
        }
    }

    internal UiWidget? FindFirstFocusable(
        UiWidget scope)
    {
        if (!IsAttachedToRoot(scope) ||
            !IsEffectivelyAvailable(scope))
        {
            return null;
        }

        if (scope is not UiContainer container)
        {
            return null;
        }

        return FindFirstFocusable(
            container);
    }

    private UiWidget? FindFirstFocusable(
        UiContainer container)
    {
        foreach (var child in container.Children)
        {
            if (!child.Visible ||
                !child.Enabled)
            {
                continue;
            }

            if (child.Focusable)
            {
                return child;
            }

            if (child is UiContainer childContainer)
            {
                var nested =
                    FindFirstFocusable(
                        childContainer);

                if (nested is not null)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private void Move(
        int direction)
    {
        ValidateFocus();

        if (_root is null)
        {
            return;
        }

        var scope =
            ActiveScope;

        var widgets =
            new List<UiWidget>();

        if (scope is null)
        {
            CollectFocusable(
                _root,
                widgets,
                includeSelf: true);
        }
        else
        {
            CollectFocusable(
                scope,
                widgets,
                includeSelf: false);
        }

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
            SetFocus(
                direction > 0
                    ? widgets[0]
                    : widgets[^1]);

            return;
        }

        var nextIndex =
            currentIndex +
            direction;

        if (nextIndex < 0)
        {
            nextIndex =
                widgets.Count - 1;
        }
        else if (nextIndex >= widgets.Count)
        {
            nextIndex =
                0;
        }

        SetFocus(
            widgets[nextIndex]);
    }

    private void CollectFocusable(
        UiWidget widget,
        List<UiWidget> result,
        bool includeSelf)
    {
        if (!widget.Visible ||
            !widget.Enabled)
        {
            return;
        }

        if (includeSelf &&
            widget.Focusable)
        {
            result.Add(
                widget);
        }

        if (widget is not UiContainer container)
        {
            return;
        }

        foreach (var child in container.Children)
        {
            CollectFocusable(
                child,
                result,
                includeSelf: true);
        }
    }

    private bool IsInsideActiveScope(
        UiWidget widget)
    {
        if (ActiveScope is null)
        {
            return true;
        }

        var current =
            widget;

        while (current is not null)
        {
            if (ReferenceEquals(
                    current,
                    ActiveScope))
            {
                return true;
            }

            current =
                current.Parent;
        }

        return false;
    }

    internal bool IsAttachedToRoot(
        UiWidget widget)
    {
        ArgumentNullException.ThrowIfNull(
            widget);

        return IsAttachedToRootCore(widget);
    }

    private bool IsAttachedToRootCore(
        UiWidget widget)
    {
        var current =
            widget;

        while (current is not null)
        {
            if (ReferenceEquals(
                    current,
                    _root))
            {
                return true;
            }

            current =
                current.Parent;
        }

        return false;
    }

    private static bool IsEffectivelyAvailable(
        UiWidget widget)
    {
        var current =
            widget;

        while (current is not null)
        {
            if (!current.Visible ||
                !current.Enabled)
            {
                return false;
            }

            current =
                current.Parent;
        }

        return true;
    }

    private int FindScopeIndex(
        UiWidget scope)
    {
        for (var i =
                 _scopes.Count - 1;
             i >= 0;
             i--)
        {
            if (ReferenceEquals(
                    _scopes[i].Scope,
                    scope))
            {
                return i;
            }
        }

        return -1;
    }

    private void PruneUnavailableScopes()
    {
        while (_scopes.Count > 0)
        {
            var entry =
                _scopes[^1];

            if (IsAttachedToRoot(entry.Scope) &&
                IsEffectivelyAvailable(entry.Scope))
            {
                return;
            }

            _scopes.RemoveAt(
                _scopes.Count - 1);

            if (FocusedWidget is not null &&
                !IsInsideActiveScope(
                    FocusedWidget))
            {
                ClearFocus();
            }

            if (FocusedWidget is null &&
                entry.PreviousFocus is not null &&
                IsAttachedToRoot(entry.PreviousFocus) &&
                IsEffectivelyAvailable(entry.PreviousFocus) &&
                entry.PreviousFocus.Focusable &&
                IsInsideActiveScope(entry.PreviousFocus))
            {
                SetFocus(
                    entry.PreviousFocus);
            }
        }
    }

    private sealed class FocusScopeEntry
    {
        public FocusScopeEntry(
            UiWidget scope,
            UiWidget? previousFocus)
        {
            Scope =
                scope;

            PreviousFocus =
                previousFocus;
        }

        public UiWidget Scope { get; }

        public UiWidget? PreviousFocus { get; }
    }
}