using Engine.Core.Assets;

namespace Engine.Graphics.Resources;

public interface ITextureResourceManager : IDisposable
{
    TextureHandle Load(
        AssetPath path);

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