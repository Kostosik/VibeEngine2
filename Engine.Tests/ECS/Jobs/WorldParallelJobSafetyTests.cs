using Engine.ECS;
using Engine.ECS.Entities;
using Engine.Jobs.Scheduling;

namespace Engine.Tests.ECS.Jobs;

public sealed class WorldParallelJobSafetyTests
{
    [Fact]
    public void WorldRejectsStructuralAccessWhileParallelJobIsRunning()
    {
        using var world =
            new World();

        using var scheduler =
            new JobScheduler(
                workerCount: 1);

        using var started =
            new ManualResetEventSlim();

        using var release =
            new ManualResetEventSlim();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new TestComponent());

        var handle =
            world.ScheduleParallel(
                scheduler,
                (
                    EntityId entity,
                    ref TestComponent component) =>
                {
                    started.Set();

                    release.Wait();

                    component.Value++;
                });

        Assert.True(
            started.Wait(
                TimeSpan.FromSeconds(5)));

        Assert.Throws<InvalidOperationException>(
            () =>
                world.CreateEntity());

        Assert.Throws<InvalidOperationException>(
            () =>
                world.Get<TestComponent>(
                    entity));

        release.Set();

        scheduler.Wait(
            handle);

        ref var result =
            ref world.Get<TestComponent>(
                entity);

        Assert.Equal(
            1,
            result.Value);
    }

    private struct TestComponent
    {
        public int Value;
    }
}