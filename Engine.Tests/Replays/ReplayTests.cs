using Engine.Core.Commands;
using Engine.Core.Determinism;
using Engine.Core.Events;
using Engine.Core.Replays;
using Engine.Core.Simulations;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.Simulations;

namespace Engine.Tests.Replays;

public sealed class ReplayTests
{
    [Fact]
    public void Replay_ProducesSameStateAfterSaveAndLoad()
    {
        const int tickCount = 10;

        var replay =
            new Replay();

        var worldA =
            new ReplayTestState();

        var worldB =
            new ReplayTestState();

        var simulationA =
            CreateSimulation(
                worldA);

        var simulationB =
            CreateSimulation(
                worldB);

        simulationA.RegisterDeterministicState(
            worldA);

        simulationB.RegisterDeterministicState(
            worldB);

        simulationA.Initialize();
        simulationB.Initialize();

        var replayPath =
            Path.Combine(
                Path.GetTempPath(),
                $"vibeengine-replay-{Guid.NewGuid():N}.json");

        try
        {
            using var recorder =
                new ReplayRecorder(
                    replay,
                    simulationA.CommandDispatcher);

            recorder.Start(
                Tick.Zero);

            var tick =
                Tick.Zero;

            for (var i = 0; i < tickCount; i++)
            {
                tick++;

                recorder.SetTick(
                    tick);

                simulationA.Commands.Enqueue(
                    new AddValueCommand(1));

                simulationA.FixedUpdate(
                    new FixedSystemContext(
                        new SimulationTime(
                            default,
                            tick)));
            }

            recorder.Stop();

            var registry =
                new ReplayCommandRegistry();

            registry.Register<AddValueCommand>(
                "add_value");

            ReplaySerializer.Save(
                replay,
                replayPath,
                registry);

            var loadedReplay =
                ReplaySerializer.Load(
                    replayPath,
                    registry);

            var player =
                new ReplayPlayer(
                    loadedReplay);

            tick =
                Tick.Zero;

            for (var i = 0; i < tickCount; i++)
            {
                tick++;

                player.EnqueueCommands(
                    tick,
                    simulationB.Commands);

                simulationB.FixedUpdate(
                    new FixedSystemContext(
                        new SimulationTime(
                            default,
                            tick)));
            }

            var hashA =
                simulationA.GetStateHash();

            var hashB =
                simulationB.GetStateHash();

            Assert.Equal(
                tickCount,
                worldA.Value);

            Assert.Equal(
                tickCount,
                worldB.Value);

            Assert.Equal(
                hashA,
                hashB);

            Assert.Equal(
                tickCount,
                replay.Frames.Count);

            Assert.Equal(
                replay.Frames.Count,
                loadedReplay.Frames.Count);

            simulationA.Shutdown();
            simulationB.Shutdown();
        }
        finally
        {
            if (File.Exists(replayPath))
            {
                File.Delete(replayPath);
            }
        }
    }

    private static Simulation CreateSimulation(
        ReplayTestState state)
    {
        var scheduler =
            new SystemScheduler();

        var commands =
            new CommandQueue();

        var events =
            new EventBus();

        var handler =
            new AddValueCommandHandler(
                state);

        var simulation =
            new Simulation(
                scheduler,
                commands,
                events);

        simulation.CommandDispatcher.Register(
            handler);

        return simulation;
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
        private readonly ReplayTestState _state;

        public AddValueCommandHandler(
            ReplayTestState state)
        {
            _state = state;
        }

        public void Handle(
            AddValueCommand command)
        {
            _state.Value += command.Value;
        }
    }

    private sealed class ReplayTestState
        : IDeterministicState
    {
        public int Value { get; set; }

        public void AddToHash(
            ref DeterministicStateHasher hasher)
        {
            hasher.AddInt32(
                Value);
        }
    }
}