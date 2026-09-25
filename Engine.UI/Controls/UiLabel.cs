using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.UI.Controls;

public sealed class UiLabel : UiWidget
{
    public UiLabel(
        string text = "")
    {
        Text = text;

        HorizontalAlignment =
            UiHorizontalAlignment.Left;

        VerticalAlignment =
            UiVerticalAlignment.Top;

        HorizontalTextAlignment =
            UiTextHorizontalAlignment.Left;

        VerticalTextAlignment =
            UiTextVerticalAlignment.Top;
    }

    public string Text { get; set; }

    public float FontSize { get; set; } = 16.0f;

    public UiColor Color { get; set; } =
        UiColor.White;

    public UiTextHorizontalAlignment
        HorizontalTextAlignment
    { get; set; }

    public UiTextVerticalAlignment
        VerticalTextAlignment
    { get; set; }

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        return context.MeasureText(
            Text,
            FontSize);
    }

    protected override void OnRender(
        UiRenderContext context)
    {
        var textSize =
            context.MeasureText(
                Text,
                FontSize);

        var position =
            UiTextLayout.CalculatePosition(
                Bounds,
                textSize,
                HorizontalTextAlignment,
                VerticalTextAlignment);

        context.DrawText(
            Text,
            position,
            FontSize,
            Color);
    }
}