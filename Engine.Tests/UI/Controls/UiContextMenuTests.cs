using Engine.Core.Math;
using Engine.UI.Controls;
using Engine.UI.Core;

namespace Engine.Tests.UI.Controls;

public sealed class UiContextMenuTests
{
    [Fact]
    public void ClearItems_WhenMenuIsOpen_ClosesMenu()
    {
        var overlay =
            new UiOverlayLayer();

        var menu =
            new UiContextMenu(
                overlay);

        menu.AddItem(
            "Test",
            static () => { });

        menu.Open(
            new Vector2(
                20.0f,
                20.0f));

        Assert.True(
            menu.IsOpen);

        menu.ClearItems();

        Assert.False(
            menu.IsOpen);
    }

    [Fact]
    public void Open_WhenMenuHasNoItems_DoesNotOpen()
    {
        var overlay =
            new UiOverlayLayer();

        var menu =
            new UiContextMenu(
                overlay);

        menu.Open(
            new Vector2(
                20.0f,
                20.0f));

        Assert.False(
            menu.IsOpen);
    }
}