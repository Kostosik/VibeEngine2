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

    bool Unload(
        AssetPath path);

    void UnloadAll();
}