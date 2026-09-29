using Engine.Core.Math;
using Engine.Input;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Tests.UI;

public sealed class UiFocusScopeTests
{
    [Fact]
    public void EnterScope_FocusesFirstFocusableWidget()
    {
        var root =
            new UiRoot();

        var first =
            new TestWidget
            {
                Focusable = true
            };

        var second =
            new TestWidget
            {
                Focusable = true
            };

        var scope =
            new UiPanel();

        scope.AddChild(first);
        scope.AddChild(second);

        root.AddChild(scope);

        var focus =
            CreateFocus(root);

        focus.EnterScope(
            scope);

        Assert.Same(
            first,
            focus.FocusedWidget);

        Assert.Same(
            scope,
            focus.ActiveScope);
    }

    [Fact]
    public void MoveNext_StaysInsideActiveScope()
    {
        var root =
            new UiRoot();

        var outside =
            new TestWidget
            {
                Focusable = true
            };

        var scope =
            new UiPanel();

        var first =
            new TestWidget
            {
                Focusable = true
            };

        var second =
            new TestWidget
            {
                Focusable = true
            };

        root.AddChild(outside);
        root.AddChild(scope);

        scope.AddChild(first);
        scope.AddChild(second);

        var focus =
            CreateFocus(root);

        focus.SetFocus(
            outside);

        focus.EnterScope(
            scope);

        Assert.Same(
            first,
            focus.FocusedWidget);

        focus.MoveNext();

        Assert.Same(
            second,
            focus.FocusedWidget);

        focus.MoveNext();

        Assert.Same(
            first,
            focus.FocusedWidget);
    }

    [Fact]
    public void ExitScope_RestoresPreviousFocus()
    {
        var root =
            new UiRoot();

        var outside =
            new TestWidget
            {
                Focusable = true
            };

        var scope =
            new UiPanel();

        var inside =
            new TestWidget
            {
                Focusable = true
            };

        root.AddChild(outside);
        root.AddChild(scope);
        scope.AddChild(inside);

        var focus =
            CreateFocus(root);

        focus.SetFocus(
            outside);

        focus.EnterScope(
            scope);

        Assert.Same(
            inside,
            focus.FocusedWidget);

        focus.ExitScope(
            scope);

        Assert.Same(
            outside,
            focus.FocusedWidget);

        Assert.Null(
            focus.ActiveScope);
    }

    [Fact]
    public void NestedScopes_RestoreOuterFocus()
    {
        var root =
            new UiRoot();

        var outer =
            new UiPanel();

        var outerWidget =
            new TestWidget
            {
                Focusable = true
            };

        var inner =
            new UiPanel();

        var innerWidget =
            new TestWidget
            {
                Focusable = true
            };

        root.AddChild(
            outer);

        outer.AddChild(
            outerWidget);

        outer.AddChild(
            inner);

        inner.AddChild(
            innerWidget);

        var focus =
            CreateFocus(root);

        focus.EnterScope(
            outer);

        Assert.Same(
            outerWidget,
            focus.FocusedWidget);

        focus.EnterScope(
            inner);

        Assert.Same(
            innerWidget,
            focus.FocusedWidget);

        Assert.Same(
            inner,
            focus.ActiveScope);

        focus.ExitScope(
            inner);

        Assert.Same(
            outerWidget,
            focus.FocusedWidget);

        Assert.Same(
            outer,
            focus.ActiveScope);

        focus.ExitScope(
            outer);

        Assert.Null(
            focus.FocusedWidget);

        Assert.Null(
            focus.ActiveScope);
    }

    private static UiFocusManager CreateFocus(
        UiRoot root)
    {
        return new UiFocusManagerProxy(
            root).Focus;
    }

    private sealed class UiFocusManagerProxy
    {
        public UiFocusManagerProxy(
            UiRoot root)
        {
            Focus =
                new UiFocusManager();

            _ =
                new UiInputRouter(
                    root,
                    new EmptyPointerInput(),
                    new EmptyTextInput(),
                    Focus);
        }

        public UiFocusManager Focus { get; }
    }

    private sealed class TestWidget :
        UiWidget
    {
    }

    private sealed class EmptyPointerInput :
        IPointerInput
    {
        public Vector2 Position =>
            Vector2.Zero;

        public float ScrollDelta =>
            0.0f;

        public bool IsDown(
            InputMouseButton button) =>
            false;

        public bool IsPressed(
            InputMouseButton button) =>
            false;

        public bool IsReleased(
            InputMouseButton button) =>
            false;
    }

    private sealed class EmptyTextInput :
        ITextInput
    {
        public IReadOnlyList<char> Characters =>
            Array.Empty<char>();

        public bool IsPressed(
            TextInputKey key) =>
            false;
    }
}