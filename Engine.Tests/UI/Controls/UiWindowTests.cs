using Engine.Core.Math;
using Engine.UI.Controls;
using Engine.UI.Core;

namespace Engine.Tests.UI.Controls;

public sealed class UiWindowTests
{
    [Fact]
    public void Show_OpensWindow()
    {
        var overlays =
            new UiOverlayLayer();

        var window =
            new UiWindow(
                overlays,
                "Test");

        window.Show(
            new Vector2(
                20.0f,
                30.0f));

        Assert.True(
            window.IsOpen);

        Assert.True(
            window.Visible);

        Assert.Contains(
            window,
            overlays.Children);
    }

    [Fact]
    public void Close_HidesAndDetachesWindow()
    {
        var overlays =
            new UiOverlayLayer();

        var window =
            new UiWindow(
                overlays,
                "Test");

        window.Show(
            new Vector2(
                20.0f,
                30.0f));

        window.Close();

        Assert.False(
            window.IsOpen);

        Assert.False(
            window.Visible);

        Assert.Null(
            window.Parent);

        Assert.DoesNotContain(
            window,
            overlays.Children);
    }

    [Fact]
    public void Close_WhenAlreadyClosed_DoesNotRaiseClosedAgain()
    {
        var overlays =
            new UiOverlayLayer();

        var window =
            new UiWindow(
                overlays,
                "Test");

        var closeCount = 0;

        window.Closed +=
            () => closeCount++;

        window.Show(
            Vector2.Zero);

        window.Close();
        window.Close();

        Assert.Equal(
            1,
            closeCount);
    }

    [Fact]
    public void Show_WhenAlreadyOpen_MovesWindowWithoutReattaching()
    {
        var overlays =
            new UiOverlayLayer();

        var window =
            new UiWindow(
                overlays,
                "Test");

        window.Show(
            new Vector2(
                10.0f,
                20.0f));

        window.Show(
            new Vector2(
                40.0f,
                50.0f));

        Assert.True(
            window.IsOpen);

        Assert.Single(
            overlays.Children);

        Assert.Same(
            window,
            overlays.Children[0]);

        Assert.Equal(
            new Vector2(
                40.0f,
                50.0f),
            overlays.GetPosition(
                window));
    }
}