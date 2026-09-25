using Engine.ECS;
using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;

namespace Engine.Tests.Serialization.SaveLoad;

public sealed class EcsWorldSaveLoadTests
{
    [Fact]
    public void EcsWorld_RoundTripsThroughRealFile()
    {
        var registry =
            new EcsComponentSerializerRegistry();

        registry.Register(
            "test.value",
            new TestValueSerializer());

        var serializer =
            new EcsWorldStateSerializer(
                registry);

        using var world =
            new World();

        var first =
            world.CreateEntity();

        var destroyed =
            world.CreateEntity();

        var third =
            world.CreateEntity();

        world.Add(
            first,
            new TestValue(10));

        world.Add(
            third,
            new TestValue(30));

        Assert.True(
            world.DestroyEntity(
                destroyed));

        var path =
            Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}.vbe");

        try
        {
            BinaryFileSerializer.Save(
                path,
                world.CaptureState(),
                serializer);

            using var restoredWorld =
                new World();

            var state =
                BinaryFileSerializer.Load(
                    path,
                    serializer);

            restoredWorld.RestoreState(
                state);

            Assert.True(
                restoredWorld.Exists(
                    first));

            Assert.False(
                restoredWorld.Exists(
                    destroyed));

            Assert.True(
                restoredWorld.Exists(
                    third));

            Assert.Equal(
                10,
                restoredWorld
                    .Get<TestValue>(
                        first)
                    .Value);

            Assert.Equal(
                30,
                restoredWorld
                    .Get<TestValue>(
                        third)
                    .Value);

            Assert.Equal(
                world.EntityCount,
                restoredWorld.EntityCount);

            var expectedNextEntity =
                world.CreateEntity();

            var actualNextEntity =
                restoredWorld.CreateEntity();

            Assert.Equal(
                expectedNextEntity,
                actualNextEntity);
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