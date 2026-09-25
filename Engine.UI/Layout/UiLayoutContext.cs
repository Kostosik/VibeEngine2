using Engine.Core.Math;
using Engine.Graphics.Fonts;
using Engine.UI.Styling;

namespace Engine.UI.Layout;

public sealed class UiLayoutContext
{
    private readonly IFontManager _fonts;

    public UiLayoutContext(
        IFontManager fonts,
        UiTheme theme)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        ArgumentNullException.ThrowIfNull(theme);

        _fonts = fonts;
        Theme = theme;
    }

    public UiTheme Theme { get; }

    public Vector2 MeasureText(
        string text,
        float fontSize)
    {
        if (string.IsNullOrEmpty(text) ||
            fontSize <= 0.0f ||
            !Theme.DefaultFont.IsValid)
        {
            return Vector2.Zero;
        }

        var metrics =
            _fonts.MeasureText(
                Theme.DefaultFont,
                text);

        if (metrics.LineHeight <= 0.0f)
        {
            return Vector2.Zero;
        }

        var scale =
            fontSize /
            metrics.LineHeight;

        return new Vector2(
            metrics.Width * scale,
            metrics.Height * scale);
    }
}