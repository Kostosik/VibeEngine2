using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Physics.Components;
using Engine.Physics.Queries;
using Engine.Physics.Shapes;

namespace Engine.Tests.Physics;

public sealed class PhysicsQueryOverlap2DTests
{
    [Fact]
    public void OverlapPoint_FindsContainingCircle()
    {
        var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            entity,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        var query =
            new PhysicsQuery2D(
                world);

        var results =
            new List<EntityId>();

        query.OverlapPoint(
            FixedVector2.Zero,
            results);

        Assert.Single(
            results);

        Assert.Equal(
            entity,
            results[0]);
    }

    [Fact]
    public void OverlapPoint_DoesNotFindSeparatedCollider()
    {
        var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(3),
                    Fixed32.Zero)));

        world.Add(
            entity,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        var query =
            new PhysicsQuery2D(
                world);

        var results =
            new List<EntityId>();

        query.OverlapPoint(
            FixedVector2.Zero,
            results);

        Assert.Empty(
            results);
    }

    [Fact]
    public void OverlapPoint_FindsPolygon()
    {
        var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            entity,
            new Collider2D(
                new PolygonShape2D(
                    new[]
                    {
                        new FixedVector2(
                            Fixed32.FromInt(-1),
                            Fixed32.FromInt(-1)),

                        new FixedVector2(
                            Fixed32.One,
                            Fixed32.FromInt(-1)),

                        new FixedVector2(
                            Fixed32.One,
                            Fixed32.One),

                        new FixedVector2(
                            Fixed32.FromInt(-1),
                            Fixed32.One)
                    })));

        var query =
            new PhysicsQuery2D(
                world);

        var results =
            new List<EntityId>();

        query.OverlapPoint(
            FixedVector2.Zero,
            results);

        Assert.Single(
            results);

        Assert.Equal(
            entity,
            results[0]);
    }

    [Fact]
    public void OverlapBounds_ReturnsIntersectingColliders()
    {
        var world =
            new World();

        var first =
            world.CreateEntity();

        var second =
            world.CreateEntity();

        world.Add(
            first,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(1),
                    Fixed32.Zero)));

        world.Add(
            second,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(5),
                    Fixed32.Zero)));

        world.Add(
            first,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        world.Add(
            second,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        var query =
            new PhysicsQuery2D(
                world);

        var results =
            new List<EntityId>();

        query.OverlapBounds(
            new FixedBounds2(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromInt(-1)),

                new FixedVector2(
                    Fixed32.FromInt(2),
                    Fixed32.One)),
            results);

        Assert.Single(
            results);

        Assert.Equal(
            first,
            results[0]);
    }

    [Fact]
    public void OverlapBounds_ExcludesTriggersByDefault()
    {
        var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new WorldTransform2D(
                FixedVector2.Zero));

        var collider =
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One));

        collider.IsTrigger =
            true;

        world.Add(
            entity,
            collider);

        var query =
            new PhysicsQuery2D(
                world);

        var results =
            new List<EntityId>();

        query.OverlapBounds(
            new FixedBounds2(
                new FixedVector2(
                    Fixed32.FromInt(-2),
                    Fixed32.FromInt(-2)),

                new FixedVector2(
                    Fixed32.FromInt(2),
                    Fixed32.FromInt(2))),
            results);

        Assert.Empty(
            results);
    }
}