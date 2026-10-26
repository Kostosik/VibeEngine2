namespace Engine.Graphics.Fonts;

public readonly record struct FontCodepointRange(
    int FirstCodepoint,
    int CodepointCount)
{
    public void Validate()
    {
        if (FirstCodepoint < 0 ||
            FirstCodepoint > 0x10FFFF)
        {
            throw new ArgumentOutOfRangeException(
                nameof(FirstCodepoint));
        }

        if (CodepointCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(CodepointCount));
        }

        var lastCodepoint =
            (long)FirstCodepoint + CodepointCount - 1;

        if (lastCodepoint > 0x10FFFF)
        {
            throw new ArgumentOutOfRangeException(
                nameof(CodepointCount),
                "The range exceeds the Unicode codepoint limit.");
        }
    }
}