using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;
using Engine.ECS.Prefabs;
using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Prefabs;

namespace Engine.Serialization.Content;

/// <summary>
/// Loads binary .prefab assets through the content pipeline.
/// </summary>
public sealed class PrefabAssetContentLoader :
    IContentLoader<PrefabAsset>
{
    private const string FileExtension = ".prefab";

    private readonly BinaryContentLoader<PrefabAsset> _loader;

    public PrefabAssetContentLoader(
        EcsComponentSerializerRegistry registry)
        : this(
            registry,
            SerializationContext.Default)
    {
    }

    public PrefabAssetContentLoader(
        EcsComponentSerializerRegistry registry,
        SerializationContext serializationContext)
    {
        ArgumentNullException.ThrowIfNull(registry);

        _loader = new BinaryContentLoader<PrefabAsset>(
            new PrefabAssetSerializer(registry),
            static asset => asset.Extension.Equals(
                FileExtension,
                StringComparison.OrdinalIgnoreCase),
            serializationContext);
    }

    public Type AssetType => typeof(PrefabAsset);

    public bool CanLoad(ContentAsset asset)
    {
        return _loader.CanLoad(asset);
    }

    public PrefabAsset Load(
        AssetPath path,
        IContentLoadContext context)
    {
        return _loader.Load(path, context);
    }

    public ValueTask<PrefabAsset> LoadAsync(
        AssetPath path,
        IContentLoadContext context,
        CancellationToken cancellationToken = default)
    {
        return _loader.LoadAsync(
            path,
            context,
            cancellationToken);
    }

    object IContentLoader.Load(
        AssetPath path,
        IContentLoadContext context)
    {
        return Load(path, context);
    }
}