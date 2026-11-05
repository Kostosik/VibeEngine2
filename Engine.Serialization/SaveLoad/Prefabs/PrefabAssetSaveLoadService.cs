using Engine.ECS.Prefabs;
using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;

namespace Engine.Serialization.SaveLoad.Prefabs;

public sealed class PrefabAssetSaveLoadService
{
    private readonly PrefabAssetSerializer _serializer;

    public PrefabAssetSaveLoadService(
        EcsComponentSerializerRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        _serializer = new PrefabAssetSerializer(registry);
    }

    public void Save(string path, PrefabAsset asset)
    {
        Save(path, asset, SerializationContext.Default);
    }

    public void Save(
        string path,
        PrefabAsset asset,
        SerializationContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(asset);

        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        BinaryFileSerializer.Save(
            path,
            asset,
            _serializer,
            context);
    }

    public PrefabAsset Load(string path)
    {
        return Load(path, SerializationContext.Default);
    }

    public PrefabAsset Load(
        string path,
        SerializationContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return BinaryFileSerializer.Load(
            path,
            _serializer,
            context);
    }
}