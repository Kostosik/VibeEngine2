namespace Engine.Graphics.Fonts;

public readonly record struct FontDescription(
    int PixelHeight,
    int AtlasWidth = 1024,
    int AtlasHeight = 1024,
    int Padding = 2)
{
    public void Validate()
    {
        if (PixelHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(PixelHeight));
        }

        if (AtlasWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(AtlasWidth));
        }

        if (AtlasHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(AtlasHeight));
        }

        if (Padding < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Padding));
        }
    }
}