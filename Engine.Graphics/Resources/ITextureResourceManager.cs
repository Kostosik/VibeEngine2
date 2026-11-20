using Engine.Core.Assets;

namespace Engine.Graphics.Resources;

public interface ITextureResourceManager : IDisposable
{
    TextureHandle Load(
        AssetPath path);

    TextureHandle Load(
    AssetPath path,
    TextureColorSpace colorSpace)
    {
        if (colorSpace == TextureColorSpace.Srgb)
            return Load(path);

        throw new NotSupportedException(
            "Explicit texture color-space loading is not supported.");
    }

    TextureAtlas LoadAtlas(
        AssetPath path,
        int tileWidth,
        int tileHeight);

    bool IsLoaded(
        AssetPath path);

    bool TryGet(
        AssetPath path,
        out TextureHandle texture);

    bool TryGetDescription(
        AssetPath path,
        out TextureDescription description);

    bool Unload(
        AssetPath path);

    void UnloadAll();
}