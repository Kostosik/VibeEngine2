using Engine.Core.Assets;

namespace Engine.Core.Resources;

public sealed class ResourceManager<TResource>
{
    private readonly IAssetSource _source;
    private readonly IResourceLoader<TResource> _loader;

    private readonly Dictionary<AssetPath, TResource> _resources = new();

    public ResourceManager(
        IAssetSource source,
        IResourceLoader<TResource> loader)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(loader);

        _source = source;
        _loader = loader;
    }

    public TResource Load(
        AssetPath path)
    {
        if (_resources.TryGetValue(
                path,
                out var resource))
        {
            return resource;
        }

        var data =
            _source.Load(path);

        resource =
            _loader.Load(
                path,
                data);

        _resources.Add(
            path,
            resource);

        return resource;
    }

    public bool TryGet(
        AssetPath path,
        out TResource resource)
    {
        return _resources.TryGetValue(
            path,
            out resource!);
    }

    public bool IsLoaded(
        AssetPath path)
    {
        return _resources.ContainsKey(path);
    }

    public bool Unload(
        AssetPath path)
    {
        if (!_resources.Remove(
                path,
                out var resource))
        {
            return false;
        }

        DisposeResource(resource);

        return true;
    }

    public void UnloadAll()
    {
        foreach (var resource in _resources.Values)
        {
            DisposeResource(resource);
        }

        _resources.Clear();
    }

    private static void DisposeResource(
        TResource resource)
    {
        if (resource is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}