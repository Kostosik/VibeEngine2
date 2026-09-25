using Engine.Core.Math;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.UI.Controls;

public sealed class UiSpacer : UiWidget
{
    public UiSpacer(
        float width = 0.0f,
        float height = 0.0f)
    {
        Width = width;
        Height = height;
    }

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        return new Vector2(
            Width ?? 0.0f,
            Height ?? 0.0f);
    }
}