using Engine.ECS.Entities;
using Engine.Jobs.Scheduling;

namespace Engine.Tests.ECS.Jobs;

public sealed class WorldParallelJobTests
{
    [Fact]
    public void ScheduleParallel_PairJob_ProcessesOnlyEntitiesWithBothComponents()
    {
        using var world =
            new Engine.ECS.World();

        using var scheduler =
            new JobScheduler(
                workerCount: 2);

        var firstOnly =
            world.CreateEntity();

        var secondOnly =
            world.CreateEntity();

        world.Add(
            firstOnly,
            new FirstComponent());

        world.Add(
            secondOnly,
            new SecondComponent());

        var processed =
            0;

        var handle =
            world.ScheduleParallel<
                FirstComponent,
                SecondComponent>(
                scheduler,
                (
                    EntityId entity,
                    ref FirstComponent first,
                    ref SecondComponent second) =>
                {
                    Interlocked.Increment(
                        ref processed);
                });

        scheduler.Wait(
            handle);

        Assert.Equal(
            0,
            processed);
    }

    private struct FirstComponent
    {
    }

    private struct SecondComponent
    {
    }

    [Fact]
    public void ScheduleParallel_ProcessesAllComponents()
    {
        using var world =
            new Engine.ECS.World();

        using var scheduler =
            new JobScheduler(
                workerCount: 4);

        const int entityCount = 10_000;

        for (var i = 0;
             i < entityCount;
             i++)
        {
            var entity =
                world.CreateEntity();

            world.Add(
                entity,
                new TestComponent());
        }

        var handle =
            world.ScheduleParallel(
                scheduler,
                (
                    EntityId entity,
                    ref TestComponent component) =>
                {
                    component.Value++;
                },
                batchSize: 64);

        scheduler.Wait(
            handle);

        var processed =
            0;

        foreach (var item in
                 world.Query<TestComponent>())
        {
            Assert.Equal(
                1,
                item.Component.Value);

            processed++;
        }

        Assert.Equal(
            entityCount,
            processed);
    }

    private struct TestComponent
    {
        public int Value;
    }
}