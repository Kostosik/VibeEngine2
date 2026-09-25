using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;
using Engine.UI.Screens;

namespace Game.Sandbox.UI;

public sealed class SettingsScreen : UiScreen
{
    private readonly UiButton _backButton;

    public SettingsScreen(
        UiOverlayLayer overlays,
        Action onBack)
        : base(CreateRoot(
            overlays,
            out var backButton,
            onBack))
    {
        _backButton =
            backButton;
    }

    public override void OnEnter(
        UiFocusManager focus)
    {
        focus.SetFocus(
            _backButton);
    }

    private static UiWidget CreateRoot(
        UiOverlayLayer overlays,
        out UiButton backButton,
        Action onBack)
    {
        var background =
            new UiPanel
            {
                Background =
                    new UiColor(
                        0,
                        0,
                        0,
                        190),

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Stretch
            };

        var panel =
            new UiPanel
            {
                Width = 420.0f,
                Height = 300.0f,

                Padding =
                    new UiThickness(20.0f),

                Background =
                    new UiColor(
                        25,
                        25,
                        25,
                        245),

                HorizontalAlignment =
                    UiHorizontalAlignment.Center,

                VerticalAlignment =
                    UiVerticalAlignment.Center
            };

        var stack =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical,

                Spacing = 8.0f
            };

        stack.AddChild(
            new UiLabel("SETTINGS")
            {
                FontSize = 24.0f
            });

        var qualityLabel =
            new UiLabel("QUALITY")
            {
                FontSize = 14.0f
            };

        stack.AddChild(
            qualityLabel);

        var quality =
            new UiDropdown(
                overlays);

        quality.AddOption("LOW");
        quality.AddOption("MEDIUM");
        quality.AddOption("HIGH");
        quality.AddOption("ULTRA");

        quality.SetSelectedIndex(2);

        stack.AddChild(
            quality);

        backButton =
            new UiButton("BACK")
            {
                Width = 220.0f,
                Height = 42.0f,
                FontSize = 16.0f
            };

        backButton.Clicked +=
            onBack;

        stack.AddChild(
            backButton);

        panel.AddChild(
            stack);

        background.AddChild(
            panel);

        return background;
    }
}