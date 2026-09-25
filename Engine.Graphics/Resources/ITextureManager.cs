namespace Engine.Graphics.Resources;

public interface ITextureManager
{
    TextureHandle Create(TextureDescription description);

    TextureHandle Create(TextureData data);

    bool Exists(TextureHandle texture);

    TextureDescription GetDescription(TextureHandle texture);

    void Destroy(TextureHandle texture);
}