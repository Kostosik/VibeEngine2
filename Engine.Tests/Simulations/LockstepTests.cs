using Engine.Core.Commands;
using Engine.Core.Determinism;
using Engine.Core.Events;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.Simulations;
using Engine.Simulations.Lockstep;

namespace Engine.Tests.Simulations;

public sealed class LockstepTests
{
    [Fact]
    public void Execute_UsesDeterministicParticipantOrder()
    {
        var stateA =
            new LockstepState();

        var stateB =
            new LockstepState();

        var simulationA =
            CreateSimulation(
                stateA);

        var simulationB =
            CreateSimulation(
                stateB);

        simulationA.Initialize();
        simulationB.Initialize();

        var lockstepA =
            new LockstepCoordinator(
                simulationA,
                2);

        var lockstepB =
            new LockstepCoordinator(
                simulationB,
                2);

        var tick =
            TickFromInt(1);

        var context =
            new FixedSystemContext(
                new SimulationTime(
                    default,
                    tick));

        var participantZeroCommand =
            new SetSequenceCommand(
                1);

        var participantOneCommand =
            new SetSequenceCommand(
                2);

        lockstepA.Submit(
            tick,
            1,
            new ICommand[]
            {
                participantOneCommand
            });

        lockstepA.Submit(
            tick,
            0,
            new ICommand[]
            {
                participantZeroCommand
            });

        lockstepB.Submit(
            tick,
            0,
            new ICommand[]
            {
                participantZeroCommand
            });

        lockstepB.Submit(
            tick,
            1,
            new ICommand[]
            {
                participantOneCommand
            });

        Assert.True(
            lockstepA.IsReady(
                tick));

        Assert.True(
            lockstepB.IsReady(
                tick));

        Assert.True(
            lockstepA.TryExecute(
                tick,
                context));

        Assert.True(
            lockstepB.TryExecute(
                tick,
                context));

        Assert.Equal(
            12,
            stateA.Value);

        Assert.Equal(
            12,
            stateB.Value);

        var hashA =
            GetHash(
                stateA);

        var hashB =
            GetHash(
                stateB);

        Assert.Equal(
            hashA,
            hashB);

        simulationA.Shutdown();
        simulationB.Shutdown();
    }

    private static Simulation CreateSimulation(
        LockstepState state)
    {
        var scheduler =
            new SystemScheduler();

        var commands =
            new CommandQueue();

        var events =
            new EventBus();

        var simulation =
            new Simulation(
                scheduler,
                commands,
                events);

        simulation.CommandDispatcher.Register(
            new SetSequenceCommandHandler(
                state));

        return simulation;
    }

    private static DeterministicStateHash GetHash(
        LockstepState state)
    {
        var hasher =
            DeterministicStateHasher.Create();

        state.AddToHash(
            ref hasher);

        return hasher.GetHash();
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

    private sealed class SetSequenceCommand
        : ICommand
    {
        public SetSequenceCommand(
            int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private sealed class SetSequenceCommandHandler
        : ICommandHandler<SetSequenceCommand>
    {
        private readonly LockstepState _state;

        public SetSequenceCommandHandler(
            LockstepState state)
        {
            _state = state;
        }


        public void Handle(
            SetSequenceCommand command)
        {
            _state.Append(
                command.Value);
        }
    }

    private sealed class LockstepState
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