using Engine.Content.Assets;

namespace Engine.Content.Loading;

public interface IContentLoaderRegistry
{
    void Register<T>(
        IContentLoader<T> loader)
        where T : class;

    bool TryGet<T>(
        ContentAsset asset,
        out IContentLoader<T>? loader)
        where T : class;

    IReadOnlyList<IContentLoader> FindLoaders(
        ContentAsset asset);

    void Clear();
}