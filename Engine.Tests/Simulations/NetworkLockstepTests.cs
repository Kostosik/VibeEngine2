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
                clientSession,
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

        Assert.True(
    clientSession.Disconnect(
        clientConnection.Id));

        Assert.False(
            lockstep.TryGetConnection(
                1,
                out _));

        simulation.Shutdown();
    }

    [Fact]
    public void TwoParticipants_ExchangeCommandsAndConvergeToSameStateHash()
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
                "server-convergence",
                1700);

        var clientEndpoint =
            new NetworkEndpoint(
                "client-convergence",
                1701);

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

        using var clientCommandChannel =
            new NetworkCommandChannel(
                clientSession,
                registry);

        using var serverCommandChannel =
            new NetworkCommandChannel(
                serverSession,
                registry);

        using var clientHashChannel =
            new NetworkStateHashChannel(
                clientSession);

        using var serverHashChannel =
            new NetworkStateHashChannel(
                serverSession);

        var clientState =
            new NetworkLockstepState();

        var serverState =
            new NetworkLockstepState();

        var clientSimulation =
            CreateSimulation(
                clientState);

        var serverSimulation =
            CreateSimulation(
                serverState);

        clientSimulation.Initialize();
        serverSimulation.Initialize();

        var clientCoordinator =
            new LockstepCoordinator(
                clientSimulation,
                2);

        var serverCoordinator =
            new LockstepCoordinator(
                serverSimulation,
                2);

        using var clientLockstep =
            new NetworkLockstep(
                clientCoordinator,
                clientSession,
                clientCommandChannel,
                clientHashChannel,
                0);

        using var serverLockstep =
            new NetworkLockstep(
                serverCoordinator,
                serverSession,
                serverCommandChannel,
                serverHashChannel,
                1);

        clientLockstep.AddConnection(
            1,
            clientConnection.Id);

        serverLockstep.AddConnection(
            0,
            serverConnection.Id);

        var tick =
            new Tick(1);

        clientLockstep.SubmitLocal(
            tick,
            new ICommand[]
            {
            new AddValueCommand(1)
            });

        serverLockstep.SubmitLocal(
            tick,
            new ICommand[]
            {
            new AddValueCommand(2)
            });

        var context =
            new FixedSystemContext(
                new SimulationTime(
                    default,
                    tick));

        Assert.True(
            clientLockstep.TryExecute(
                tick,
                context));

        Assert.True(
            serverLockstep.TryExecute(
                tick,
                context));

        Assert.Equal(
            12,
            clientState.Value);

        Assert.Equal(
            12,
            serverState.Value);

        Assert.Equal(
            clientSimulation.GetStateHash(),
            serverSimulation.GetStateHash());

        var nextTick =
            new Tick(2);

        clientLockstep.TryExecute(
            nextTick,
            new FixedSystemContext(
                new SimulationTime(
                    default,
                    nextTick)));

        Assert.Equal(
            clientSimulation.GetStateHash(),
            serverSimulation.GetStateHash());

        clientSimulation.Shutdown();
        serverSimulation.Shutdown();
    }

    [Fact]
    public void TwoParticipants_DifferentState_ReportsDesync()
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
                "server-desync",
                1800);

        var clientEndpoint =
            new NetworkEndpoint(
                "client-desync",
                1801);

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

        using var clientCommandChannel =
            new NetworkCommandChannel(
                clientSession,
                registry);

        using var serverCommandChannel =
            new NetworkCommandChannel(
                serverSession,
                registry);

        using var clientHashChannel =
            new NetworkStateHashChannel(
                clientSession);

        using var serverHashChannel =
            new NetworkStateHashChannel(
                serverSession);

        var clientState =
            new NetworkLockstepState();

        var serverState =
            new NetworkLockstepState();

        serverState.Append(
            9);

        var clientSimulation =
            CreateSimulation(
                clientState);

        var serverSimulation =
            CreateSimulation(
                serverState);

        clientSimulation.Initialize();
        serverSimulation.Initialize();

        var clientCoordinator =
            new LockstepCoordinator(
                clientSimulation,
                2);

        var serverCoordinator =
            new LockstepCoordinator(
                serverSimulation,
                2);

        using var clientLockstep =
            new NetworkLockstep(
                clientCoordinator,
                clientSession,
                clientCommandChannel,
                clientHashChannel,
                0);

        using var serverLockstep =
            new NetworkLockstep(
                serverCoordinator,
                serverSession,
                serverCommandChannel,
                serverHashChannel,
                1);

        clientLockstep.AddConnection(
            1,
            clientConnection.Id);

        serverLockstep.AddConnection(
            0,
            serverConnection.Id);

        var detected =
            false;

        clientLockstep.DesyncDetected +=
            (_, _, _, _) =>
            {
                detected = true;
            };

        var tick =
            new Tick(1);

        clientLockstep.SubmitLocal(
            tick,
            new ICommand[]
            {
            new AddValueCommand(1)
            });

        serverLockstep.SubmitLocal(
            tick,
            new ICommand[]
            {
            new AddValueCommand(2)
            });

        var context =
            new FixedSystemContext(
                new SimulationTime(
                    default,
                    tick));

        Assert.True(
            clientLockstep.TryExecute(
                tick,
                context));

        Assert.True(
            serverLockstep.TryExecute(
                tick,
                context));

        var nextTick =
            new Tick(2);

        Assert.False(
            clientLockstep.TryExecute(
                nextTick,
                new FixedSystemContext(
                    new SimulationTime(
                        default,
                        nextTick))));

        Assert.True(
            detected);

        Assert.NotEqual(
            clientSimulation.GetStateHash(),
            serverSimulation.GetStateHash());

        clientSimulation.Shutdown();
        serverSimulation.Shutdown();
    }

    [Fact]
    public void AddConnection_RejectsUnknownConnection()
    {
        using var transport =
            new LoopbackTransport();

        using var session =
            new NetworkSession(
                transport);

        session.Start(
            new NetworkEndpoint(
                "lockstep-owner",
                1900));

        var simulation =
            CreateSimulation(
                new NetworkLockstepState());

        simulation.Initialize();

        var coordinator =
            new LockstepCoordinator(
                simulation,
                2);

        var registry =
            new ReplayCommandRegistry();

        registry.Register<AddValueCommand>(
            "add_value");

        using var commandChannel =
            new NetworkCommandChannel(
                session,
                registry);

        using var hashChannel =
            new NetworkStateHashChannel(
                session);

        using var lockstep =
            new NetworkLockstep(
                coordinator,
                session,
                commandChannel,
                hashChannel,
                0);

        Assert.Throws<ArgumentException>(
            () =>
                lockstep.AddConnection(
                    1,
                    new ConnectionId(123)));

        simulation.Shutdown();
    }

    [Fact]
    public void TryExecute_NonNextTick_DoesNotPrepareRemoteCommands()
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
                "future-server",
                1920);

        var clientEndpoint =
            new NetworkEndpoint(
                "future-client",
                1921);

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

        using var hashChannel =
            new NetworkStateHashChannel(
                clientSession);

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

        using var lockstep =
            new NetworkLockstep(
                coordinator,
                clientSession,
                clientChannel,
                hashChannel,
                0);

        lockstep.AddConnection(
            1,
            clientConnection.Id);

        var futureTick =
            new Tick(2);

        var futureBatch =
            new NetworkCommandBatch(
                futureTick);

        futureBatch.Add(
            new AddValueCommand(2));

        Assert.True(
            serverChannel.Send(
                serverConnection.Id,
                futureBatch));

        clientSession.Update();

        var context =
            new FixedSystemContext(
                new SimulationTime(
                    default,
                    futureTick));

        Assert.Throws<InvalidOperationException>(
            () =>
                lockstep.TryExecute(
                    futureTick,
                    context));

        var currentTick =
            new Tick(1);

        lockstep.SubmitLocal(
            currentTick,
            Array.Empty<ICommand>());

        Assert.False(
            lockstep.TryExecute(
                currentTick,
                new FixedSystemContext(
                    new SimulationTime(
                        default,
                        currentTick))));

        var currentBatch =
            new NetworkCommandBatch(
                currentTick);

        Assert.True(
            serverChannel.Send(
                serverConnection.Id,
                currentBatch));

        Assert.True(
            lockstep.TryExecute(
                currentTick,
                new FixedSystemContext(
                    new SimulationTime(
                        default,
                        currentTick))));

        lockstep.SubmitLocal(
            futureTick,
            Array.Empty<ICommand>());

        Assert.True(
            lockstep.TryExecute(
                futureTick,
                context));

        Assert.Equal(
            2,
            state.Value);

        simulation.Shutdown();
    }

    [Fact]
    public void TryExecute_RemovesLateCommandBatchFromAlreadyExecutedTick()
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

        serverSession.Start(
            new NetworkEndpoint(
                "stale-command-server",
                1930));

        clientSession.Start(
            new NetworkEndpoint(
                "stale-command-client",
                1931));

        var clientConnection =
            clientSession.Connect(
                serverSession.LocalEndpoint);

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

        using var clientHashChannel =
            new NetworkStateHashChannel(
                clientSession);

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

        using var lockstep =
            new NetworkLockstep(
                coordinator,
                clientSession,
                clientChannel,
                clientHashChannel,
                0);

        lockstep.AddConnection(
            1,
            clientConnection.Id);

        var tick1 =
            new Tick(1);

        lockstep.SubmitLocal(
            tick1,
            Array.Empty<ICommand>());

        var tick1Batch =
            new NetworkCommandBatch(
                tick1);

        Assert.True(
            serverChannel.Send(
                serverConnection.Id,
                tick1Batch));

        Assert.True(
            lockstep.TryExecute(
                tick1,
                new FixedSystemContext(
                    new SimulationTime(
                        default,
                        tick1))));

        var staleBatch =
            new NetworkCommandBatch(
                tick1);

        staleBatch.Add(
            new AddValueCommand(9));

        Assert.True(
            serverChannel.Send(
                serverConnection.Id,
                staleBatch));

        var tick2 =
            new Tick(2);

        lockstep.SubmitLocal(
            tick2,
            Array.Empty<ICommand>());

        var tick2Batch =
            new NetworkCommandBatch(
                tick2);

        Assert.True(
            serverChannel.Send(
                serverConnection.Id,
                tick2Batch));

        Assert.True(
            lockstep.TryExecute(
                tick2,
                new FixedSystemContext(
                    new SimulationTime(
                        default,
                        tick2))));

        Assert.False(
            clientChannel.TryGet(
                clientConnection.Id,
                tick1,
                out _));

        Assert.False(
            clientChannel.TryGet(
                clientConnection.Id,
                tick2,
                out _));

        simulation.Shutdown();
    }

    [Fact]
    public void DisconnectBeforeRemoteCommands_BlocksCurrentTick()
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

        serverSession.Start(
            new NetworkEndpoint(
                "disconnect-lockstep-server",
                1950));

        clientSession.Start(
            new NetworkEndpoint(
                "disconnect-lockstep-client",
                1951));

        var clientConnection =
            clientSession.Connect(
                serverSession.LocalEndpoint);

        serverSession.Update();

        Assert.Single(
            serverSession.Connections);

        var registry =
            new ReplayCommandRegistry();

        registry.Register<AddValueCommand>(
            "add_value");

        using var clientCommandChannel =
            new NetworkCommandChannel(
                clientSession,
                registry);

        using var hashChannel =
            new NetworkStateHashChannel(
                clientSession);

        var state =
            new NetworkLockstepState();

        var simulation =
            CreateSimulation(
                state);

        simulation.RegisterDeterministicState(
            state);

        simulation.Initialize();

        var coordinator =
            new LockstepCoordinator(
                simulation,
                2);

        using var lockstep =
            new NetworkLockstep(
                coordinator,
                clientSession,
                clientCommandChannel,
                hashChannel,
                0);

        lockstep.AddConnection(
            1,
            clientConnection.Id);

        var tick =
            new Tick(1);

        lockstep.SubmitLocal(
            tick,
            Array.Empty<ICommand>());

        Assert.True(
            clientSession.Disconnect(
                clientConnection.Id));

        Assert.False(
            lockstep.TryGetConnection(
                1,
                out _));

        Assert.False(
            lockstep.TryExecute(
                tick,
                new FixedSystemContext(
                    new SimulationTime(
                        default,
                        tick))));

        Assert.Equal(
            Tick.Zero,
            coordinator.LastExecutedTick);

        Assert.Equal(
            0,
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

        simulation.RegisterDeterministicState(
            state);

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