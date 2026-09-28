using Engine.Content.Assets;
using Engine.Core.Assets;

namespace Engine.Content.Loading;

public interface IContentLoader
{
    Type AssetType { get; }

    bool CanLoad(
        ContentAsset asset);

    object Load(
        AssetPath path,
        IContentLoadContext context);
}

public interface IContentLoader<T> :
    IContentLoader
    where T : class
{
    new T Load(
        AssetPath path,
        IContentLoadContext context);

    ValueTask<T> LoadAsync(
        AssetPath path,
        IContentLoadContext context,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<T>(
            Load(
                path,
                context));
    }
}