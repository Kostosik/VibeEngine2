using Engine.Core.Math;
using Engine.UI.Actions;
using Engine.UI.Assets;
using Engine.UI.Controls;
using Engine.UI.Layout;

namespace Engine.Tests.UI.Assets;

public sealed class UiAssetLoaderTests
{

    [Fact]
    public void Load_RestoresRuntimeControlSettings()
    {
        var rootId =
            Guid.NewGuid();

        var dropdownId =
            Guid.NewGuid();

        var toggleId =
            Guid.NewGuid();

        var textBoxId =
            Guid.NewGuid();

        var asset =
            new UiAsset(
                "Controls",
                new Vector2(
                    1280.0f,
                    720.0f),
                new[]
                {
                new UiAssetElement(
                    rootId,
                    Guid.Empty,
                    UiAssetElementType.Root,
                    "Root",
                    new UiAssetLayout(
                        UiAnchor.TopLeft,
                        Vector2.Zero,
                        new Vector2(
                            1280.0f,
                            720.0f))),

                new UiAssetElement(
                    dropdownId,
                    rootId,
                    UiAssetElementType.Dropdown,
                    "Quality",
                    new UiAssetLayout(
                        UiAnchor.TopLeft,
                        Vector2.Zero,
                        new Vector2(
                            220.0f,
                            42.0f)),
                    dropdownOptions:
                    new[]
                    {
                        "Low",
                        "Medium",
                        "High"
                    },
                    selectedIndex: 2),

                new UiAssetElement(
                    toggleId,
                    rootId,
                    UiAssetElementType.Toggle,
                    "VSync",
                    new UiAssetLayout(
                        UiAnchor.TopLeft,
                        Vector2.Zero,
                        new Vector2(
                            220.0f,
                            36.0f)),
                    text: "VSync",
                    toggleValue: true),

                new UiAssetElement(
                    textBoxId,
                    rootId,
                    UiAssetElementType.TextBox,
                    "Name",
                    new UiAssetLayout(
                        UiAnchor.TopLeft,
                        Vector2.Zero,
                        new Vector2(
                            240.0f,
                            42.0f)),
                    text: "Player",
                    placeholder: "Enter name",
                    maxLength: 64)
                });

        var instance =
            new UiAssetLoader().Load(
                asset);

        var dropdown =
            Assert.IsType<UiDropdown>(
                instance.Get(
                    dropdownId));

        Assert.Equal(
            2,
            dropdown.SelectedIndex);

        Assert.Equal(
            "High",
            dropdown.SelectedText);

        var toggle =
            Assert.IsType<UiToggle>(
                instance.Get(
                    toggleId));

        Assert.True(
            toggle.IsOn);

        var textBox =
            Assert.IsType<UiTextBox>(
                instance.Get(
                    textBoxId));

        Assert.Equal(
            "Player",
            textBox.Text);

        Assert.Equal(
            "Enter name",
            textBox.Placeholder);

        Assert.Equal(
            64,
            textBox.MaxLength);
    }

