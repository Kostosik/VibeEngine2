namespace Engine.Graphics.Resources;

public sealed class TextureAtlasResource : IDisposable
{
    private readonly ITextureManager _textureManager;
    private bool _disposed;

    internal TextureAtlasResource(
        ITextureManager textureManager,
        TextureHandle texture,
        TextureAtlas atlas)
    {
        _textureManager = textureManager;
        Texture = texture;
        Atlas = atlas;
    }

    public TextureHandle Texture { get; }

    public TextureAtlas Atlas { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        _textureManager.Destroy(Texture);
        _disposed = true;
    }
}