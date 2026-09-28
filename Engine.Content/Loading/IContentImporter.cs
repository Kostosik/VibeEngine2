using Engine.Content.Assets;
using Engine.Core.Assets;

namespace Engine.Content.Loading;

public interface IContentImporter<T>
    where T : class
{
    bool CanImport(
        ContentAsset asset);

    T Import(
        AssetPath path,
        ReadOnlyMemory<byte> data);
}