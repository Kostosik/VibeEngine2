using Engine.ECS;

namespace Engine.Tests.ECS.DirtyTracking;

public sealed class WorldDirtyTrackingTests
{
    [Fact]
    public void ReadOnlyQuery_DoesNotMarkComponentDirty()
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

        var versionBefore =
            world.GetChangeVersion<TestComponent>();

        var processed =
            0;

        foreach (var item in
                 world.QueryReadOnly<TestComponent>())
        {
            Assert.Equal(
                processed == 0 ? 10 : 20,
                item.Component.Value);

            processed++;
        }

        Assert.Equal(
            2,
            processed);

        Assert.Equal(
            0,
            world.GetDirtyEntities<TestComponent>().Length);

        Assert.Equal(
            versionBefore,
            world.GetChangeVersion<TestComponent>());
    }

    [Fact]
    public void ReadOnlyQueryPair_DoesNotMarkComponentsDirty()
    {
        using var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new TestComponent(10));

        world.Add(
            entity,
            new SecondTestComponent(20));

        world.ClearDirty<TestComponent>();
        world.ClearDirty<SecondTestComponent>();

        var firstVersion =
            world.GetChangeVersion<TestComponent>();

        var secondVersion =
            world.GetChangeVersion<SecondTestComponent>();

        foreach (var item in
                 world.QueryReadOnly<
                     TestComponent,
                     SecondTestComponent>())
        {
            Assert.Equal(
                10,
                item.First.Value);

            Assert.Equal(
                20,
                item.Second.Value);
        }

        Assert.Equal(
            0,
            world.GetDirtyEntities<TestComponent>().Length);

        Assert.Equal(
            0,
            world.GetDirtyEntities<SecondTestComponent>().Length);

        Assert.Equal(
            firstVersion,
            world.GetChangeVersion<TestComponent>());

        Assert.Equal(
            secondVersion,
            world.GetChangeVersion<SecondTestComponent>());
    }

    private struct SecondTestComponent
    {
        public SecondTestComponent(
            int value)
        {
            Value =
                value;
        }

        public int Value;
    }

    [Fact]
    public void Inspection_DoesNotMarkComponentDirty()
    {
        using var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new TestComponent(10));

        world.ClearDirty<TestComponent>();

        var versionBefore =
            world.GetChangeVersion<TestComponent>();

        var found =
            world.Inspector.TryGetComponent(
                entity,
                typeof(TestComponent),
                out var component);

        Assert.True(found);
        Assert.NotNull(component);

        Assert.Equal(
            0,
            world.GetDirtyEntities<TestComponent>().Length);

        Assert.Equal(
            versionBefore,
            world.GetChangeVersion<TestComponent>());
    }

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