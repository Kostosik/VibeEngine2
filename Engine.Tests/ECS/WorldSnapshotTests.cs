using Engine.Core.Determinism;
using Engine.ECS.Components;
using Engine.ECS.Entities;

namespace Engine.Tests.ECS;

public sealed class WorldSnapshotTests
{
    [Fact]
    public void RestoreSnapshot_RestoresExactWorldState()
    {
        var world =
            new Engine.ECS.World();

        var first =
            world.CreateEntity();

        var second =
            world.CreateEntity();

        world.Add(
            first,
            new TestComponent
            {
                Value = 10
            });

        world.Add(
            second,
            new TestComponent
            {
                Value = 20
            });

        var beforeHash =
            GetHash(world);

        var snapshot =
            world.CreateSnapshot();

        world.Get<TestComponent>(
            first).Value = 999;

        world.DestroyEntity(
            second);

        var third =
            world.CreateEntity();

        world.Add(
            third,
            new TestComponent
            {
                Value = 300
            });

        world.RestoreSnapshot(
            snapshot);

        var afterHash =
            GetHash(world);

        Assert.Equal(
            beforeHash,
            afterHash);

        Assert.Equal(
            2,
            world.EntityCount);

        Assert.True(
            world.Exists(first));

        Assert.True(
            world.Exists(second));

        Assert.False(
            world.Exists(third));

        Assert.Equal(
            10,
            world.Get<TestComponent>(
                first).Value);

        Assert.Equal(
            20,
            world.Get<TestComponent>(
                second).Value);
    }

    private static DeterministicStateHash GetHash(
        Engine.ECS.World world)
    {
        var hasher =
            DeterministicStateHasher.Create();

        world.AddToHash(
            ref hasher);

        return hasher.GetHash();
    }

    private struct TestComponent
        : IDeterministicState
    {
        public int Value;

        public void AddToHash(
            ref DeterministicStateHasher hasher)
        {
            hasher.AddInt32(
                Value);
        }
    }
}