using Engine.Core.Determinism;
using Engine.ECS;

namespace Engine.Tests.ECS.Determinism;

public sealed class ComponentStorageDeterminismTests
{
    [Fact]
    public void Hash_IsIndependentOfDenseStorageOrder()
    {
        using var world =
            new World();

        var first =
            world.CreateEntity();

        var second =
            world.CreateEntity();

        var third =
            world.CreateEntity();

        world.Add(
            first,
            new TestComponent(10));

        world.Add(
            second,
            new TestComponent(20));

        world.Add(
            third,
            new TestComponent(30));

        var initialHash =
            CalculateHash(
                world);

        Assert.True(
            world.Remove<TestComponent>(
                first));

        world.Add(
            first,
            new TestComponent(10));

        var reorderedHash =
            CalculateHash(
                world);

        Assert.Equal(
            initialHash,
            reorderedHash);
    }

    private static DeterministicStateHash CalculateHash(
        World world)
    {
        var hasher =
            DeterministicStateHasher.Create();

        world.AddToHash(
            ref hasher);

        return hasher.GetHash();
    }

    private readonly record struct TestComponent(
        int Value) :
        IDeterministicState
    {
        public void AddToHash(
            ref DeterministicStateHasher hasher)
        {
            hasher.AddInt32(
                Value);
        }
    }
}