using Engine.Core.Math;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;

namespace Engine.Tests.UI.Controls;

public sealed class UiModalTests
{
    [Fact]
    public void NewModal_IsClosedAndHidden()
    {
        var overlay =
            new UiOverlayLayer();

        var focus =
            new UiFocusManager();

        var modal =
            new UiModal(
                overlay,
                focus);

        Assert.False(
            modal.IsOpen);

        Assert.False(
            modal.Visible);

        Assert.Empty(
            overlay.Children);
    }

    [Fact]
    public void Show_OpensAndMakesModalVisible()
    {
        var overlay =
            new UiOverlayLayer();

        var focus =
            new UiFocusManager();

        var modal =
            new UiModal(
                overlay,
                focus);

        modal.Show();

        Assert.True(
            modal.IsOpen);

        Assert.True(
            modal.Visible);

        Assert.Same(
            modal,
            overlay.Children[0]);
    }

    [Fact]
    public void Close_HidesAndDetachesModal()
    {
        var overlay =
            new UiOverlayLayer();

        var focus =
            new UiFocusManager();

        var modal =
            new UiModal(
                overlay,
                focus);

        modal.Show();
        modal.Close();

        Assert.False(
            modal.IsOpen);

        Assert.False(
            modal.Visible);

        Assert.Null(
            modal.Parent);

        Assert.Empty(
            overlay.Children);
    }

    [Fact]
    public void Close_WhenAlreadyClosed_DoesNotRaiseClosedAgain()
    {
        var overlay =
            new UiOverlayLayer();

        var focus =
            new UiFocusManager();

        var modal =
            new UiModal(
                overlay,
                focus);

        var closeCount = 0;

        modal.Closed +=
            () => closeCount++;

        modal.Show();

        modal.Close();
        modal.Close();

        Assert.Equal(
            1,
            closeCount);
    }

    [Fact]
    public void Show_WhenAlreadyOpen_DoesNotChangeState()
    {
        var overlay =
            new UiOverlayLayer();

        var focus =
            new UiFocusManager();

        var modal =
            new UiModal(
                overlay,
                focus);

        modal.Show();

        modal.Show();

        Assert.True(
            modal.IsOpen);

        Assert.True(
            modal.Visible);

        Assert.Single(
            overlay.Children);

        Assert.Same(
            modal,
            overlay.Children[0]);
    }
}