using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.ECS.Systems;
using Engine.Transform;

namespace Engine.Tests.Transform;

public sealed class TransformHierarchy3DSystemTests
{
    [Fact]
    public void DefaultComponentsUseIdentity()
    {
        var local = new LocalTransform3D();
        var world = new WorldTransform3D();

        Assert.Equal(Vector3.Zero, local.Position);
        Assert.Equal(Quaternion.Identity, local.Rotation);
        Assert.Equal(Vector3.One, local.Scale);

        Assert.Equal(Vector3.Zero, world.Position);
        Assert.Equal(Quaternion.Identity, world.Rotation);
        Assert.Equal(Vector3.One, world.Scale);
    }

    [Fact]
    public void RootLocalTransformBecomesWorldTransform()
    {
        using var world = new World();

        var entity = world.CreateEntity();

        AddLocal(world, entity, new Vector3(5, 7, 9));

        var system = new TransformHierarchy3DSystem(world);

        system.Update(default);

        var result = world.Get<WorldTransform3D>(entity);

        Assert.Equal(new Vector3(5, 7, 9), result.Position);
    }

    [Fact]
    public void ChildUsesParentWorldTransform()
    {
        using var world = new World();

        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        AddLocal(world, parent, new Vector3(10, 20, 30));
        AddLocal(world, child, new Vector3(3, 4, 5));

        world.Add(child, new TransformParent3D(parent));

        var system = new TransformHierarchy3DSystem(world);

        system.Update(default);

        var result = world.Get<WorldTransform3D>(child);

        Assert.Equal(new Vector3(13, 24, 35), result.Position);
    }

    [Fact]
    public void NestedHierarchyIsResolved()
    {
        using var world = new World();

        var root = world.CreateEntity();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        AddLocal(world, root, new Vector3(10, 0, 0));
        AddLocal(world, parent, new Vector3(5, 0, 0));
        AddLocal(world, child, new Vector3(2, 0, 0));

        world.Add(parent, new TransformParent3D(root));
        world.Add(child, new TransformParent3D(parent));

        var system = new TransformHierarchy3DSystem(world);

        system.Update(default);

        Assert.Equal(
            new Vector3(17, 0, 0),
            world.Get<WorldTransform3D>(child).Position);
    }

    [Fact]
    public void ParentRotationAffectsChildPosition()
    {
        using var world = new World();

        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        AddLocal(world, parent, Vector3.Zero);
        AddLocal(world, child, Vector3.UnitX);

        world.Get<LocalTransform3D>(parent).Rotation =
            Quaternion.CreateFromAxisAngle(
                Vector3.UnitZ,
                MathF.PI / 2f);

        world.Add(child, new TransformParent3D(parent));

        var system = new TransformHierarchy3DSystem(world);

        system.Update(default);

        var position = world.Get<WorldTransform3D>(child).Position;

        Assert.Equal(0f, position.X, 0.001f);
        Assert.Equal(1f, position.Y, 0.001f);
        Assert.Equal(0f, position.Z, 0.001f);
    }

    [Fact]
    public void ParentScaleAffectsChildPosition()
    {
        using var world = new World();

        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        AddLocal(world, parent, Vector3.Zero);
        AddLocal(world, child, new Vector3(2, 3, 4));

        world.Get<LocalTransform3D>(parent).Scale =
            new Vector3(2, 3, 4);

        world.Add(child, new TransformParent3D(parent));

        var system = new TransformHierarchy3DSystem(world);

        system.Update(default);

        var position = world.Get<WorldTransform3D>(child).Position;

        Assert.Equal(new Vector3(4, 9, 16), position);
    }

    [Fact]
    public void ParentWithWorldTransformOnlyIsValid()
    {
        using var world = new World();

        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        world.Add(
            parent,
            new WorldTransform3D(new Vector3(10, 20, 30)));

        AddLocal(world, child, new Vector3(1, 2, 3));

        world.Add(child, new TransformParent3D(parent));

        var system = new TransformHierarchy3DSystem(world);

        system.Update(default);

        Assert.Equal(
            new Vector3(11, 22, 33),
            world.Get<WorldTransform3D>(child).Position);
    }

    [Fact]
    public void MissingWorldTransformThrows()
    {
        using var world = new World();

        var entity = world.CreateEntity();

        world.Add(
            entity,
            new LocalTransform3D(
                new Transform3D(Vector3.Zero)));

        var system = new TransformHierarchy3DSystem(world);

        Assert.Throws<InvalidOperationException>(
            () => system.Update(default));
    }

    [Fact]
    public void ParentWithoutLocalTransformThrows()
    {
        using var world = new World();

        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        world.Add(parent, new WorldTransform3D());
        world.Add(child, new WorldTransform3D());
        world.Add(child, new TransformParent3D(parent));

        var system = new TransformHierarchy3DSystem(world);

        Assert.Throws<InvalidOperationException>(
            () => system.Update(default));
    }

    [Fact]
    public void MissingParentThrows()
    {
        using var world = new World();

        var child = world.CreateEntity();
        var missingParent = world.CreateEntity();

        world.DestroyEntity(missingParent);

        AddLocal(world, child, Vector3.Zero);
        world.Add(child, new TransformParent3D(missingParent));

        var system = new TransformHierarchy3DSystem(world);

        Assert.Throws<InvalidOperationException>(
            () => system.Update(default));
    }

    [Fact]
    public void CyclicHierarchyThrows()
    {
        using var world = new World();

        var first = world.CreateEntity();
        var second = world.CreateEntity();

        AddLocal(world, first, Vector3.Zero);
        AddLocal(world, second, Vector3.Zero);

        world.Add(first, new TransformParent3D(second));
        world.Add(second, new TransformParent3D(first));

        var system = new TransformHierarchy3DSystem(world);

        Assert.Throws<InvalidOperationException>(
            () => system.Update(default));
    }

    [Fact]
    public void TransformParentWithoutLocalTransformThrows()
    {
        using var world = new World();

        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        world.Add(parent, new WorldTransform3D());
        world.Add(child, new WorldTransform3D());
        world.Add(child, new TransformParent3D(parent));

        var system = new TransformHierarchy3DSystem(world);

        Assert.Throws<InvalidOperationException>(
            () => system.Update(default));
    }

    private static void AddLocal(
        World world,
        Engine.ECS.Entities.EntityId entity,
        Vector3 position)
    {
        world.Add(
            entity,
            new LocalTransform3D(
                new Transform3D(position)));

        world.Add(
            entity,
            new WorldTransform3D());
    }
}