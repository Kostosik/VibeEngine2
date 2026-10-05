using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Game.Sandbox;

public sealed class SandboxHud :
    UiPanel
{
    private readonly UiLabel _mode;
    private readonly UiLabel _status;
    private readonly UiLabel _performance;
    private readonly UiLabel _interaction;
    public SandboxHud()
    {
        HorizontalAlignment =
            UiHorizontalAlignment.Stretch;

        VerticalAlignment =
            UiVerticalAlignment.Stretch;

        IsHitTestVisible = false;

        var panel =
            new UiPanel
            {
                Width = 390.0f,
                Height = 400.0f,
                Padding =
                    new UiThickness(12.0f),

                Background =
                    new UiColor(
                        8,
                        8,
                        8,
                        215),

                HorizontalAlignment =
                    UiHorizontalAlignment.Left,

                VerticalAlignment =
                    UiVerticalAlignment.Top,

                Margin =
                    new UiThickness(12.0f)
            };

        var stack =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical,

                Spacing = 5.0f
            };

        stack.AddChild(
            new UiLabel(
                "VIBEENGINE2 SANDBOX")
            {
                FontSize = 20.0f
            });

        _mode =
            new UiLabel()
            {
                FontSize = 16.0f
            };

        _performance = new UiLabel() { FontSize = 16f };

        _status =
            new UiLabel()
            {
                FontSize = 13.0f
            };

        _interaction =
    new UiLabel
    {
        FontSize = 16.0f
    };

        _interaction.Visible =
            false;

        stack.AddChild(
            _interaction);

        stack.AddChild(_mode);

        stack.AddChild(
            new UiSeparator());

        stack.AddChild(_status);

        stack.AddChild(
            new UiSeparator());

        stack.AddChild(
            new UiLabel(
                "F2  Switch scenario\n" +
                "F3  Reset\n" +
                "F4  Spawn stress batch\n" +
                "F5  Clear stress batch\n" +
                "SPACE  Force\n" +
                "T  Toggle trigger\n" +
                "R  Rotate obstacle\n" +
                "F1  Console")
            {
                FontSize = 13.0f
            });

        stack.AddChild(
    new UiSeparator());

        stack.AddChild(_performance);

        panel.AddChild(
            stack);



        AddChild(panel);
    }

    public void SetPerformance(
    string text)
    {
        _performance.Text =
            text;
    }

    public void SetInteraction(
    string? text)
    {
        _interaction.Text =
            text ?? string.Empty;

        _interaction.Visible =
            !string.IsNullOrEmpty(text);
    }

    public void SetMode(
        string mode)
    {
        _mode.Text =
            $"SCENARIO: {mode}";
    }

    public void SetStatus(
        string status)
    {
        _status.Text =
            status;
    }
}