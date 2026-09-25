using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.UI.Controls;

public sealed class UiProgressBar : UiWidget
{
    private float _value;

    public UiProgressBar()
    {
        Width = 200.0f;
        Height = 20.0f;

        HorizontalAlignment =
            UiHorizontalAlignment.Left;

        VerticalAlignment =
            UiVerticalAlignment.Top;
    }

    public float Value
    {
        get => _value;

        set =>
            _value =
                Math.Clamp(
                    value,
                    0.0f,
                    1.0f);
    }

    public UiColor Background { get; set; } =
        new(35, 35, 35, 230);

    public UiColor Fill { get; set; } =
        new(80, 200, 100, 255);

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        return new Vector2(
            Width ?? 200.0f,
            Height ?? 20.0f);
    }

    protected override void OnRender(
        UiRenderContext context)
    {
        context.DrawRectangle(
            Bounds,
            Background,
            filled: true);

        var fillWidth =
            Bounds.Width *
            _value;

        if (fillWidth <= 0.0f)
        {
            return;
        }

        context.DrawRectangle(
            new UiRect(
                Bounds.X,
                Bounds.Y,
                fillWidth,
                Bounds.Height),
            Fill,
            filled: true,
            layer: 1);
    }
}