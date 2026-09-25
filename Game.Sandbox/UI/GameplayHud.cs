using System.Globalization;
using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.UI.Controls;
using Engine.UI.Layout;

namespace Game.Sandbox.UI;

public sealed class GameplayHud : UiPanel
{
    private readonly UiLabel _positionLabel;
    private readonly UiProgressBar _healthBar;
    public event Action? TestWindowRequested;
    private readonly UiLabel _interactionLabel;
    public GameplayHud()
    {
        HorizontalAlignment =
            UiHorizontalAlignment.Stretch;

        VerticalAlignment =
            UiVerticalAlignment.Stretch;

        IsHitTestVisible = true;

        var infoPanel =
            new UiPanel
            {
                Width = 260.0f,
                Height = 110.0f,

                Padding =
                    new UiThickness(12.0f),

                Background =
                    new UiColor(
                        10,
                        10,
                        10,
                        190),

                HorizontalAlignment =
                    UiHorizontalAlignment.Left,

                VerticalAlignment =
                    UiVerticalAlignment.Top
            };

        var infoStack =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical
            };

        infoStack.AddChild(
            new UiLabel("HP")
            {
                FontSize = 14.0f
            });

        _healthBar =
            new UiProgressBar
            {
                Width = 220.0f,
                Height = 18.0f
            };

        infoStack.AddChild(
            _healthBar);

        infoStack.AddChild(
            new UiLabel("HP: 100"));

        _positionLabel =
            new UiLabel();

        _interactionLabel =
    new UiLabel
    {
        FontSize = 16.0f
    };

        infoStack.AddChild(
            _interactionLabel);
        _interactionLabel.Visible = false;
        infoStack.AddChild(
            _positionLabel);

        infoPanel.AddChild(
            infoStack);

        AddChild(
            infoPanel);

        var controlsPanel =
            new UiPanel
            {
                Width = 280.0f,
                Height = 55.0f,

                Padding =
                    new UiThickness(10.0f),

                Background =
                    new UiColor(
                        10,
                        10,
                        10,
                        170),

                HorizontalAlignment =
                    UiHorizontalAlignment.Left,

                VerticalAlignment =
                    UiVerticalAlignment.Bottom
            };

        controlsPanel.AddChild(
            new UiLabel(
                "WASD Move   Q/E Zoom   ESC Pause")
            {
                FontSize = 24f
            });

        AddChild(
            controlsPanel);

        var testWindowButton =
            new UiButton("UI WINDOW")
            {
                Width = 150.0f,
                Height = 38.0f,
                FontSize = 14.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Right,

                VerticalAlignment =
                    UiVerticalAlignment.Bottom,

                Margin =
                    new UiThickness(
                        0.0f,
                        0.0f,
                        12.0f,
                        12.0f)
            };
        testWindowButton.IsHitTestVisible = true;
        testWindowButton.Clicked +=
            () =>
            {
                TestWindowRequested?.Invoke();
            };

        AddChild(
            testWindowButton);
    }

    public void SetInteractionPrompt(
    string? text)
    {
        _interactionLabel.Text =
            text ?? string.Empty;

        _interactionLabel.Visible =
            !string.IsNullOrEmpty(text);
    }
    public void SetHealth(
    float value)
    {
        _healthBar.Value =
            value;
    }
    public void SetPlayerPosition(
        FixedVector2 position)
    {
        _positionLabel.Text =
            $"POS: {position.X.ToFloat().ToString("0.0", CultureInfo.InvariantCulture)}, " +
            $"{position.Y.ToFloat().ToString("0.0", CultureInfo.InvariantCulture)}";
    }
}