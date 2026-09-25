using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Game.Sandbox.UI;

public sealed class UiShowcaseWindow
{
    private readonly UiSystem _ui;
    private readonly UiWindow _window;

    private readonly UiModal _modal;
    private readonly UiContextMenu _contextMenu;

    public UiShowcaseWindow(
        UiSystem ui,
        TextureHandle imageTexture)
    {
        ArgumentNullException.ThrowIfNull(ui);

        _ui = ui;

        _window =
            new UiWindow(
                _ui.Overlays,
                "UI SHOWCASE");

        _modal =
            CreateModal();

        _contextMenu =
            CreateContextMenu();

        _window.SetContent(
            BuildContent(
                imageTexture));
    }

    public void Show(
        Vector2 position)
    {
        _window.Show(
            position);
    }

    public void Close()
    {
        _modal.Close();
        _contextMenu.Close();

        _window.Close();
    }

    private UiWidget BuildContent(
        TextureHandle imageTexture)
    {
        var scrollView =
           new UiScrollView
           {
               HorizontalScrolling = false,
               VerticalScrolling = true,

               ScrollSpeed = 32.0f,

               HorizontalAlignment =
                   UiHorizontalAlignment.Stretch,

               VerticalAlignment =
                   UiVerticalAlignment.Stretch
           };

        var content =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical,

                Spacing = 10.0f
            };

        scrollView.SetContent(
            content);

        BuildHeader(
            content);

        BuildButtonSection(
            content);

        BuildImageSection(
            content,
            imageTexture);

        BuildProgressSection(
            content);

        BuildTextBoxSection(
            content);

        BuildSliderSection(
            content);

        BuildToggleSection(
            content);

        BuildDropdownSection(
            content);

        BuildListSection(
            content);

        BuildTooltipSection(
            content);

        BuildModalSection(
            content);

        BuildContextMenuSection(
            content);

        BuildFooter(
            content);

