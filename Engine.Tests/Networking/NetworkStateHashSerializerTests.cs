using Engine.Core.Determinism;
using Engine.Core.Time;
using Engine.Networking.Simulation;

namespace Engine.Tests.Networking;

public sealed class NetworkStateHashSerializerTests
{
    [Fact]
    public void SerializeAndDeserialize_PreservesTickAndHash()
    {
        var message =
            new NetworkStateHashMessage(
                new Tick(42),
                new DeterministicStateHash(
                    123456789UL));

        var payload =
            NetworkStateHashSerializer.Serialize(
                message);

        var restored =
            NetworkStateHashSerializer.Deserialize(
                payload);

        Assert.Equal(
            message.Tick,
            restored.Tick);

        Assert.Equal(
            message.Hash,
            restored.Hash);
    }

    [Fact]
    public void Deserialize_WithInvalidPayloadSize_Throws()
    {
        var payload =
            new byte[15];

        Assert.Throws<InvalidDataException>(
            () =>
                NetworkStateHashSerializer.Deserialize(
                    payload));
    }
}