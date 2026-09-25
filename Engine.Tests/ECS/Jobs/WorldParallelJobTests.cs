using Engine.ECS.Entities;
using Engine.Jobs.Scheduling;

namespace Engine.Tests.ECS.Jobs;

public sealed class WorldParallelJobTests
{
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