using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.UI.Layout;

namespace Engine.UI.Styling;

public sealed class UiTheme
{

    public FontHandle DefaultFont { get; set; }
    public UiColor PanelBackground { get; init; } =
        new(20, 20, 20, 220);

    public float ButtonWidth { get; init; } = 180.0f;

    public float ButtonHeight { get; init; } = 42.0f;

    public float ButtonFontSize { get; init; } = 16.0f;

    public UiThickness ButtonPadding { get; init; } =
        new(12.0f, 8.0f, 12.0f, 8.0f);

    public UiColor ButtonNormal { get; init; } =
        new(50, 50, 50, 255);

    public UiColor ButtonHovered { get; init; } =
        new(70, 70, 70, 255);

    public UiColor ButtonPressed { get; init; } =
        new(90, 90, 90, 255);

    public UiColor ButtonDisabled { get; init; } =
        new(35, 35, 35, 180);

    public UiColor ButtonFocusedBorder { get; init; } =
        new(220, 220, 220, 255);

    public UiColor Text { get; init; } =
        UiColor.White;
}