        return scrollView;
    }

    private static void BuildHeader(
        UiStackPanel content)
    {
        content.AddChild(
            new UiLabel("UI SHOWCASE")
            {
                FontSize = 20.0f
            });

        content.AddChild(
            new UiLabel(
                "Тестовый полигон базовых UI-контролов.")
            {
                FontSize = 13.0f
            });

        content.AddChild(
            new UiSeparator());

        content.AddChild(
            new UiSpacer
            {
                Height = 4.0f
            });
    }

    private static void BuildButtonSection(
        UiStackPanel content)
    {
        content.AddChild(
            new UiLabel("BUTTON")
            {
                FontSize = 14.0f
            });

        var button =
            new UiButton("TEST BUTTON")
            {
                Width = 180.0f,
                Height = 38.0f,
                FontSize = 14.0f
            };

        button.Clicked +=
            () =>
            {
                button.Text =
                    button.Text == "TEST BUTTON"
                        ? "CLICKED!"
                        : "TEST BUTTON";
            };

        content.AddChild(
            button);
    }

    private static void BuildImageSection(
        UiStackPanel content,
        TextureHandle imageTexture)
    {
        content.AddChild(
            new UiLabel("IMAGE")
            {
                FontSize = 14.0f
            });

        content.AddChild(
            new UiImage(
                imageTexture,
                new Vector2(
                    64.0f,
                    64.0f))
            {
                PreserveAspectRatio = true
            });
    }

    private static void BuildProgressSection(
        UiStackPanel content)
    {
        content.AddChild(
            new UiLabel("PROGRESS BAR")
            {
                FontSize = 14.0f
            });

        content.AddChild(
            new UiProgressBar
            {
                Width = 320.0f,
                Height = 18.0f,
                Value = 0.65f
            });
    }

    private static void BuildTextBoxSection(
        UiStackPanel content)
    {
        content.AddChild(
            new UiLabel("TEXT BOX")
            {
                FontSize = 14.0f
            });

        var result =
            new UiLabel("Text: ")
            {
                FontSize = 12.0f
            };

        var textBox =
            new UiTextBox
            {
                Width = 320.0f,
                Height = 42.0f,
                Placeholder = "Введите текст..."
            };

        textBox.TextChanged +=
            text =>
            {
                result.Text =
                    $"Text: {text}";
            };

        content.AddChild(
            textBox);

        content.AddChild(
            result);
    }

    private static void BuildSliderSection(
        UiStackPanel content)
    {
        content.AddChild(
            new UiLabel("SLIDER")
            {
                FontSize = 14.0f
            });

        var progress =
            new UiProgressBar
            {
                Width = 320.0f,
                Height = 18.0f,
                Value = 0.5f
            };

        var valueLabel =
            new UiLabel("Value: 0.50")
            {
                FontSize = 12.0f
            };

        var slider =
            new UiSlider
            {
                Width = 320.0f,
                Value = 0.5f
            };

        slider.ValueChanged +=
            value =>
            {
                valueLabel.Text =
                    $"Value: {value:0.00}";

                progress.Value =
                    value;
            };

        content.AddChild(
            slider);

        content.AddChild(
            valueLabel);

        content.AddChild(
            progress);
    }

    private static void BuildToggleSection(
        UiStackPanel content)
    {
        content.AddChild(
            new UiLabel("TOGGLE")
            {
                FontSize = 14.0f
            });

        var stateLabel =
            new UiLabel("Fullscreen: OFF")
            {
                FontSize = 12.0f
            };

        var toggle =
            new UiToggle("Fullscreen")
            {
                ShowFocusVisual = false
            };

        toggle.ValueChanged +=
            value =>
            {
                stateLabel.Text =
                    $"Fullscreen: {(value ? "ON" : "OFF")}";
            };

        content.AddChild(
            toggle);

        content.AddChild(
            stateLabel);
    }

    private void BuildDropdownSection(
        UiStackPanel content)
    {
        content.AddChild(
            new UiLabel("DROPDOWN")
            {
                FontSize = 14.0f
            });

        var stateLabel =
            new UiLabel("Selected: none")
            {
                FontSize = 12.0f
            };

        var dropdown =
            new UiDropdown(
                _ui.Overlays);

        dropdown.AddOption(
            "LOW");

        dropdown.AddOption(
            "MEDIUM");

        dropdown.AddOption(
            "HIGH");

        dropdown.AddOption(
            "ULTRA");

        dropdown.SelectionChanged +=
            (
                index,
                text) =>
            {
                stateLabel.Text =
                    $"Selected: {text}";
            };

        content.AddChild(
            dropdown);

        content.AddChild(
            stateLabel);
    }

    private static void BuildListSection(
        UiStackPanel content)
    {
        content.AddChild(
            new UiLabel("LIST + SCROLL VIEW")
            {
                FontSize = 14.0f
            });

        var list =
            new UiList
            {
                Width = 320.0f,
                Height = 120.0f,

                Background =
                    new UiColor(
                        30,
                        30,
                        30,
                        255)
            };

        for (var i = 1; i <= 10; i++)
        {
            var index =
                i;

            var button =
                new UiButton(
                    $"List Item {index}")
                {
                    Width = 300.0f,
                    Height = 32.0f,
                    FontSize = 13.0f
                };

            button.Clicked +=
                () =>
                {
                    button.Text =
                        $"Selected {index}";
                };

            list.AddItem(
                button);
        }

        content.AddChild(
            list);
    }

    private UiModal CreateModal()
    {
        var modal =
            new UiModal(
                _ui.Overlays,
                _ui.Focus);

        var content =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical,

                Spacing = 10.0f
            };

        content.AddChild(
            new UiLabel("Test Modal")
            {
                FontSize = 20.0f
            });

        content.AddChild(
            new UiLabel(
                "Модальное окно блокирует\n" +
                "взаимодействие с фоном.")
            {
                FontSize = 14.0f
            });

        var buttons =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Horizontal,

                Spacing = 8.0f
            };

        var cancel =
            new UiButton("CANCEL")
            {
                Width = 120.0f,
                Height = 38.0f,
                FontSize = 13.0f
            };

        var confirm =
            new UiButton("CONFIRM")
            {
                Width = 120.0f,
                Height = 38.0f,
                FontSize = 13.0f
            };

        cancel.Clicked +=
            modal.Close;

        confirm.Clicked +=
            modal.Close;

        buttons.AddChild(
            cancel);

        buttons.AddChild(
            confirm);

        content.AddChild(
            buttons);

        modal.SetContent(
            content);

        modal.SetSize(
            480.0f,
            260.0f);

        return modal;
    }

    private UiContextMenu CreateContextMenu()
    {
        var menu =
            new UiContextMenu(
                _ui.Overlays);

        menu.AddItem(
            "Action 1",
            () =>
            {
            });

        menu.AddItem(
            "Action 2",
            () =>
            {
            });

        menu.AddItem(
            "Reset",
            () =>
            {
            });

        return menu;
    }

    private void BuildTooltipSection(
        UiStackPanel content)
    {
        content.AddChild(
            new UiLabel("TOOLTIP")
            {
                FontSize = 14.0f
            });

        var button =
            new UiButton("HOVER ME")
            {
                Width = 180.0f,
                Height = 38.0f,
                FontSize = 14.0f
            };

        _ =
            new UiTooltip(
                _ui.Overlays,
                button,
                "Tooltip работает при наведении.");

        content.AddChild(
            button);
    }

    private void BuildModalSection(
        UiStackPanel content)
    {
        content.AddChild(
            new UiLabel("MODAL")
            {
                FontSize = 14.0f
            });

        var button =
            new UiButton("OPEN MODAL")
            {
                Width = 180.0f,
                Height = 38.0f,
                FontSize = 14.0f
            };

        button.Clicked +=
            () =>
            {
                _modal.Show();
            };

        content.AddChild(
            button);
    }

    private void BuildContextMenuSection(
        UiStackPanel content)
    {
        content.AddChild(
            new UiLabel("CONTEXT MENU")
            {
                FontSize = 14.0f
            });

        var button =
            new UiButton("OPEN CONTEXT MENU")
            {
                Width = 220.0f,
                Height = 38.0f,
                FontSize = 13.0f
            };

        button.Clicked +=
            () =>
            {
                _contextMenu.Open(
                    new Vector2(
                        button.Bounds.X,
                        button.Bounds.Bottom));
            };

        content.AddChild(
            button);
    }

    private static void BuildFooter(
        UiStackPanel content)
    {
        content.AddChild(
            new UiSeparator());

        content.AddChild(
            new UiSpacer
            {
                Height = 60.0f
            });
    }
}