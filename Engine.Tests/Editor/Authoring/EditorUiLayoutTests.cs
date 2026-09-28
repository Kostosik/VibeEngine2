using Engine.Core.Math;
using Engine.Editor.UI.Authoring;

namespace Engine.Tests.Editor.Authoring;

public sealed class EditorUiLayoutTests
{
    [Fact]
    public void Resolve_DefaultLayout_UsesTopLeftOrigin()
    {
        var layout =
            new EditorUiLayout(
                Vector2.Zero,
                new Vector2(10.0f, 20.0f),
                new Vector2(200.0f, 40.0f));

        var result =
            layout.Resolve(
                new Vector2(1280.0f, 720.0f));

        Assert.Equal(
            new Rectangle(
                10.0f,
                20.0f,
                200.0f,
                40.0f),
            result);
    }

    [Fact]
    public void Resolve_CenterAnchor_CentersElement()
    {
        var layout =
            new EditorUiLayout(
                new Vector2(0.5f, 0.5f),
                Vector2.Zero,
                new Vector2(200.0f, 40.0f));

        var result =
            layout.Resolve(
                new Vector2(1280.0f, 720.0f));

        Assert.Equal(
            new Rectangle(
                540.0f,
                340.0f,
                200.0f,
                40.0f),
            result);
    }

    [Fact]
    public void Resolve_BottomRightAnchor_WithNegativeOffset_StaysInsideParent()
    {
        var layout =
            new EditorUiLayout(
                new Vector2(1.0f, 1.0f),
                new Vector2(-20.0f, -20.0f),
                new Vector2(200.0f, 40.0f));

        var result =
            layout.Resolve(
                new Vector2(1280.0f, 720.0f));

        Assert.Equal(
            new Rectangle(
                1060.0f,
                660.0f,
                200.0f,
                40.0f),
            result);
    }

    [Fact]
    public void Constructor_InvalidAnchor_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new EditorUiLayout(
                    new Vector2(1.1f, 0.5f),
                    Vector2.Zero,
                    new Vector2(100.0f, 40.0f)));
    }

    [Fact]
    public void Constructor_NegativeSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new EditorUiLayout(
                    Vector2.Zero,
                    Vector2.Zero,
                    new Vector2(-100.0f, 40.0f)));
    }
}