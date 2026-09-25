using Engine.Core.Systems;
using Engine.ECS;
using Engine.ECS.Entities;
using Engine.Jobs.Integration;
using Engine.Jobs.Scheduling;

namespace Engine.Tests.ECS.Jobs;

public sealed class WorldSystemJobIntegrationTests
{
    [Fact]
    public void SystemJob_WaitsForEcsParallelJobs()
    {
        using var world =
            new World();

        using var scheduler =
            new JobScheduler(
                workerCount: 1);

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new TestComponent());

        var strategy =
            new JobSystemExecutionStrategy(
                scheduler);

        var systems =
            new SystemScheduler(
                strategy);

        systems.Add(
            new TestSystem(
                world,
                scheduler),
            SystemPhase.FixedUpdate);

        systems.Build();

        systems.FixedUpdate(
            default);

        ref var component =
            ref world.Get<TestComponent>(
                entity);

        Assert.Equal(
            1,
            component.Value);
    }

    private sealed class TestSystem :
        IFixedUpdateSystem
    {
        private readonly World _world;

        private readonly JobScheduler _scheduler;

        public TestSystem(
            World world,
            JobScheduler scheduler)
        {
            _world =
                world;

            _scheduler =
                scheduler;
        }

        public void FixedUpdate(
            FixedSystemContext context)
        {
            _world.ScheduleParallel(
                _scheduler,
                (
                    EntityId entity,
                    ref TestComponent component) =>
                {
                    component.Value++;
                });
        }
    }

    private struct TestComponent
    {
        public int Value;
    }
}