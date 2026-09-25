using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;
using Engine.UI.Screens;

namespace Game.Sandbox.UI;

public sealed class PauseScreen : UiScreen
{
    private readonly UiButton _resumeButton;
    private readonly UiButton _settingsButton;
    private readonly UiOverlayLayer _overlays;
    public PauseScreen(
        UiOverlayLayer overlays,
        Action onResume,
        Action onSettings)
            : base(CreateRoot(
            out var resumeButton,
            out var settingsButton,
            onResume,
            onSettings))
    {
        _resumeButton =
            resumeButton;
        _overlays =
            overlays;
        _settingsButton =
            settingsButton;
    }

    public override void OnEnter(
        UiFocusManager focus)
    {
        focus.SetFocus(
            _resumeButton);
    }

    private static UiWidget CreateRoot(
        out UiButton resumeButton,
        out UiButton settingsButton,
        Action onResume,
        Action onSettings)
    {
        var background =
            new UiPanel
            {
                Background =
                    new UiColor(
                        0,
                        0,
                        0,
                        180),

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Stretch
            };

        var panel =
            new UiPanel
            {
                Width = 320.0f,
                Height = 240.0f,

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

                Spacing = 12.0f
            };

        var title =
            new UiLabel("PAUSED")
            {
                FontSize = 24.0f
            };

        resumeButton =
            new UiButton("RESUME");

        settingsButton =
            new UiButton("SETTINGS");

        resumeButton.Clicked +=
            onResume;

        settingsButton.Clicked +=
            onSettings;

        stack.AddChild(title);
        stack.AddChild(resumeButton);
        stack.AddChild(settingsButton);

        panel.AddChild(stack);
        background.AddChild(panel);

        return background;
    }
}