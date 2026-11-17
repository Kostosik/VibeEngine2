using Engine.Core.Math;

namespace Engine.Graphics.DebugGraphics;

internal readonly record struct DebugTextLineLayout(
    string Text,
    float Width,
    float OffsetX,
    float OffsetY);

internal readonly record struct DebugTextLayoutResult(
    IReadOnlyList<DebugTextLineLayout> Lines,
    Vector2 Size);

internal static class DebugTextLayout
{
    private const int TabSize = 4;

    public static DebugTextLayoutResult Create(
        string text,
        float letterSpacing,
        float lineSpacing,
        DebugTextAlignment alignment)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (!float.IsFinite(letterSpacing) ||
            letterSpacing < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(letterSpacing));
        }

        if (!float.IsFinite(lineSpacing) ||
            lineSpacing < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lineSpacing));
        }

        var lines = SplitLines(text);
        var widths = new float[lines.Length];

        var maxWidth = 0.0f;

        for (var i = 0; i < lines.Length; i++)
        {
            widths[i] = MeasureLine(
                lines[i],
                letterSpacing);

            maxWidth = MathF.Max(
                maxWidth,
                widths[i]);
        }

        var result = new DebugTextLineLayout[lines.Length];

        for (var i = 0; i < lines.Length; i++)
        {
            var offsetX = alignment switch
            {
                DebugTextAlignment.Center =>
                    (maxWidth - widths[i]) * 0.5f,

                DebugTextAlignment.Right =>
                    maxWidth - widths[i],

                _ => 0.0f
            };

            result[i] = new DebugTextLineLayout(
                lines[i],
                widths[i],
                offsetX,
                i * (DebugBitmapFont.GlyphHeight + lineSpacing));
        }

        var height =
            DebugBitmapFont.GlyphHeight +
            Math.Max(0, lines.Length - 1) *
            (DebugBitmapFont.GlyphHeight + lineSpacing);

        return new DebugTextLayoutResult(
            result,
            new Vector2(maxWidth, height));
    }

    private static string[] SplitLines(string text)
    {
        var lines = new List<string>();

        var start = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is not ('\r' or '\n'))
                continue;

            lines.Add(
                text.Substring(start, i - start));

            if (text[i] == '\r' &&
                i + 1 < text.Length &&
                text[i + 1] == '\n')
            {
                i++;
            }

            start = i + 1;
        }

        lines.Add(
            text.Substring(start));

        return lines.ToArray();
    }

    private static float MeasureLine(
        string text,
        float letterSpacing)
    {
        var columns = 0;

        foreach (var character in text)
        {
            if (character == '\t')
            {
                columns =
                    ((columns / TabSize) + 1) *
                    TabSize;

                continue;
            }

            // Любой неизвестный символ занимает
            // место одного replacement-глифа.
            columns++;
        }

        if (columns == 0)
            return 0.0f;

        return
            columns * DebugBitmapFont.GlyphWidth +
            (columns - 1) * letterSpacing;
    }
}