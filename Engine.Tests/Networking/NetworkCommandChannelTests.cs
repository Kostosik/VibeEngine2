using Engine.Core.Commands;
using Engine.Core.Replays;
using Engine.Core.Time;
using Engine.Networking.Connections;
using Engine.Networking.Sessions;
using Engine.Networking.Simulation;
using Engine.Networking.Transport;

namespace Engine.Tests.Networking;

public sealed class NetworkCommandChannelTests
{
    [Fact]
    public void SendAndUpdate_DeliversCommandsForCorrectTick()
    {
        using var serverTransport =
            new LoopbackTransport();

        using var clientTransport =
            new LoopbackTransport();

        using var serverSession =
            new NetworkSession(
                serverTransport);

        using var clientSession =
            new NetworkSession(
                clientTransport);

        var serverEndpoint =
            new NetworkEndpoint(
                "server",
                1000);

        var clientEndpoint =
            new NetworkEndpoint(
                "client",
                1001);

        serverSession.Start(
            serverEndpoint);

        clientSession.Start(
            clientEndpoint);

        var clientConnection =
            clientSession.Connect(
                serverEndpoint);

        serverSession.Update();

        var serverConnection =
            Assert.Single(
                serverSession.Connections);

        var registry =
            new ReplayCommandRegistry();

        registry.Register<AddValueCommand>(
            "add_value");

        using var serverChannel =
            new NetworkCommandChannel(
                serverSession,
                registry);

        using var clientChannel =
            new NetworkCommandChannel(
                clientSession,
                registry);

        var batch =
            new NetworkCommandBatch(
                TickFromInt(42));

        batch.Add(
            new AddValueCommand(10));

        batch.Add(
            new AddValueCommand(20));

        Assert.True(
            clientChannel.Send(
                clientConnection.Id,
                batch));

        serverChannel.Update();

        Assert.True(
            serverChannel.TryGet(
                serverConnection.Id,
                TickFromInt(42),
                out var commands));

        Assert.Equal(
            2,
            commands.Count);

        var firstCommand =
            Assert.IsType<AddValueCommand>(
                commands[0]);

        var secondCommand =
            Assert.IsType<AddValueCommand>(
                commands[1]);

        Assert.Equal(
            10,
            firstCommand.Value);

        Assert.Equal(
            20,
            secondCommand.Value);
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