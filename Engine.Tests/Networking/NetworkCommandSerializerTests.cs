using Engine.Core.Commands;
using Engine.Core.Replays;
using Engine.Core.Time;
using Engine.Networking.Simulation;

namespace Engine.Tests.Networking;

public sealed class NetworkCommandSerializerTests
{
    [Fact]
    public void SerializeAndDeserialize_PreservesTickAndCommands()
    {
        var registry =
            new ReplayCommandRegistry();

        registry.Register<AddValueCommand>(
            "add_value");

        var batch =
            new NetworkCommandBatch(
                TickFromInt(42));

        batch.Add(
            new AddValueCommand(10));

        batch.Add(
            new AddValueCommand(20));

        var payload =
            NetworkCommandSerializer.Serialize(
                batch,
                registry);

        var restored =
            NetworkCommandSerializer.Deserialize(
                payload,
                registry);

        Assert.Equal(
            batch.Tick,
            restored.Tick);

        Assert.Equal(
            2,
            restored.Commands.Count);

        var first =
            Assert.IsType<AddValueCommand>(
                restored.Commands[0]);

        var second =
            Assert.IsType<AddValueCommand>(
                restored.Commands[1]);

        Assert.Equal(
            10,
            first.Value);

        Assert.Equal(
            20,
            second.Value);
    }

    private static Tick TickFromInt(
        int value)
    {
        var tick =
            Tick.Zero;

        for (var i = 0; i < value; i++)
        {
            tick++;
        }

        return tick;
    }

    private sealed class AddValueCommand
        : ICommand
    {
        public AddValueCommand(
            int value)
        {
            Value = value;
        }

        public int Value { get; }
    }
}