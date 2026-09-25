namespace Engine.Graphics.Fonts;

public interface IFontManager
{
    FontHandle DefaultFont { get; }

    FontHandle Create(
        ReadOnlyMemory<byte> data,
        FontDescription description);

    bool Exists(
        FontHandle font);

    FontGlyph GetGlyph(
        FontHandle font,
        int codepoint);

    float GetKerning(
        FontHandle font,
        int leftCodepoint,
        int rightCodepoint);

    FontMetrics GetMetrics(
        FontHandle font);

    void Destroy(
        FontHandle font);

    FontTextMetrics MeasureText(
    FontHandle font,
    string text);
}