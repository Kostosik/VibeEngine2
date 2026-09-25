using Engine.Core.Commands;
using Engine.Core.Determinism;
using Engine.Core.Events;
using Engine.Core.Replays;
using Engine.Core.Simulations;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS;
using Engine.ECS.Entities;
using Engine.Simulations;
using Engine.Simulations.Rollback;

namespace Engine.Tests.Replays;

public sealed class RollbackTests
{
    [Fact]
    public void Rollback_RestoreAndReplay_ProducesSameState()
    {
        const int tickCount = 10;
        const int rollbackTick = 5;

        var world =
            new Engine.ECS.World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new Counter());

        var state =
            new RollbackTestState(
                world,
                entity);

        var replay =
            new Replay();

        var simulation =
            CreateSimulation(
                state);

        simulation.RegisterDeterministicState(
            world);

        simulation.Initialize();

        using var recorder =
            new ReplayRecorder(
                replay,
                simulation.CommandDispatcher);

        recorder.Start(
            Tick.Zero);

        var rollbackBuffer =
            new RollbackBuffer(
                world,
                tickCount);

        var tick =
            Tick.Zero;

        DeterministicStateHash hashBeforeRollback =
            default;

        for (var i = 0; i < tickCount; i++)
        {
            tick++;

            recorder.SetTick(
                tick);

            simulation.Commands.Enqueue(
                new AddCounterCommand(
                    1));

            simulation.FixedUpdate(
                new FixedSystemContext(
                    new SimulationTime(
                        default,
                        tick)));

            rollbackBuffer.Capture(
                tick);

            if (tick == TickFromInt(
                    rollbackTick))
            {
                hashBeforeRollback =
                    simulation.GetStateHash();
            }
        }

        var finalHash =
            simulation.GetStateHash();

        recorder.Stop();

        Assert.Equal(
            tickCount,
            state.Value);

        Assert.True(
            rollbackBuffer.Restore(
                TickFromInt(
                    rollbackTick)));

        Assert.Equal(
            rollbackTick,
            state.Value);

        var player =
            new ReplayPlayer(
                replay);

        tick =
            TickFromInt(
                rollbackTick);

        while (tick != TickFromInt(
                   tickCount))
        {
            tick++;

            player.EnqueueCommands(
                tick,
                simulation.Commands);

            simulation.FixedUpdate(
                new FixedSystemContext(
                    new SimulationTime(
                        default,
                        tick)));
        }

        var replayedHash =
            simulation.GetStateHash();

        Assert.Equal(
            tickCount,
            state.Value);

        Assert.Equal(
            finalHash,
            replayedHash);

        Assert.NotEqual(
            hashBeforeRollback,
            finalHash);

        simulation.Shutdown();
    }

    private static Simulation CreateSimulation(
        RollbackTestState state)
    {
        var scheduler =
            new SystemScheduler();

        var commands =
            new CommandQueue();

        var events =
            new EventBus();

    simulation:
        var simulation =
            new Simulation(
                scheduler,
                commands,
                events);

        simulation.CommandDispatcher.Register(
            new AddCounterCommandHandler(
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

    private sealed class AddCounterCommand
        : ICommand
    {
        public AddCounterCommand(
            int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private sealed class AddCounterCommandHandler
        : ICommandHandler<AddCounterCommand>
    {
        private readonly RollbackTestState _state;

        public AddCounterCommandHandler(
            RollbackTestState state)
        {
            _state = state;
        }

        public void Handle(
            AddCounterCommand command)
        {
            _state.Value += command.Value;
        }
    }

    private sealed class RollbackTestState
    {
        private readonly Engine.ECS.World _world;
        private readonly EntityId _entity;

        public RollbackTestState(
            Engine.ECS.World world,
            EntityId entity)
        {
            _world = world;
            _entity = entity;
        }

        public int Value
        {
            get => _world.Get<Counter>(
                _entity).Value;

            set => _world.Get<Counter>(
                _entity).Value = value;
        }
    }

    private struct Counter
        : IDeterministicState
    {
        public int Value;

        public void AddToHash(
            ref DeterministicStateHasher hasher)
        {
            hasher.AddInt32(
                Value);
        }
    }
}