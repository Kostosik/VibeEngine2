using Engine.Core.Determinism;
using Engine.ECS;
using Engine.ECS.Entities;
using Engine.Jobs.Scheduling;

namespace Engine.Tests.ECS.Determinism;

public sealed class WorldDeterminismHashTests
{
    [Fact]
    public void IdenticalWorlds_ProduceIdenticalHashes()
    {
        using var first = CreateWorld();
        using var second = CreateWorld();

        Assert.Equal(
            CalculateHash(first),
            CalculateHash(second));
    }

    [Fact]
    public void DeterministicComponentChange_ChangesHash()
    {
        using var first = new World();
        using var second = new World();

        var firstEntity = first.CreateEntity();
        var secondEntity = second.CreateEntity();

        first.Add(firstEntity, new PositionComponent(10));
        second.Add(secondEntity, new PositionComponent(20));

        Assert.NotEqual(
            CalculateHash(first),
            CalculateHash(second));
    }

    [Fact]
    public void StorageCreationOrder_DoesNotChangeHash()
    {
        using var first = new World();
        using var second = new World();

        var firstEntity = first.CreateEntity();
        var secondEntity = second.CreateEntity();

        first.Add(firstEntity, new PositionComponent(10));
        first.Add(firstEntity, new HealthComponent(100));

        second.Add(secondEntity, new HealthComponent(100));
        second.Add(secondEntity, new PositionComponent(10));

        Assert.Equal(
            CalculateHash(first),
            CalculateHash(second));
    }

    [Fact]
    public void NonDeterministicComponent_DoesNotChangeHash()
    {
        using var world = new World();

        var entity = world.CreateEntity();

        var initialHash = CalculateHash(world);

        world.Add(entity, new VisualComponent(123));

        Assert.Equal(
            initialHash,
            CalculateHash(world));
    }

    [Fact]
    public void EntityDestruction_ChangesHash()
    {
        using var first = new World();
        using var second = new World();

        var firstEntity = first.CreateEntity();
        var secondEntity = second.CreateEntity();

        first.DestroyEntity(firstEntity);

        Assert.NotEqual(
            CalculateHash(first),
            CalculateHash(second));
    }

    [Fact]
    public void AddToHash_RejectsRunningParallelJobs()
    {
        using var world = new World();
        using var scheduler = new JobScheduler(workerCount: 1);
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();

        var entity = world.CreateEntity();
        world.Add(entity, new BlockingComponent());

        var handle = world.ScheduleParallel(
            scheduler,
            (
                EntityId id,
                ref BlockingComponent component) =>
            {
                started.Set();
                release.Wait();
                component.Value++;
            });

        try
        {
            Assert.True(
                started.Wait(TimeSpan.FromSeconds(5)));

            Assert.Throws<InvalidOperationException>(() =>
            {
                var hasher = DeterministicStateHasher.Create();
                world.AddToHash(ref hasher);
            });
        }
        finally
        {
            release.Set();
            scheduler.Wait(handle);
        }
    }

    private static World CreateWorld()
    {
        var world = new World();

        var first = world.CreateEntity();
        var second = world.CreateEntity();

        world.Add(first, new PositionComponent(10));
        world.Add(second, new PositionComponent(20));

        world.Add(first, new HealthComponent(100));
        world.Add(second, new HealthComponent(80));

        return world;
    }

    private static DeterministicStateHash CalculateHash(World world)
    {
        var hasher = DeterministicStateHasher.Create();

        world.AddToHash(ref hasher);

        return hasher.GetHash();
    }

    private readonly record struct PositionComponent(int Value)
        : IDeterministicState
    {
        public void AddToHash(ref DeterministicStateHasher hasher)
        {
            hasher.AddInt32(Value);
        }
    }

    private readonly record struct HealthComponent(int Value)
        : IDeterministicState
    {
        public void AddToHash(ref DeterministicStateHasher hasher)
        {
            hasher.AddInt32(Value);
        }
    }

    private readonly record struct VisualComponent(int Value);

    private struct BlockingComponent
    {
        public int Value;
    }
}