    [Fact]
    public void UiAnchor_RejectsNaN()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new UiAnchor(
                    float.NaN,
                    0.0f));
    }

    [Fact]
    public void UiAssetLayout_RejectsNonFiniteSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new UiAssetLayout(
                    UiAnchor.TopLeft,
                    Vector2.Zero,
                    new Vector2(
                        float.PositiveInfinity,
                        100.0f)));
    }

    [Fact]
    public void Asset_RejectsHierarchyCycle()
    {
        var rootId =
            Guid.NewGuid();

        var firstId =
            Guid.NewGuid();

        var secondId =
            Guid.NewGuid();

        Assert.Throws<InvalidDataException>(
            () =>
                new UiAsset(
                    "Invalid",
                    new Vector2(
                        1280.0f,
                        720.0f),
                    new[]
                    {
                    new UiAssetElement(
                        rootId,
                        Guid.Empty,
                        UiAssetElementType.Root,
                        "Root",
                        new UiAssetLayout(
                            UiAnchor.TopLeft,
                            Vector2.Zero,
                            new Vector2(
                                1280.0f,
                                720.0f))),

                    new UiAssetElement(
                        firstId,
                        secondId,
                        UiAssetElementType.Panel,
                        "First",
                        new UiAssetLayout(
                            UiAnchor.TopLeft,
                            Vector2.Zero,
                            new Vector2(
                                100.0f,
                                100.0f))),

                    new UiAssetElement(
                        secondId,
                        firstId,
                        UiAssetElementType.Panel,
                        "Second",
                        new UiAssetLayout(
                            UiAnchor.TopLeft,
                            Vector2.Zero,
                            new Vector2(
                                100.0f,
                                100.0f)))
                    }));
    }

    [Fact]
    public void Load_ButtonAction_InvokesRegisteredAction()
    {
        var rootId =
            Guid.NewGuid();

        var buttonId =
            Guid.NewGuid();

        var invoked =
            false;

        var actions =
            new UiActionRegistry();

        actions.Register(
            "StartGame",
            () => invoked = true);

        var asset =
            new UiAsset(
                "MainMenu",
                new Vector2(
                    1280.0f,
                    720.0f),
                new[]
                {
                new UiAssetElement(
                    rootId,
                    Guid.Empty,
                    UiAssetElementType.Root,
                    "Root",
                    new UiAssetLayout(
                        UiAnchor.TopLeft,
                        Vector2.Zero,
                        new Vector2(
                            1280.0f,
                            720.0f))),

                new UiAssetElement(
                    buttonId,
                    rootId,
                    UiAssetElementType.Button,
                    "StartButton",
                    new UiAssetLayout(
                        UiAnchor.Center,
                        Vector2.Zero,
                        new Vector2(
                            240.0f,
                            60.0f)),
                    text: "Start",
                    action: "StartGame")
                });

        var instance =
            new UiAssetLoader(
                actions: actions)
                .Load(
                    asset);

        var button =
            Assert.IsType<UiButton>(
                instance.Get(
                    buttonId));

        button.Click();

        Assert.True(
            invoked);
    }

    [Fact]
    public void Load_BuildsRuntimeHierarchy_RegardlessOfElementOrder()
    {
        var rootId =
            Guid.NewGuid();

        var panelId =
            Guid.NewGuid();

        var buttonId =
            Guid.NewGuid();

        var asset =
            new UiAsset(
                "MainMenu",
                new Vector2(
                    1280.0f,
                    720.0f),
                new[]
                {
                    new UiAssetElement(
                        rootId,
                        Guid.Empty,
                        UiAssetElementType.Root,
                        "Root",
                        new UiAssetLayout(
                            UiAnchor.TopLeft,
                            Vector2.Zero,
                            new Vector2(
                                1280.0f,
                                720.0f))),

                    // Intentionally before its parent.
                    new UiAssetElement(
                        buttonId,
                        panelId,
                        UiAssetElementType.Button,
                        "StartButton",
                        new UiAssetLayout(
                            UiAnchor.BottomCenter,
                            new Vector2(
                                0.0f,
                                -30.0f),
                            new Vector2(
                                240.0f,
                                60.0f)),
                        text: "Start"),

                    new UiAssetElement(
                        panelId,
                        rootId,
                        UiAssetElementType.Panel,
                        "MenuPanel",
                        new UiAssetLayout(
                            UiAnchor.Center,
                            Vector2.Zero,
                            new Vector2(
                                500.0f,
                                300.0f)))
                });

        var instance =
            new UiAssetLoader().Load(
                asset);

        var root =
            instance.Root;

        Assert.Single(
            root.Children);

        var panel =
            Assert.IsType<UiPanel>(
                root.Children[0]);

        Assert.Single(
            panel.Children);

        var button =
            Assert.IsType<UiButton>(
                panel.Children[0]);

        Assert.Equal(
            "Start",
            button.Text);

        Assert.Equal(
            UiLayoutMode.Anchor,
            panel.LayoutMode);

        Assert.Equal(
            UiAnchor.Center,
            panel.Anchor);

        Assert.Equal(
            new Vector2(
                500.0f,
                300.0f),
            new Vector2(
                panel.Width!.Value,
                panel.Height!.Value));

        Assert.Equal(
            UiAnchor.BottomCenter,
            button.Anchor);

        Assert.Equal(
            new Vector2(
                0.0f,
                -30.0f),
            button.Offset);

        Assert.Equal(
            new Vector2(
                240.0f,
                60.0f),
            new Vector2(
                button.Width!.Value,
                button.Height!.Value));

        Assert.Same(
            panel,
            instance.Get(panelId));

        Assert.Same(
            button,
            instance.Get(buttonId));
    }
}