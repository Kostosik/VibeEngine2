using Engine.Core.Math;

namespace Engine.UI.Layout;

public static class UiTextLayout
{
    public static Vector2 CalculatePosition(
        UiRect bounds,
        Vector2 textSize,
        UiTextHorizontalAlignment horizontal,
        UiTextVerticalAlignment vertical)
    {
        var x =
            horizontal switch
            {
                UiTextHorizontalAlignment.Center =>
                    bounds.X +
                    (bounds.Width -
                     textSize.X) /
                    2.0f,

                UiTextHorizontalAlignment.Right =>
                    bounds.Right -
                    textSize.X,

                _ =>
                    bounds.X
            };

        var y =
            vertical switch
            {
                UiTextVerticalAlignment.Center =>
                    bounds.Y +
                    (bounds.Height -
                     textSize.Y) /
                    2.0f,

                UiTextVerticalAlignment.Bottom =>
                    bounds.Bottom -
                    textSize.Y,

                _ =>
                    bounds.Y
            };

        return new Vector2(
            x,
            y);
    }
}