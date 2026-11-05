using Engine.ECS;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.ECS.Prefabs;

namespace Engine.Tests.ECS.Prefabs;

public sealed class PrefabAssetCaptureTests
{
    [Fact]
    public void Create_CapturesRootAndDescendants()
    {
        using var world = new World();

        var root = world.CreateEntity();
        var child = world.CreateEntity();
        var grandchild = world.CreateEntity();

        // This entity is outside the selected hierarchy.
        world.CreateEntity();

        world.Add(
            child,
            new TransformParent2D(root));

        world.Add(
            grandchild,
            new TransformParent2D(child));

        var prefab = PrefabAssetCapture.Create(
            "Hierarchy",
            world,
            root);

        Assert.Equal(3, prefab.Entities.Count);
        Assert.Equal(new PrefabEntityId(1), prefab.RootEntity);

        var capturedChild = Assert.Single(
            prefab.Entities.Where(
                entity => entity.Id == new PrefabEntityId(2)));

        var capturedGrandchild = Assert.Single(
            prefab.Entities.Where(
                entity => entity.Id == new PrefabEntityId(3)));

        var childParent = Assert.Single(
            capturedChild.Components.OfType<
                PrefabComponent<TransformParent2D>>());

        var grandchildParent = Assert.Single(
            capturedGrandchild.Components.OfType<
                PrefabComponent<TransformParent2D>>());

        Assert.Equal(
            new EntityId(1, 0),
            childParent.Component.Parent);

        Assert.Equal(
            new EntityId(2, 0),
            grandchildParent.Component.Parent);
    }

    [Fact]
    public void Create_RejectsReferenceOutsideHierarchy()
    {
        using var world = new World();

        var root = world.CreateEntity();
        var external = world.CreateEntity();

        world.Add(
            root,
            new TestEntityReferenceComponent(external));

        Assert.Throws<InvalidDataException>(
            () => PrefabAssetCapture.Create(
                "InvalidPrefab",
                world,
                root));
    }

    private readonly record struct TestEntityReferenceComponent(
        EntityId Target) : IPrefabEntityReferenceRemappable
    {
        public object RemapPrefabEntityReferences(
            Func<EntityId, EntityId> remap)
        {
            ArgumentNullException.ThrowIfNull(remap);

            return new TestEntityReferenceComponent(
                remap(Target));
        }
    }
}