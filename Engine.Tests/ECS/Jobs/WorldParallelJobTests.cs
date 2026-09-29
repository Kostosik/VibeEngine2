using Engine.ECS.Entities;
using Engine.Jobs.Scheduling;

namespace Engine.Tests.ECS.Jobs;

public sealed class WorldParallelJobTests
{
    [Fact]
    public void ScheduleParallelReadOnly_DoesNotMarkComponentsDirty()
    {
        using var world =
            new Engine.ECS.World();

        using var scheduler =
            new JobScheduler(
                workerCount: 4);

        const int entityCount =
            10_000;

        for (var i = 0;
             i < entityCount;
             i++)
        {
            var entity =
                world.CreateEntity();

            world.Add(
                entity,
                new TestComponent
                {
                    Value = i
                });
        }

        world.ClearDirty<TestComponent>();

        var versionBefore =
            world.GetChangeVersion<TestComponent>();

        var sum =
            0L;

        var handle =
            world.ScheduleParallelReadOnly(
                scheduler,
                (
                    EntityId entity,
                    in TestComponent component) =>
                {
                    Interlocked.Add(
                        ref sum,
                        component.Value);
                },
                batchSize: 64);

        scheduler.Wait(
            handle);

        Assert.Equal(
            49_995_000,
            sum);

        Assert.Equal(
            0,
            world.GetDirtyEntities<TestComponent>().Length);

        Assert.Equal(
            versionBefore,
            world.GetChangeVersion<TestComponent>());
    }

    [Fact]
    public void ScheduleParallelReadOnly_PairJob_DoesNotMarkComponentsDirty()
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
                new FirstComponent());

            if (i % 2 == 0)
            {
                world.Add(
                    entity,
                    new SecondComponent());
            }
        }

        world.ClearDirty<FirstComponent>();
        world.ClearDirty<SecondComponent>();

        var firstVersion =
            world.GetChangeVersion<FirstComponent>();

        var secondVersion =
            world.GetChangeVersion<SecondComponent>();

        var processed =
            0;

        var handle =
            world.ScheduleParallelReadOnly<
                FirstComponent,
                SecondComponent>(
                    scheduler,
                    (
                        EntityId entity,
                        in FirstComponent first,
                        in SecondComponent second) =>
                    {
                        Interlocked.Increment(
                            ref processed);
                    },
                    batchSize: 64);

        scheduler.Wait(
            handle);

        Assert.Equal(
            entityCount / 2,
            processed);

        Assert.Equal(
            0,
            world.GetDirtyEntities<FirstComponent>().Length);

        Assert.Equal(
            0,
            world.GetDirtyEntities<SecondComponent>().Length);

        Assert.Equal(
            firstVersion,
            world.GetChangeVersion<FirstComponent>());

        Assert.Equal(
            secondVersion,
            world.GetChangeVersion<SecondComponent>());
    }

    [Fact]
    public void ScheduleParallel_TracksAllMutatedComponentsAsDirty()
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

        world.ClearDirty<TestComponent>();

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

        var dirty =
            world.GetDirtyEntities<TestComponent>();

        Assert.Equal(
            entityCount,
            dirty.Length);

        var unique =
            new HashSet<EntityId>();

        foreach (var entity in dirty)
        {
            unique.Add(entity);
        }

        Assert.Equal(
            entityCount,
            unique.Count);


    }

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