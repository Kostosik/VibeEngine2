using Engine.Core.Assets;

namespace Engine.Content.Assets;

public interface IContentCatalog
{
    IReadOnlyList<ContentAsset> GetAssets();

    bool Contains(
        AssetPath path);

    bool TryGet(
        AssetPath path,
        out ContentAsset? asset);
}