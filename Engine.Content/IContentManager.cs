using Engine.Core.Assets;
using Engine.Content.Loading;

namespace Engine.Content;

public interface IContentManager
{
    T Load<T>(
        AssetPath path)
        where T : class;

    T Reload<T>(
        AssetPath path)
        where T : class;

    bool IsLoaded<T>(
        AssetPath path)
        where T : class;

    bool Unload<T>(
        AssetPath path)
        where T : class;

    void ClearCache();

    ValueTask<T> LoadAsync<T>(
    AssetPath path,
    CancellationToken cancellationToken = default)
    where T : class;
}