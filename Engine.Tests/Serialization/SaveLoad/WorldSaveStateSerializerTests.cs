using Engine.Core.Time;
using Engine.ECS.Entities;
using Engine.ECS.Persistence;
using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Worlds;
using Engine.Worlds.Chunks;
using Engine.Worlds.Persistence;
using Engine.Worlds.Spatial;
using Engine.Worlds.Tiles;

namespace Engine.Tests.Serialization.SaveLoad;

public sealed class WorldSaveStateSerializerTests
{
    [Fact]
    public void WorldSaveState_RoundTripsThroughRealFile()
    {
        var registry =
            new EcsComponentSerializerRegistry();

        registry.Register(
            "test.value",
            new TestValueSerializer());

        var ecsSerializer =
            new EcsWorldStateSerializer(
                registry);

        var serializer =
            new WorldSaveStateSerializer(
                ecsSerializer);

        var entity =
            new EntityId(
                1,
                1);

        var ecs =
            new EcsWorldState(
                new EntityStoreState(
                    2,
                    new uint[] { 1 },
                    new uint[] { 1 },
                    Array.Empty<uint>()),
                new WorldComponentState[]
                {
                    new WorldComponentState<TestValue>(
                        new[] { entity },
                        new[] { new TestValue(42) })
                });

        var tiles =
            Enumerable
                .Range(0, 16)
                .Select(
                    value =>
                        new Tile(
                            (uint)(value + 100)))
                .ToArray();

        var state =
            new WorldSaveState(
                new ChunkSize(
                    4,
                    4),
                ecs,
                new[]
                {
                    new ChunkSaveState(
                        new ChunkPosition(
                            -2,
                            3),
                        ChunkSimulationState.Simulating,
                        ChunkPresentationState.Irrelevant,
                        tiles)
                });

        var path =
            Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}.vbe");

        try
        {
            BinaryFileSerializer.Save(
                path,
                state,
                serializer);

            var restored =
                BinaryFileSerializer.Load(
                    path,
                    serializer);

            Assert.Equal(
                4,
                restored.ChunkSize.Width);

            Assert.Equal(
                4,
                restored.ChunkSize.Height);

            Assert.Single(
                restored.Ecs.Components);

            Assert.Single(
                restored.Chunks);

            var chunk =
                restored.Chunks[0];

            Assert.Equal(
                new ChunkPosition(
                    -2,
                    3),
                chunk.Position);

            Assert.Equal(
                ChunkSimulationState.Simulating,
                chunk.Simulation);

            Assert.Equal(
                ChunkPresentationState.Irrelevant,
                chunk.Presentation);

            Assert.Equal(
                16,
                chunk.Tiles.Length);

            Assert.Equal(
                (uint)100,
                chunk.Tiles[0].Value);

            Assert.Equal(
                (uint)115,
                chunk.Tiles[15].Value);

            var component =
                (WorldComponentState<TestValue>)
                    restored.Ecs.Components[0];

            Assert.Equal(
                entity,
                component.Entities[0]);

            Assert.Equal(
                42,
                component.Components[0].Value);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private readonly record struct TestValue(
        int Value);

    private sealed class TestValueSerializer :
        IBinarySerializer<TestValue>
    {
        public void Serialize(
            ref SerializationWriter writer,
            TestValue value)
        {
            writer.WriteInt32(
                value.Value);
        }

        public TestValue Deserialize(
            ref SerializationReader reader)
        {
            return new TestValue(
                reader.ReadInt32());
        }
    }
}