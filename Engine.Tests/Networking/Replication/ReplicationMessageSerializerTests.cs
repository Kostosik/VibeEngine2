using Engine.Networking.Replication;
using Engine.Serialization.Binary;

namespace Engine.Tests.Networking.Replication;

public sealed class ReplicationMessageSerializerTests
{
    [Fact]
    public void SerializeDeserialize_PreservesSpawnMessage()
    {
        var message =
            ReplicationMessage.Spawn(
                new ReplicatedEntityState(
                    new NetworkEntityId(100),
                    new[]
                    {
                        new ReplicatedComponentState(
                            "component.a",
                            new byte[]
                            {
                                1,
                                2,
                                3
                            }),
                        new ReplicatedComponentState(
                            "component.b",
                            new byte[]
                            {
                                4,
                                5
                            })
                    }));

        var serializer =
            new ReplicationMessageSerializer();

        var data =
            BinarySerializer.Serialize(
                message,
                serializer);

        var restored =
            BinarySerializer.Deserialize(
                data,
                serializer);

        Assert.Equal(
            ReplicationOperation.Spawn,
            restored.Operation);

        Assert.Equal(
            100UL,
            restored.State.Id.Value);

        Assert.Equal(
            2,
            restored.State.Components.Count);

        Assert.Equal(
            "component.a",
            restored.State.Components[0].Id);

        Assert.Equal(
            new byte[]
            {
                1,
                2,
                3
            },
            restored.State.Components[0].Payload.ToArray());

        Assert.Equal(
            "component.b",
            restored.State.Components[1].Id);

        Assert.Equal(
            new byte[]
            {
                4,
                5
            },
            restored.State.Components[1].Payload.ToArray());
    }

    [Fact]
    public void Deserialize_DespawnMessageContainsNoComponents()
    {
        var message =
            ReplicationMessage.Despawn(
                new NetworkEntityId(200));

        var serializer =
            new ReplicationMessageSerializer();

        var data =
            BinarySerializer.Serialize(
                message,
                serializer);

        var restored =
            BinarySerializer.Deserialize(
                data,
                serializer);

        Assert.Equal(
            ReplicationOperation.Despawn,
            restored.Operation);

        Assert.Equal(
            200UL,
            restored.State.Id.Value);

        Assert.Empty(
            restored.State.Components);
    }

}