
using Engine.Core.Math;

namespace Engine.UI.Layout;

public static class UiTextLayout
{
    public static string TruncateToFit(
        string text,
        float maxWidth,
        Func<string, float> measureWidth)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(measureWidth);

        if (string.IsNullOrEmpty(text) ||
            float.IsNaN(maxWidth) ||
            maxWidth <= 0.0f)
        {
            return string.Empty;
        }

        var fullTextWidth = measureWidth(text);

        if (float.IsFinite(fullTextWidth) &&
            fullTextWidth >= 0.0f &&
            fullTextWidth <= maxWidth)
        {
            return text;
        }

        const string ellipsis = "…";

        var ellipsisWidth = measureWidth(ellipsis);

        if (!float.IsFinite(ellipsisWidth) ||
            ellipsisWidth < 0.0f ||
            ellipsisWidth > maxWidth)
        {
            return string.Empty;
        }

        // Получаем позиции начала Unicode-текстовых элементов,
        // чтобы не разрезать символы посередине.
        var textElementStarts =
            System.Globalization.StringInfo
                .ParseCombiningCharacters(text);

        if (textElementStarts.Length == 0)
        {
            return string.Empty;
        }

        // Ищем максимальный префикс, который помещается
        // вместе с многоточием.
        var low = 0;
        var high = textElementStarts.Length - 1;

        while (low < high)
        {
            var middle =
                low + (high - low + 1) / 2;

            var prefixLength =
                textElementStarts[middle];

            var candidate =
                text[..prefixLength] + ellipsis;

            var candidateWidth =
                measureWidth(candidate);

            if (float.IsFinite(candidateWidth) &&
                candidateWidth >= 0.0f &&
                candidateWidth <= maxWidth)
            {
                low = middle;
            }
            else
            {
                high = middle - 1;
            }
        }

        return
            text[..textElementStarts[low]] +
            ellipsis;
    }

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
