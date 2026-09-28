using Engine.Content.Assets;

namespace Engine.Content.Loading;

public sealed class ContentLoaderRegistry :
    IContentLoaderRegistry
{
    private readonly List<IContentLoader> _loaders =
        new();

    public void Register<T>(
        IContentLoader<T> loader)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(
            loader);

        _loaders.Add(
            loader);
    }

    public bool TryGet<T>(
        ContentAsset asset,
        out IContentLoader<T>? loader)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(
            asset);

        IContentLoader<T>? match =
            null;

        foreach (var candidate in _loaders)
        {
            if (candidate.AssetType !=
                typeof(T))
            {
                continue;
            }

            if (!candidate.CanLoad(
                    asset))
            {
                continue;
            }

            if (match is not null)
            {
                throw new InvalidOperationException(
                    $"Multiple content loaders can load '{asset.Path}' as '{typeof(T).Name}'.");
            }

            match =
                (IContentLoader<T>)candidate;
        }

        loader =
            match;

        return match is not null;
    }

    public void Clear()
    {
        _loaders.Clear();
    }
}