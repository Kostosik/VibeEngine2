using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Worlds;
using Engine.Worlds.Persistence;

namespace Engine.Serialization.SaveLoad.Worlds;

public sealed class WorldSaveLoadService
{
    private readonly WorldSaveStateSerializer _serializer;

    public WorldSaveLoadService(
        EcsComponentSerializerRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(
            registry);

        _serializer =
            new WorldSaveStateSerializer(
                new EcsWorldStateSerializer(
                    registry));
    }

    public void Save(
        string path,
        World world)
    {
        Save(
            path,
            world,
            SerializationContext.Default);
    }

    public void Save(
        string path,
        World world,
        SerializationContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            path);

        ArgumentNullException.ThrowIfNull(
            world);

        var state =
            WorldPersistence.Capture(
                world);

        BinaryFileSerializer.Save(
            path,
            state,
            _serializer,
            context);
    }

    public void Load(
        string path,
        World world)
    {
        Load(
            path,
            world,
            SerializationContext.Default);
    }

    public void Load(
        string path,
        World world,
        SerializationContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            path);

        ArgumentNullException.ThrowIfNull(
            world);

        var state =
            BinaryFileSerializer.Load(
                path,
                _serializer,
                context);

        WorldPersistence.Restore(
            world,
            state);
    }
}