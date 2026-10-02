using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Worlds;
using Engine.Worlds;
using Engine.Worlds.Persistence;

namespace Engine.Serialization.SaveLoad.Game;

public sealed class GameSaveLoadService
{
    private readonly PersistentStateRegistry _persistentStates;
    private readonly GameSaveMigrationRegistry _migrations;
    private readonly GameSaveStateSerializer _serializer;

    public GameSaveLoadService(
        EcsComponentSerializerRegistry ecsComponents,
        PersistentStateRegistry persistentStates,
        GameSaveMigrationRegistry? migrations = null)
    {
        ArgumentNullException.ThrowIfNull(
            ecsComponents);

        ArgumentNullException.ThrowIfNull(
            persistentStates);

        _persistentStates =
            persistentStates;

        _migrations =
            migrations
            ?? new GameSaveMigrationRegistry();

        _serializer =
            new GameSaveStateSerializer(
                new WorldSaveStateSerializer(
                    new EcsWorldStateSerializer(
                        ecsComponents)),
                persistentStates);
    }

    public GameSaveState Capture(
        World world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        return new GameSaveState(
            GameSaveStateSerializer.CurrentVersion,
            WorldPersistence.Capture(
                world),
            _persistentStates.Capture());
    }

    public void Restore(
        World world,
        GameSaveState state)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        ArgumentNullException.ThrowIfNull(
            state);

        var migrated =
            _migrations.MigrateTo(
                state,
                GameSaveStateSerializer.CurrentVersion);

        _persistentStates.Validate(
            migrated.PersistentStates);

        WorldPersistence.Restore(
            world,
            migrated.World);

        _persistentStates.Restore(
            migrated.PersistentStates);
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
            Capture(
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

        Restore(
            world,
            state);
    }
}