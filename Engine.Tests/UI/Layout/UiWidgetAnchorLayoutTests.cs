using Engine.Core.Math;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Tests.UI.Layout;

public sealed class UiWidgetAnchorLayoutTests
{
    [Fact]
    public void Arrange_CenterAnchor_CentersWidget()
    {
        var widget =
            new TestWidget
            {
                LayoutMode =
                    UiLayoutMode.Anchor,

                Anchor =
                    UiAnchor.Center,

                Width = 200.0f,
                Height = 40.0f
            };

        widget.Arrange(
            new UiRect(
                100.0f,
                50.0f,
                1280.0f,
                720.0f));

        Assert.Equal(
            new UiRect(
                640.0f,
                390.0f,
                200.0f,
                40.0f),
            widget.Bounds);
    }

    [Fact]
    public void Arrange_BottomRightAnchor_UsesOffset()
    {
        var widget =
            new TestWidget
            {
                LayoutMode =
                    UiLayoutMode.Anchor,

                Anchor =
                    UiAnchor.BottomRight,

                Offset =
                    new Vector2(
                        -20.0f,
                        -20.0f),

                Width = 200.0f,
                Height = 40.0f
            };

        widget.Arrange(
            new UiRect(
                100.0f,
                50.0f,
                1280.0f,
                720.0f));

        Assert.Equal(
            new UiRect(
                1160.0f,
                710.0f,
                200.0f,
                40.0f),
            widget.Bounds);
    }

    [Fact]
    public void Arrange_DefaultAlignment_RemainsUnchanged()
    {
        var widget =
            new TestWidget
            {
                Width = 200.0f,
                Height = 40.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Center,

                VerticalAlignment =
                    UiVerticalAlignment.Center
            };

        widget.Arrange(
            new UiRect(
                100.0f,
                50.0f,
                1280.0f,
                720.0f));

        Assert.Equal(
            new UiRect(
                640.0f,
                390.0f,
                200.0f,
                40.0f),
            widget.Bounds);
    }

    private sealed class TestWidget : UiWidget
    {
    }
}