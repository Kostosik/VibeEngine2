using Engine.Scripting;
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
    public void GameSaveLoad_RestoresScriptAssignmentAndRuntimeInstance()
    {
        var ecsRegistry =
            new EcsComponentSerializerRegistry();

        ecsRegistry.RegisterEngineComponents();

        var saveLoad =
            new GameSaveLoadService(
                ecsRegistry,
                new PersistentStateRegistry());

        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(4, 4),
                ecsWorld);

        var entity =
            ecsWorld.CreateEntity();

        var instances =
            new List<PersistedScript>();

        var scriptRegistry =
            new ScriptRegistry();

        scriptRegistry.Register(
            "test.persisted",
            () =>
            {
                var script = new PersistedScript();
                instances.Add(script);
                return script;
            });

        using var runtime =
            new ScriptRuntime(
                ecsWorld,
                scriptRegistry);

        const string configuration =
            """{"speed":7,"mode":"automatic"}""";

        runtime.Attach(
            entity,
            "test.persisted",
            configuration);

        var originalScript =
            Assert.Single(instances);

        var path =
            Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}.vbe");

        try
        {
            // 1. Сохраняем исходное назначение.
            saveLoad.Save(
                path,
                world);

            // 2. Имитируем изменение назначения после сохранения.
            const string changedConfiguration =
                """{"speed":999}""";

            ecsWorld.Add(
                entity,
                new ScriptAssignmentsComponent(
                    new[]
                    {
                    new ScriptAssignment(
                        "test.persisted",
                        changedConfiguration)
                    }));

            // 3. Загружаем сохранённый мир.
            saveLoad.Load(
                path,
                world);

            Assert.True(
                ecsWorld.Has<ScriptAssignmentsComponent>(
                    entity));

            var assignments =
                ecsWorld.Get<ScriptAssignmentsComponent>(
                    entity);

            Assert.True(
                assignments.TryGet(
                    "test.persisted",
                    out var assignment));

            Assert.Equal(
                configuration,
                assignment.ConfigurationJson);

            // 4. Пересоздаём runtime-экземпляры из ECS.
            runtime.RebuildInstancesFromWorld();

            Assert.Equal(2, instances.Count);
            Assert.Equal(1, originalScript.DetachCount);
            Assert.Equal(1, runtime.InstanceCount);

            var restoredScript =
                instances[1];

            Assert.Equal(1, restoredScript.AttachCount);
            Assert.Equal(
                configuration,
                restoredScript.ConfigurationJson);

            // 5. Проверяем, что восстановленный экземпляр выполняется.
            runtime.UpdateSystem.Update(default);

            Assert.Equal(1, restoredScript.UpdateCount);
            Assert.Equal(1, runtime.InstanceCount);

            // Назначение должно остаться в ECS после Update.
            Assert.True(
                ecsWorld.Has<ScriptAssignmentsComponent>(
                    entity));

            Assert.True(
                ecsWorld.Get<ScriptAssignmentsComponent>(
                    entity).Contains("test.persisted"));
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

        ecsRegistry.RegisterEngineComponents();

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

    private sealed class PersistedScript : IEntityScript
    {
        public int AttachCount { get; private set; }

        public int UpdateCount { get; private set; }

        public int DetachCount { get; private set; }

        public string? ConfigurationJson { get; private set; }

        public void OnAttach(ScriptContext context)
        {
            AttachCount++;
            ConfigurationJson = context.ConfigurationJson;
        }

        public void Update(
            ScriptContext context,
            Engine.Core.Time.TimeSnapshot time)
        {
            UpdateCount++;
        }

        public void FixedUpdate(
            ScriptContext context,
            Engine.Core.Time.SimulationTime time)
        {
        }

        public void OnDetach(ScriptContext context)
        {
            DetachCount++;
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