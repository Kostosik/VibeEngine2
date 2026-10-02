using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Game;
using Engine.Serialization.Types;
using Engine.Worlds;
using Engine.Worlds.Persistence;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Serialization.SaveLoad;

public sealed class GameSaveLoadServiceTests
{
    [Fact]
    public void Load_CorruptedFile_DoesNotMutateWorld()
    {
        var ecsRegistry =
            new EcsComponentSerializerRegistry();

        var persistentStates =
            new PersistentStateRegistry();

        var saveLoad =
            new GameSaveLoadService(
                ecsRegistry,
                persistentStates);

        using var sourceEcsWorld =
            new Engine.ECS.World();

        var sourceWorld =
            new Engine.Worlds.World(
                new ChunkSize(4, 4),
                sourceEcsWorld);

        var path =
            Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}.vbe");

        using var targetEcsWorld =
            new Engine.ECS.World();

        var targetWorld =
            new Engine.Worlds.World(
                new ChunkSize(4, 4),
                targetEcsWorld);

        var existingEntity =
            targetWorld.EcsWorld.CreateEntity();

        try
        {
            saveLoad.Save(
                path,
                sourceWorld);

            var data =
                File.ReadAllBytes(
                    path);

            File.WriteAllBytes(
                path,
                data[..^1]);

            Assert.Throws<InvalidDataException>(
                () =>
                    saveLoad.Load(
                        path,
                        targetWorld));

            Assert.True(
                targetWorld.EcsWorld.Exists(
                    existingEntity));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void GameSave_RoundTripsWorldAndPersistentState()
    {
        var ecsRegistry =
            new EcsComponentSerializerRegistry();

        ecsRegistry.Register(
    "engine.world.position",
    new WorldPositionComponentSerializer());

        var persistentStates =
            new PersistentStateRegistry();

        var value =
            42;

        persistentStates.Register(
            "test.value",
            () => value,
            restored =>
            {
                value = restored;
            },
            new IntSerializer());

        var service =
            new GameSaveLoadService(
                ecsRegistry,
                persistentStates);

        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(
                    4,
                    4),
                ecsWorld);

        var entity =
            world.SpatialEntities.CreateEntity(
                new WorldPosition(
                    3,
                    5));

        var path =
            Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}.vbe");

        try
        {
            service.Save(
                path,
                world);

            world.SpatialEntities.SetPosition(
                entity,
                new WorldPosition(
                    100,
                    200));

            value =
                999;

            service.Load(
                path,
                world);

            Assert.Equal(
                new WorldPosition(
                    3,
                    5),
                world.SpatialEntities.GetPosition(
                    entity));

            Assert.Equal(
                42,
                value);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private sealed class IntSerializer :
        IBinarySerializer<int>
    {
        public void Serialize(
            ref SerializationWriter writer,
            int value)
        {
            writer.WriteInt32(
                value);
        }

        public int Deserialize(
            ref SerializationReader reader)
        {
            return reader.ReadInt32();
        }
    }
}