using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Editor.Hierarchy;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class EcsHierarchySourceTests
{
    [Fact]
    public void GetNodes_WhenParentDoesNotExist_TreatsEntityAsRoot()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var entity =
            world.SpatialEntities.CreateEntity(
                new WorldPosition(0, 0));

        var missingParent =
            new EntityId(
                999,
                1);

        ecsWorld.Add(
            entity,
            new TransformParent2D(
                missingParent));

        var source =
            new EcsHierarchySource(
                world);

        var node =
            Assert.Single(
                source.GetNodes());

        Assert.Equal(
            entity,
            node.Id);

        Assert.Equal(
            missingParent,
            node.ParentId);
    }

    [Fact]
    public void GetNodes_WhenHierarchyContainsCycle_ReturnsAllEntities()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var first =
            world.SpatialEntities.CreateEntity(
                new WorldPosition(0, 0));

        var second =
            world.SpatialEntities.CreateEntity(
                new WorldPosition(1, 1));

        ecsWorld.Add(
            first,
            new TransformParent2D(
                second));

        ecsWorld.Add(
            second,
            new TransformParent2D(
                first));

        var source =
            new EcsHierarchySource(
                world);

        var nodes =
            source.GetNodes();

        Assert.Equal(
            2,
            nodes.Count);

        Assert.Contains(
            nodes,
            node => Equals(node.Id, first));

        Assert.Contains(
            nodes,
            node => Equals(node.Id, second));
    }
}