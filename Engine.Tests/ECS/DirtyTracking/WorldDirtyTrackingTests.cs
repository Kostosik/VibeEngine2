using Engine.ECS;

namespace Engine.Tests.ECS.DirtyTracking;

public sealed class WorldDirtyTrackingTests
{
    [Fact]
    public void MutableAccess_IsTrackedAndCanBeCleared()
    {
        using var world =
            new World();

        var first =
            world.CreateEntity();

        var second =
            world.CreateEntity();

        world.Add(
            first,
            new TestComponent(10));

        world.Add(
            second,
            new TestComponent(20));

        world.ClearDirty<TestComponent>();

        ref var firstComponent =
            ref world.Get<TestComponent>(
                first);

        firstComponent.Value++;

        var dirty =
            world.GetDirtyEntities<TestComponent>();

        Assert.Equal(
            1,
            dirty.Length);

        Assert.Equal(
            first,
            dirty[0]);

        world.ClearDirty<TestComponent>();

        Assert.Equal(
            0,
            world.GetDirtyEntities<TestComponent>().Length);

        Assert.True(
            world.Remove<TestComponent>(
                second));

        var removed =
            world.GetRemovedEntities<TestComponent>();

        Assert.Equal(
            1,
            removed.Length);

        Assert.Equal(
            second,
            removed[0]);
    }

    [Fact]
    public void ChangeVersion_ChangesWhenMutableAccessBecomesDirty()
    {
        using var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new TestComponent(10));

        var versionAfterAdd =
            world.GetChangeVersion<TestComponent>();

        Assert.NotEqual(
            0UL,
            versionAfterAdd);

        world.ClearDirty<TestComponent>();

        var cleanVersion =
            world.GetChangeVersion<TestComponent>();

        Assert.Equal(
            versionAfterAdd,
            cleanVersion);

        ref var component =
            ref world.Get<TestComponent>(
                entity);

        component.Value++;

        var versionAfterFirstWrite =
            world.GetChangeVersion<TestComponent>();

        Assert.True(
            versionAfterFirstWrite >
            cleanVersion);

        Assert.Equal(
            versionAfterFirstWrite,
            world.GetChangeVersion<TestComponent>());

        world.ClearDirty<TestComponent>();

        var afterClearVersion =
            world.GetChangeVersion<TestComponent>();

        ref var secondComponent =
            ref world.Get<TestComponent>(
                entity);

        secondComponent.Value++;

        var versionAfterSecondAccess =
            world.GetChangeVersion<TestComponent>();

        Assert.True(
            versionAfterSecondAccess >
            afterClearVersion);
    }

    private struct TestComponent
    {
        public TestComponent(
            int value)
        {
            Value =
                value;
        }

        public int Value;
    }
}