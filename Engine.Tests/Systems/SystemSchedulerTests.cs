using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.Tests.Simulations;

namespace Engine.Tests.Systems;

public sealed class SystemSchedulerTests
{
    [Fact]
    public void RegistrationCannotBeModifiedAfterBuild()
    {
        var scheduler =
            new SystemScheduler();

        var registration =
            scheduler.Add(
                new LockTestSystemA(),
                SystemPhase.Update);

        scheduler.Add(
            new LockTestSystemB(),
            SystemPhase.Update);

        scheduler.Build();

        Assert.Throws<InvalidOperationException>(
            () =>
                registration.After<LockTestSystemB>());

        Assert.Throws<InvalidOperationException>(
            () =>
                registration.Before<LockTestSystemB>());
    }

    private sealed class LockTestSystemA :
    IUpdateSystem
    {
        public void Update(
            SystemContext context)
        {
        }
    }

    private sealed class LockTestSystemB :
        IUpdateSystem
    {
        public void Update(
            SystemContext context)
        {
        }
    }

    [Fact]
    public void SystemsExecuteAccordingToDependencies()
    {
        var execution = new List<string>();

        var scheduler = new SystemScheduler();

        scheduler.Add(
            new StatisticsSystem(execution),
            SystemPhase.FixedUpdate)
            .After<ProductionSystem>()
            .After<LogisticsSystem>();

        scheduler.Add(
            new ProductionSystem(execution),
            SystemPhase.FixedUpdate)
            .After<BuildingSystem>();

        scheduler.Add(
            new LogisticsSystem(execution),
            SystemPhase.FixedUpdate)
            .After<BuildingSystem>();

        scheduler.Add(
            new BuildingSystem(execution),
            SystemPhase.FixedUpdate);

        scheduler.Build();

        scheduler.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromRatio(1, 20),
                    new Tick(1))));

        Assert.Equal(
            new[]
            {
                "Building",
                "Production",
                "Logistics",
                "Statistics"
            },
            execution);
    }

    [Fact]
    public void IndependentSystemsUseRegistrationOrder()
    {
        var execution = new List<string>();

        var scheduler = new SystemScheduler();

        scheduler.Add(
            new LogisticsSystem(execution),
            SystemPhase.FixedUpdate);

        scheduler.Add(
            new ProductionSystem(execution),
            SystemPhase.FixedUpdate);

        scheduler.Build();

        scheduler.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromRatio(1, 20),
                    new Tick(1))));

        Assert.Equal(
            new[]
            {
                "Logistics",
                "Production"
            },
            execution);
    }

    [Fact]
    public void BeforeCreatesDependency()
    {
        var execution = new List<string>();

        var scheduler = new SystemScheduler();

        scheduler.Add(
            new StatisticsSystem(execution),
            SystemPhase.FixedUpdate);

        scheduler.Add(
            new BuildingSystem(execution),
            SystemPhase.FixedUpdate)
            .Before<StatisticsSystem>();

        scheduler.Build();

        scheduler.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromRatio(1, 20),
                    new Tick(1))));

        Assert.Equal(
            new[]
            {
                "Building",
                "Statistics"
            },
            execution);
    }

    [Fact]
    public void UpdateAndFixedUpdateHaveSeparatePlans()
    {
        var execution = new List<string>();

        var scheduler = new SystemScheduler();

        scheduler.Add(
            new InputSystem(execution),
            SystemPhase.Update);

        scheduler.Add(
            new BuildingSystem(execution),
            SystemPhase.FixedUpdate);

        scheduler.Build();

        scheduler.Update(
            new SystemContext(
                new TimeSnapshot(
                    Duration.FromMilliseconds(16),
                    Duration.FromMilliseconds(16),
                    new Tick(0))));

        Assert.Equal(
            new[]
            {
                "Input"
            },
            execution);

        scheduler.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromRatio(1, 20),
                    new Tick(1))));

        Assert.Equal(
            new[]
            {
                "Input",
                "Building"
            },
            execution);
    }

    [Fact]
    public void MissingDependencyThrows()
    {
        var execution = new List<string>();

        var scheduler = new SystemScheduler();

        scheduler.Add(
            new ProductionSystem(execution),
            SystemPhase.FixedUpdate)
            .After<BuildingSystem>();

        Assert.Throws<InvalidOperationException>(
            () => scheduler.Build());
    }

    [Fact]
    public void CrossPhaseDependencyThrows()
    {
        var execution = new List<string>();

        var scheduler = new SystemScheduler();

        scheduler.Add(
            new InputSystem(execution),
            SystemPhase.Update);

        scheduler.Add(
            new ProductionSystem(execution),
            SystemPhase.FixedUpdate)
            .After<InputSystem>();

        Assert.Throws<InvalidOperationException>(
            () => scheduler.Build());
    }

    [Fact]
    public void CyclicDependencyThrows()
    {
        var execution = new List<string>();

        var scheduler = new SystemScheduler();

        scheduler.Add(
            new BuildingSystem(execution),
            SystemPhase.FixedUpdate)
            .After<StatisticsSystem>();

        scheduler.Add(
            new StatisticsSystem(execution),
            SystemPhase.FixedUpdate)
            .After<BuildingSystem>();

        Assert.Throws<InvalidOperationException>(
            () => scheduler.Build());
    }

    [Fact]
    public void DuplicateSystemTypeThrows()
    {
        var execution = new List<string>();

        var scheduler = new SystemScheduler();

        scheduler.Add(
            new BuildingSystem(execution),
            SystemPhase.FixedUpdate);

        Assert.Throws<InvalidOperationException>(
            () =>
                scheduler.Add(
                    new BuildingSystem(execution),
                    SystemPhase.FixedUpdate));
    }

    [Fact]
    public void ExecuteBeforeBuildThrows()
    {
        var scheduler = new SystemScheduler();

        scheduler.Add(
            new BuildingSystem(new List<string>()),
            SystemPhase.FixedUpdate);

        Assert.Throws<InvalidOperationException>(
            () =>
                scheduler.FixedUpdate(
                    new FixedSystemContext(
                        new SimulationTime(
                            Fixed32.FromRatio(1, 20),
                            new Tick(1)))));
    }

    [Fact]
    public void AddAfterBuildThrows()
    {
        var scheduler = new SystemScheduler();

        scheduler.Add(
            new BuildingSystem(new List<string>()),
            SystemPhase.FixedUpdate);

        scheduler.Build();

        Assert.Throws<InvalidOperationException>(
            () =>
                scheduler.Add(
                    new ProductionSystem(new List<string>()),
                    SystemPhase.FixedUpdate));
    }
}