
using Engine.UI.Layout;

namespace Engine.Tests.UI;

public sealed class UiTextLayoutTests
{
    [Fact]
    public void TruncateToFit_ReturnsOriginalTextWhenItFits()
    {
        var result = UiTextLayout.TruncateToFit(
            "Save",
            4.0f,
            text => text.Length);

        Assert.Equal("Save", result);
    }

    [Fact]
    public void TruncateToFit_AddsEllipsisWhenTextDoesNotFit()
    {
        var result = UiTextLayout.TruncateToFit(
            "Create",
            5.0f,
            text => text.Length);

        Assert.Equal("Crea…", result);
    }

    [Fact]
    public void TruncateToFit_DoesNotSplitUnicodeTextElements()
    {
        static float MeasureWidth(string text)
        {
            var starts =
                System.Globalization.StringInfo
                    .ParseCombiningCharacters(text);

            var width = 0.0f;

            for (var i = 0; i < starts.Length; i++)
            {
                var end =
                    i + 1 < starts.Length
                        ? starts[i + 1]
                        : text.Length;

                var element = text[starts[i]..end];

                // Элемент B шире остальных символов.
                width += element == "B" ? 2.0f : 1.0f;
            }

            return width;
        }

        var result = UiTextLayout.TruncateToFit(
            "A😀B",
            3.0f,
            MeasureWidth);

        Assert.Equal("A😀…", result);
    }

    [Fact]
    public void TruncateToFit_ReturnsEmptyWhenEllipsisDoesNotFit()
    {
        var result = UiTextLayout.TruncateToFit(
            "Long text",
            0.5f,
            text => text.Length);

        Assert.Equal(string.Empty, result);
    }
}
