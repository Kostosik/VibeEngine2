using Engine.Core.Time;
using Engine.ECS.Entities;
using Engine.Serialization.Binary;
using Engine.Serialization.Types;

namespace Engine.Tests.Serialization;

public sealed class CoreTypeSerializationTests
{
    [Fact]
    public void CoreTypes_RoundTripWithoutDataLoss()
    {
        var tick =
            new Tick(
                123456789UL);

        var duration =
            new Duration(
                TimeSpan.FromTicks(
                    -987654321));

        var entityId =
            new EntityId(
                42,
                7);

        var tickSerializer =
            new TickSerializer();

        var durationSerializer =
            new DurationSerializer();

        var entityIdSerializer =
            new EntityIdSerializer();

        var tickData =
            BinarySerializer.Serialize(
                tick,
                tickSerializer);

        var durationData =
            BinarySerializer.Serialize(
                duration,
                durationSerializer);

        var entityIdData =
            BinarySerializer.Serialize(
                entityId,
                entityIdSerializer);

        var restoredTick =
            BinarySerializer.Deserialize(
                tickData,
                tickSerializer);

        var restoredDuration =
            BinarySerializer.Deserialize(
                durationData,
                durationSerializer);

        var restoredEntityId =
            BinarySerializer.Deserialize(
                entityIdData,
                entityIdSerializer);

        Assert.Equal(
            tick,
            restoredTick);

        Assert.Equal(
            duration,
            restoredDuration);

        Assert.Equal(
            entityId,
            restoredEntityId);
    }
}