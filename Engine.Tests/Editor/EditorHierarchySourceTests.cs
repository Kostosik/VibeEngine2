using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Editor.Hierarchy;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class EditorHierarchySourceTests
{
    [Fact]
    public void GetNodes_UsesTransformParentHierarchy()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var root =
            ecsWorld.CreateEntity();

        var child =
            ecsWorld.CreateEntity();

        var grandchild =
            ecsWorld.CreateEntity();

        ecsWorld.Add(
            child,
            new TransformParent2D(
                root));

        ecsWorld.Add(
            grandchild,
            new TransformParent2D(
                child));

        var source =
            new EcsHierarchySource(
                world);

        var nodes =
            source.GetNodes();

        var rootNode =
            nodes.Single(
                node => node.Id.Equals(root));

        var childNode =
            nodes.Single(
                node => node.Id.Equals(child));

        var grandchildNode =
            nodes.Single(
                node => node.Id.Equals(grandchild));

        Assert.Null(
            rootNode.ParentId);

        Assert.Equal(
            root,
            childNode.ParentId);

        Assert.Equal(
            child,
            grandchildNode.ParentId);
    }

    [Fact]
    public void GetNodes_EntityWithoutParent_IsRootNode()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var entity =
            ecsWorld.CreateEntity();

        var source =
            new EcsHierarchySource(
                world);

        var nodes =
            source.GetNodes();

        var node =
            Assert.Single(nodes);

        Assert.Equal(
            entity,
            node.Id);

        Assert.Null(
            node.ParentId);
    }
}