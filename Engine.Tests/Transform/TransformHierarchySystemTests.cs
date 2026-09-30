using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.ECS.Systems;
using Engine.Transform;

namespace Engine.Tests.Transform;

public sealed class TransformHierarchySystemTests
{
    [Fact]
    public void RootLocalTransformBecomesWorldTransform()
    {
        using var world = new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            entity,
            new LocalTransform2D(
                new FixedTransform2D(
                    new FixedVector2(
                        Fixed32.FromInt(5),
                        Fixed32.FromInt(7)))));

        var system =
            new TransformHierarchySystem(
                world);

        system.FixedUpdate(
            default);

        var transform =
            world.Get<WorldTransform2D>(
                entity);

        Assert.Equal(
            Fixed32.FromInt(5),
            transform.Position.X);

        Assert.Equal(
            Fixed32.FromInt(7),
            transform.Position.Y);
    }

    [Fact]
    public void ChildUsesParentWorldTransform()
    {
        using var world = new World();

        var parent =
            world.CreateEntity();

        var child =
            world.CreateEntity();

        world.Add(
            parent,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            parent,
            new LocalTransform2D(
                new FixedTransform2D(
                    new FixedVector2(
                        Fixed32.FromInt(10),
                        Fixed32.FromInt(20)))));

        world.Add(
            child,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            child,
            new LocalTransform2D(
                new FixedTransform2D(
                    new FixedVector2(
                        Fixed32.FromInt(3),
                        Fixed32.FromInt(4)))));

        world.Add(
            child,
            new TransformParent2D(
                parent));

        var system =
            new TransformHierarchySystem(
                world);

        system.FixedUpdate(
            default);

        var transform =
            world.Get<WorldTransform2D>(
                child);

        Assert.Equal(
            Fixed32.FromInt(13),
            transform.Position.X);

        Assert.Equal(
            Fixed32.FromInt(24),
            transform.Position.Y);
    }

    [Fact]
    public void NestedHierarchyIsResolved()
    {
        using var world = new World();

        var root =
            world.CreateEntity();

        var parent =
            world.CreateEntity();

        var child =
            world.CreateEntity();

        AddLocal(
            world,
            root,
            10,
            0);

        AddLocal(
            world,
            parent,
            5,
            0);

        AddLocal(
            world,
            child,
            2,
            0);

        world.Add(
            parent,
            new TransformParent2D(
                root));

        world.Add(
            child,
            new TransformParent2D(
                parent));

        var system =
            new TransformHierarchySystem(
                world);

        system.FixedUpdate(
            default);

        var transform =
            world.Get<WorldTransform2D>(
                child);

        Assert.Equal(
            Fixed32.FromInt(17),
            transform.Position.X);
    }

    [Fact]
    public void ParentRotationAffectsChildPosition()
    {
        using var world = new World();

        var parent =
            world.CreateEntity();

        var child =
            world.CreateEntity();

        AddLocal(
            world,
            parent,
            0,
            0);

        world.Get<LocalTransform2D>(
            parent).Rotation =
            Fixed32.HalfPi;

        AddLocal(
            world,
            child,
            1,
            0);

        world.Add(
            child,
            new TransformParent2D(
                parent));

        var system =
            new TransformHierarchySystem(
                world);

        system.FixedUpdate(
            default);

        var transform =
            world.Get<WorldTransform2D>(
                child);

        Assert.Equal(
            0.0f,
            transform.Position.X.ToFloat(),
            0.001f);

        Assert.Equal(
            1.0f,
            transform.Position.Y.ToFloat(),
            0.001f);
    }

    [Fact]
    public void MissingParentThrows()
    {
        using var world = new World();

        var child =
            world.CreateEntity();

        var missingParent =
            world.CreateEntity();

        world.DestroyEntity(
            missingParent);

        AddLocal(
            world,
            child,
            1,
            0);

        world.Add(
            child,
            new TransformParent2D(
                missingParent));

        var system =
            new TransformHierarchySystem(
                world);

        Assert.Throws<InvalidOperationException>(
            () =>
                system.FixedUpdate(
                    default));
    }

    [Fact]
    public void CyclicHierarchyThrows()
    {
        using var world = new World();

        var first =
            world.CreateEntity();

        var second =
            world.CreateEntity();

        AddLocal(
            world,
            first,
            1,
            0);

        AddLocal(
            world,
            second,
            1,
            0);

        world.Add(
            first,
            new TransformParent2D(
                second));

        world.Add(
            second,
            new TransformParent2D(
                first));

        var system =
            new TransformHierarchySystem(
                world);

        Assert.Throws<InvalidOperationException>(
            () =>
                system.FixedUpdate(
                    default));
    }

    private static void AddLocal(
        World world,
        Engine.ECS.Entities.EntityId entity,
        int x,
        int y)
    {
        world.Add(
            entity,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            entity,
            new LocalTransform2D(
                new FixedTransform2D(
                    new FixedVector2(
                        Fixed32.FromInt(x),
                        Fixed32.FromInt(y)))));
    }
}