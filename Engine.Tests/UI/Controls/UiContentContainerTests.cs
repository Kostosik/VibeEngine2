using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Tests.UI.Controls;

public sealed class UiContentContainerTests
{
    [Fact]
    public void ScrollView_SetOwnContent_ThrowsWithoutLosingExistingContent()
    {
        var scrollView =
            new UiScrollView();

        var content =
            new UiPanel();

        scrollView.SetContent(
            content);

        Assert.Throws<InvalidOperationException>(
            () =>
                scrollView.SetContent(
                    scrollView));

        Assert.Same(
            content,
            scrollView.Content);

        Assert.Same(
            scrollView,
            content.Parent);
    }

    [Fact]
    public void Window_SetOwnContent_ThrowsWithoutLosingExistingContent()
    {
        var overlays =
            new UiOverlayLayer();

        var window =
            new UiWindow(
                overlays,
                "Test");

        var content =
            new UiPanel();

        window.SetContent(
            content);

        Assert.Throws<InvalidOperationException>(
            () =>
                window.SetContent(
                    window));

        Assert.Same(
            content,
            window.Content);

        Assert.Same(
            window.Content,
            content);
    }

    [Fact]
    public void Modal_SetOwnContent_ThrowsWithoutLosingExistingContent()
    {
        var overlays =
            new UiOverlayLayer();

        var focus =
            new UiFocusManager();

        var modal =
            new UiModal(
                overlays,
                focus);

        var content =
            new UiPanel();

        modal.SetContent(
            content);

        Assert.Throws<InvalidOperationException>(
            () =>
                modal.SetContent(
                    modal));

        Assert.Same(
            content,
            modal.ContentHost.Children[0]);

        Assert.Same(
            modal.ContentHost,
            content.Parent);
    }
}