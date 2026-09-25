using Engine.Core.Commands;
using Engine.Core.Determinism;
using Engine.Core.Events;
using Engine.Core.Replays;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.Networking.Connections;
using Engine.Networking.Sessions;
using Engine.Networking.Simulation;
using Engine.Networking.Transport;
using Engine.Simulations;
using Engine.Simulations.Lockstep;

namespace Engine.Tests.Simulations;

public sealed class NetworkLockstepTests
{
    [Fact]
    public void SubmitLocalAndReceiveRemote_ExecutesTickDeterministically()
    {
        using var clientTransport =
            new LoopbackTransport();

        using var serverTransport =
            new LoopbackTransport();

        using var clientSession =
            new NetworkSession(
                clientTransport);

        using var serverSession =
            new NetworkSession(
                serverTransport);

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

        using var clientChannel =
            new NetworkCommandChannel(
                clientSession,
                registry);

        using var serverChannel =
            new NetworkCommandChannel(
                serverSession,
                registry);

        var state =
            new NetworkLockstepState();

        var simulation =
            CreateSimulation(
                state);

        simulation.Initialize();

        var coordinator =
            new LockstepCoordinator(
                simulation,
                2);

        using var hashChannel =
            new NetworkStateHashChannel(
                clientSession);

        using var lockstep =
            new NetworkLockstep(
                coordinator,
                clientChannel,
                hashChannel,
                0);

        lockstep.AddConnection(
            1,
            clientConnection.Id);

        var tick =
            TickFromInt(1);

        lockstep.SubmitLocal(
            tick,
            new ICommand[]
            {
                new AddValueCommand(1)
            });

        var context =
            new FixedSystemContext(
                new SimulationTime(
                    default,
                    tick));

        Assert.False(
            lockstep.TryExecute(
                tick,
                context));

        var remoteBatch =
            new NetworkCommandBatch(
                tick);

        remoteBatch.Add(
            new AddValueCommand(2));

        Assert.True(
            serverChannel.Send(
                serverConnection.Id,
                remoteBatch));

        Assert.True(
            lockstep.TryExecute(
                tick,
                context));

        Assert.Equal(
            12,
            state.Value);

        simulation.Shutdown();
    }

    private static Simulation CreateSimulation(
        NetworkLockstepState state)
    {
        var scheduler =
            new SystemScheduler();

        var commands =
            new Engine.Core.Commands.CommandQueue();

        var events =
            new EventBus();

        var simulation =
            new Simulation(
                scheduler,
                commands,
                events);

        simulation.CommandDispatcher.Register(
            new AddValueCommandHandler(
                state));

        return simulation;
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

    private sealed class AddValueCommandHandler
        : ICommandHandler<AddValueCommand>
    {
        private readonly NetworkLockstepState _state;

        public AddValueCommandHandler(
            NetworkLockstepState state)
        {
            _state = state;
        }

        public void Handle(
            AddValueCommand command)
        {
            _state.Append(
                command.Value);
        }
    }

    private sealed class NetworkLockstepState
        : IDeterministicState
    {
        public int Value { get; private set; }

        public void Append(
            int value)
        {
            Value =
                Value * 10 +
                value;
        }

        public void AddToHash(
            ref DeterministicStateHasher hasher)
        {
            hasher.AddInt32(
                Value);
        }
    }
}