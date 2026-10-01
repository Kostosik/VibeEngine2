using Engine.Core.Events;
using Engine.Core.Math;
using Engine.Physics.Components;
using Engine.Physics.Queries;
using Engine.Physics.Shapes;
using Engine.ECS;
using Engine.ECS.Components;

namespace Engine.Tests.Physics;

public sealed class PhysicsQuery2DTests
{
    [Fact]
    public void Raycast_HitsNearestAabb()
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
                    Fixed32.FromInt(2),
                    Fixed32.Zero)));

        world.Add(
            second,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(5),
                    Fixed32.Zero)));

        world.Add(
            first,
            PhysicsBody2D.Static());

        world.Add(
            second,
            PhysicsBody2D.Static());

        world.Add(
            first,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.One,
                        Fixed32.One))));

        world.Add(
            second,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.One,
                        Fixed32.One))));

        var query =
            new PhysicsQuery2D(
                world);

        var hit =
            query.Raycast(
                FixedVector2.Zero,
                new FixedVector2(
                    Fixed32.One,
                    Fixed32.Zero),
                Fixed32.FromInt(10),
                out var result);

        Assert.True(hit);

        Assert.Equal(
            first,
            result.Entity);

        Assert.Equal(
            Fixed32.FromFloat(1.5f),
            result.Distance);
    }

    [Fact]
    public void Raycast_HitsCircle()
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
            PhysicsBody2D.Static());

        world.Add(
            entity,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        var query =
            new PhysicsQuery2D(
                world);

        var hit =
            query.Raycast(
                FixedVector2.Zero,
                new FixedVector2(
                    Fixed32.One,
                    Fixed32.Zero),
                Fixed32.FromInt(10),
                out var result);

        Assert.True(hit);

        Assert.Equal(
            entity,
            result.Entity);

        Assert.Equal(
            Fixed32.FromInt(2),
            result.Distance);
    }

    [Fact]
    public void Raycast_HitsPolygon()
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
            PhysicsBody2D.Static());

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

        var hit =
            query.Raycast(
                FixedVector2.Zero,
                new FixedVector2(
                    Fixed32.One,
                    Fixed32.Zero),
                Fixed32.FromInt(10),
                out var result);

        Assert.True(hit);

        Assert.Equal(
            entity,
            result.Entity);

        Assert.Equal(
            Fixed32.FromInt(2),
            result.Distance);
    }

    [Fact]
    public void Raycast_RespectsLayerMask()
    {
        var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(2),
                    Fixed32.Zero)));

        world.Add(
            entity,
            PhysicsBody2D.Static());

        var collider =
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One));

        collider.CollisionLayer =
            2u;

        world.Add(
            entity,
            collider);

        var query =
            new PhysicsQuery2D(
                world);

        var hit =
            query.Raycast(
                FixedVector2.Zero,
                new FixedVector2(
                    Fixed32.One,
                    Fixed32.Zero),
                Fixed32.FromInt(10),
                out _,
                1u);

        Assert.False(hit);
    }

    [Fact]
    public void Raycast_ExcludesTriggersByDefault()
    {
        var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(2),
                    Fixed32.Zero)));

        world.Add(
            entity,
            PhysicsBody2D.Static());

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

        Assert.False(
            query.Raycast(
                FixedVector2.Zero,
                new FixedVector2(
                    Fixed32.One,
                    Fixed32.Zero),
                Fixed32.FromInt(10),
                out _));
    }
}