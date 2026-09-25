using Engine.Core.Commands;
using Engine.Core.Events;
using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.Simulations;
using Xunit;

namespace Engine.Tests.Simulations;

public sealed class DeterminismTests
{
    [Fact]
    public void SameInput_ProducesSameResult()
    {
        var first =
            RunSimulation(1000);

        var second =
            RunSimulation(1000);

        Assert.Equal(
            first.Position,
            second.Position);

        Assert.Equal(
            first.Velocity,
            second.Velocity);
    }

    private static DeterministicSimulationState RunSimulation(
        int ticks)
    {
        var state =
            new DeterministicSimulationState
            {
                Position =
                    FixedVector2.Zero,

                Velocity =
                    new FixedVector2(
                        Fixed32.FromInt(3),
                        Fixed32.FromInt(2))
            };

        var delta =
            Fixed32.FromRatio(1, 60);

        for (var tick = 0;
             tick < ticks;
             tick++)
        {
            state.Position =
                state.Position +
                state.Velocity *
                delta;

            if (tick == 300)
            {
                state.Velocity =
                    state.Velocity +
                    new FixedVector2(
                        Fixed32.FromInt(1),
                        Fixed32.FromInt(-1));
            }
        }

        return state;
    }

    [Fact]
    public void FixedDelta_IsDeterministic()
    {
        var delta =
            Fixed32.FromRatio(1, 60);

        var first =
            FixedVector2.Zero;

        var second =
            FixedVector2.Zero;

        var velocity =
            new FixedVector2(
                Fixed32.FromInt(3),
                Fixed32.FromInt(2));

        for (var i = 0; i < 600; i++)
        {
            first += velocity * delta;
        }

        for (var i = 0; i < 600; i++)
        {
            second += velocity * delta;
        }

        Assert.Equal(
            first,
            second);
    }

    [Fact]
    public void SameSimulation_ProducesSameResult()
    {
        var first =
            CreateSystem();

        var second =
            CreateSystem();

        var delta =
            Fixed32.FromRatio(1, 60);

        for (ulong i = 0; i < 10_000; i++)
        {
            var context =
                new FixedSystemContext(
                    new SimulationTime(
                        delta,
                        new Tick(i + 1)));

            first.FixedUpdate(context);
            second.FixedUpdate(context);
        }

        Assert.Equal(
            first.Position,
            second.Position);

        Assert.Equal(
            first.Velocity,
            second.Velocity);
    }

    private static MovementTestSystem CreateSystem()
    {
        return new MovementTestSystem
        {
            Velocity =
                new FixedVector2(
                    Fixed32.FromInt(3),
                    Fixed32.FromInt(2))
        };
    }

    [Fact]
    public void Scheduler_ProducesDeterministicResult()
    {
        var first =
            CreateScheduler(
                out var firstSystem);

        var second =
            CreateScheduler(
                out var secondSystem);

        var delta =
            Fixed32.FromRatio(1, 60);

        for (ulong i = 0; i < 10_000; i++)
        {
            var context =
                new FixedSystemContext(
                    new SimulationTime(
                        delta,
                        new Tick(i + 1)));

            first.FixedUpdate(context);
            second.FixedUpdate(context);
        }

        Assert.Equal(
            firstSystem.Position,
            secondSystem.Position);
    }

    private static SystemScheduler CreateScheduler(
        out MovementTestSystem system)
    {
        system =
            new MovementTestSystem
            {
                Velocity =
                    new FixedVector2(
                        Fixed32.FromInt(3),
                        Fixed32.FromInt(2))
            };

        var scheduler =
            new SystemScheduler();

        scheduler.Add(
            system,
            SystemPhase.FixedUpdate);

        scheduler.Build();

        return scheduler;
    }

    [Fact]
    public void SameSimulationInput_ProducesSameResult()
    {
        var first =
            CreateSimulation(
                out var firstSystem);

        var second =
            CreateSimulation(
                out var secondSystem);

        first.Initialize();
        second.Initialize();

        var delta =
            Fixed32.FromRatio(
                1,
                60);

        const int ticks = 10_000;

        for (var i = 0; i < ticks; i++)
        {
            var time =
                new SimulationTime(
                    delta,
                    new Tick(
                        (ulong)(i + 1)));

            var context =
                new FixedSystemContext(
                    time);

            first.FixedUpdate(context);
            second.FixedUpdate(context);
        }

        Assert.Equal(
            firstSystem.Position,
            secondSystem.Position);

        Assert.Equal(
            firstSystem.Velocity,
            secondSystem.Velocity);

        first.Shutdown();
        second.Shutdown();
    }

    private static Simulation CreateSimulation(
        out MovementTestSystem movementSystem)
    {
        movementSystem =
            new MovementTestSystem
            {
                Velocity =
                    new FixedVector2(
                        Fixed32.FromInt(3),
                        Fixed32.FromInt(2))
            };

        var scheduler =
            new SystemScheduler();

        scheduler.Add(
            movementSystem,
            SystemPhase.FixedUpdate);

        return new Simulation(
            scheduler,
            new CommandQueue(),
            new EventBus());
    }
}