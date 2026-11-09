using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.ECS.Systems;
using Engine.Transform;
using Xunit;

namespace Engine.Tests.Transform;

public sealed class FixedTransformHierarchy3DSystemTests
{
    [Fact]
    public void DefaultComponentsUseIdentity()
    {
        var local = new FixedLocalTransform3D();
        var world = new FixedWorldTransform3D();

        Assert.Equal(FixedVector3.Zero, local.Position);
        Assert.Equal(FixedQuaternion.Identity, local.Rotation);
        Assert.Equal(FixedVector3.One, local.Scale);

        Assert.Equal(FixedVector3.Zero, world.Position);
        Assert.Equal(FixedQuaternion.Identity, world.Rotation);
        Assert.Equal(FixedVector3.One, world.Scale);
    }

    [Fact]
    public void RootLocalTransformBecomesWorldTransform()
    {
        using var world = new World();
        var entity = world.CreateEntity();

        AddLocal(world, entity, Vector(5, 7, 9));

        var system = new FixedTransformHierarchy3DSystem(world);
        system.FixedUpdate(default);

        Assert.Equal(
            Vector(5, 7, 9),
            world.Get<FixedWorldTransform3D>(entity).Position);
    }

    [Fact]
    public void ChildUsesParentWorldTransform()
    {
        using var world = new World();

        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        AddLocal(world, parent, Vector(10, 20, 30));
        AddLocal(world, child, Vector(3, 4, 5));

        world.Add(child, new FixedTransformParent3D(parent));

        var system = new FixedTransformHierarchy3DSystem(world);
        system.FixedUpdate(default);

        Assert.Equal(
            Vector(13, 24, 35),
            world.Get<FixedWorldTransform3D>(child).Position);
    }

    [Fact]
    public void NestedHierarchyIsResolved()
    {
        using var world = new World();

        var root = world.CreateEntity();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        AddLocal(world, root, Vector(10, 0, 0));
        AddLocal(world, parent, Vector(5, 0, 0));
        AddLocal(world, child, Vector(2, 0, 0));

        world.Add(parent, new FixedTransformParent3D(root));
        world.Add(child, new FixedTransformParent3D(parent));

        var system = new FixedTransformHierarchy3DSystem(world);
        system.FixedUpdate(default);

        Assert.Equal(
            Vector(17, 0, 0),
            world.Get<FixedWorldTransform3D>(child).Position);
    }

    [Fact]
    public void ParentRotationAffectsChildPosition()
    {
        using var world = new World();

        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        AddLocal(world, parent, Vector(0, 0, 0));
        AddLocal(world, child, Vector(1, 0, 0));

        world.Get<FixedLocalTransform3D>(parent).Rotation =
            FixedQuaternion.CreateFromAxisAngle(
                FixedVector3.UnitZ,
                Fixed32.HalfPi);

        world.Add(child, new FixedTransformParent3D(parent));

        var system = new FixedTransformHierarchy3DSystem(world);
        system.FixedUpdate(default);

        var position = world.Get<FixedWorldTransform3D>(child).Position;
        var tolerance = Fixed32.FromFloat(0.01f);

        Assert.True(Fixed32.Abs(position.X) <= tolerance);
        Assert.True(Fixed32.Abs(position.Y - Fixed32.One) <= tolerance);
    }

    [Fact]
    public void ParentScaleAffectsChildPosition()
    {
        using var world = new World();

        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        AddLocal(world, parent, Vector(0, 0, 0));
        AddLocal(world, child, Vector(2, 3, 4));

        world.Get<FixedLocalTransform3D>(parent).Scale = Vector(2, 3, 4);

        world.Add(child, new FixedTransformParent3D(parent));

        var system = new FixedTransformHierarchy3DSystem(world);
        system.FixedUpdate(default);

        Assert.Equal(
            Vector(4, 9, 16),
            world.Get<FixedWorldTransform3D>(child).Position);
    }

    [Fact]
    public void MissingWorldTransformThrows()
    {
        using var world = new World();

        var entity = world.CreateEntity();
        world.Add(entity, new FixedLocalTransform3D());

        var system = new FixedTransformHierarchy3DSystem(world);

        Assert.Throws<InvalidOperationException>(
            () => system.FixedUpdate(default));
    }

    [Fact]
    public void CyclicHierarchyThrows()
    {
        using var world = new World();

        var first = world.CreateEntity();
        var second = world.CreateEntity();

        AddLocal(world, first, Vector(0, 0, 0));
        AddLocal(world, second, Vector(0, 0, 0));

        world.Add(first, new FixedTransformParent3D(second));
        world.Add(second, new FixedTransformParent3D(first));

        var system = new FixedTransformHierarchy3DSystem(world);

        Assert.Throws<InvalidOperationException>(
            () => system.FixedUpdate(default));
    }

    private static void AddLocal(
        World world,
        Engine.ECS.Entities.EntityId entity,
        FixedVector3 position)
    {
        world.Add(
            entity,
            new FixedLocalTransform3D(
                new FixedTransform3D(position)));

        world.Add(entity, new FixedWorldTransform3D());
    }

    private static FixedVector3 Vector(int x, int y, int z)
    {
        return new FixedVector3(
            Fixed32.FromInt(x),
            Fixed32.FromInt(y),
            Fixed32.FromInt(z));
    }
}