using Engine.Graphics.Fonts;

namespace Engine.Graphics.OpenGL.Fonts;

internal sealed class OpenGLFont :
    IDisposable
{
    public OpenGLFont(
        FontHandle handle,
        FontDescription description,
        FontMetrics metrics,
        Dictionary<int, FontGlyph> glyphs,
        IReadOnlyList<Engine.Graphics.Resources.TextureHandle> textures)
    {
        Handle = handle;
        Description = description;
        Metrics = metrics;
        Glyphs = glyphs;
        Textures = textures;
    }

    public FontHandle Handle { get; }

    public FontDescription Description { get; }

    public FontMetrics Metrics { get; }

    public IReadOnlyDictionary<int, FontGlyph> Glyphs { get; }

    public IReadOnlyList<Engine.Graphics.Resources.TextureHandle> Textures { get; }

    public void Dispose()
    {
    }
}