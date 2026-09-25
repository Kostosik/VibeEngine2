using Engine.Core.Determinism;
using Engine.Core.Events;
using Engine.Core.Simulations;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS;
using Engine.Simulations;

namespace Engine.Tests.Simulations;

public sealed class DeterministicSimulationTests
{
    [Fact]
    public void FixedSimulation_WithSameWorldAndCommands_ProducesSameState()
    {
        const int tickCount = 10;

        var worldA =
            new Engine.ECS.World();

        var worldB =
            new Engine.ECS.World();

        var executionA =
            new List<string>();

        var executionB =
            new List<string>();

        var simulationA =
            CreateSimulation(
                worldA,
                executionA);

        var simulationB =
            CreateSimulation(
                worldB,
                executionB);

        simulationA.Initialize();
        simulationB.Initialize();

        for (var i = 1; i <= tickCount; i++)
        {
            var time =
                new SimulationTime(
                    default,
                    Tick.Zero);

            var context =
                new FixedSystemContext(
                    time);

            simulationA.FixedUpdate(context);
            simulationB.FixedUpdate(context);
        }

        Assert.Equal(
            tickCount,
            worldA.EntityCount);

        Assert.Equal(
            tickCount,
            worldB.EntityCount);

        Assert.Equal(
            worldA.EntityCount,
            worldB.EntityCount);

        Assert.Equal(
            executionA,
            executionB);

        Assert.Equal(
            tickCount * 2,
            executionA.Count);

        var expectedExecution =
            new List<string>();

        for (var i = 0; i < tickCount; i++)
        {
            expectedExecution.Add("Produce");
            expectedExecution.Add("Apply");
        }

        Assert.Equal(
            expectedExecution,
            executionA);

        var hashA =
            simulationA.GetStateHash();

        var hashB =
            simulationB.GetStateHash();

        Assert.Equal(
            hashA,
            hashB);

        simulationA.Shutdown();
        simulationB.Shutdown();
    }

    private static Simulation CreateSimulation(
        Engine.ECS.World world,
        List<string> execution)
    {
        var scheduler =
            new SystemScheduler();

        var commands =
            new Engine.Core.Commands.CommandQueue();

        var events =
            new EventBus();

        var producer =
            new DeterministicCommandProducerSystem(
                world,
                execution);

        var applier =
            new DeterministicCommandApplySystem(
                world,
                execution);

        scheduler.Add(
            producer,
            SystemPhase.FixedUpdate);

        scheduler
            .Add(
                applier,
                SystemPhase.FixedUpdate)
            .After<
                DeterministicCommandProducerSystem>();

        var simulation =
            new Simulation(
                scheduler,
                commands,
                events);

        simulation.RegisterDeterministicState(
            world);

        return simulation;
    }

    private sealed class DeterministicCommandProducerSystem
        : IFixedUpdateSystem
    {
        private readonly Engine.ECS.World _world;
        private readonly List<string> _execution;

        public DeterministicCommandProducerSystem(
            Engine.ECS.World world,
            List<string> execution)
        {
            _world = world;
            _execution = execution;
        }

        public void FixedUpdate(
            FixedSystemContext context)
        {
            _execution.Add("Produce");

            _world.Commands.CreateEntity();
        }
    }

    private sealed class DeterministicCommandApplySystem
        : IFixedUpdateSystem
    {
        private readonly Engine.ECS.World _world;
        private readonly List<string> _execution;

        public DeterministicCommandApplySystem(
            Engine.ECS.World world,
            List<string> execution)
        {
            _world = world;
            _execution = execution;
        }

        public void FixedUpdate(
            FixedSystemContext context)
        {
            _execution.Add("Apply");

            _world.ApplyCommands();
        }
    }
}