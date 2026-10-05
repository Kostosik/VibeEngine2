using Engine.Core.Events;
using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.Physics;
using Engine.Physics.Collision;
using Engine.Physics.Components;
using Engine.Physics.Materials;
using Engine.Physics.Shapes;

namespace Engine.Tests.Physics;

public sealed class PhysicsSystem2DTests
{
    [Fact]
    public void CircleRestingOnFloorCancelsGravity()
    {
        using var world =
            new World();

        var floor =
            world.CreateEntity();

        var circle =
            world.CreateEntity();

        world.Add(
            floor,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromFloat(4f))));

        world.Add(
            floor,
            PhysicsBody2D.Static());

        world.Add(
            floor,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.FromInt(32),
                        Fixed32.One))));

        world.Add(
            circle,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromFloat(5.5f))));

        world.Add(
            circle,
            PhysicsBody2D.Dynamic(
                Fixed32.One));

        world.Add(
            circle,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        var settings =
            new PhysicsSettings2D
            {
                Gravity =
                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.FromFloat(-9.8f)),

                Substeps = 4,
                PositionIterations = 2,
                VelocityIterations = 4
            };

        var physics =
            new PhysicsSystem2D(
                world,
                settings,
                new EventBus());

        physics.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromFloat(1f / 60f),
                    Tick.Zero)));

        var body =
            world.Get<PhysicsBody2D>(
                circle);

        var transform =
            world.Get<WorldTransform2D>(
                circle);

        Assert.True(
            Fixed32.Abs(
                body.Velocity.Y) <=
            Fixed32.FromFloat(0.001f),
            $"Velocity.Y={body.Velocity.Y.ToFloat():F6}, " +
            $"Position.Y={transform.Position.Y.ToFloat():F6}, " +
            $"Contacts={physics.Contacts.Count}");
    }

    [Fact]
    public void SlightlyRotatedTriangleSettlesOnFloor()
    {
        using var world =
            new World();

        var floor =
            world.CreateEntity();

        var triangle =
            world.CreateEntity();

        world.Add(
            floor,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromFloat(4f))));

        world.Add(
            floor,
            PhysicsBody2D.Static());

        world.Add(
            floor,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.FromInt(32),
                        Fixed32.One))));

        world.Add(
            triangle,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromFloat(5.55f)))
            {
                Rotation =
                    Fixed32.FromFloat(0.05f)
            });

        world.Add(
            triangle,
            PhysicsBody2D.Dynamic(
                Fixed32.One));

        world.Add(
            triangle,
            new Collider2D(
                new PolygonShape2D(
                    new[]
                    {
                    new FixedVector2(
                        -Fixed32.One,
                        -Fixed32.One),

                    new FixedVector2(
                        Fixed32.One,
                        -Fixed32.One),

                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.One)
                    })));

        var settings =
            new PhysicsSettings2D
            {
                Gravity =
                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.FromFloat(-9.8f)),

                Substeps = 4,
                PositionIterations = 2,
                VelocityIterations = 4
            };

        var physics =
            new PhysicsSystem2D(
                world,
                settings,
                new EventBus());

        for (var i = 0; i < 240; i++)
        {
            physics.FixedUpdate(
                new FixedSystemContext(
                    new SimulationTime(
                        Fixed32.FromFloat(1f / 20f),
                        new Tick((ulong)i))));
        }

        var body =
            world.Get<PhysicsBody2D>(
                triangle);

        var transform =
            world.Get<WorldTransform2D>(
                triangle);

        Assert.True(
            body.IsSleeping,
            $"Y={transform.Position.Y.ToFloat():F6}, " +
            $"Rotation={transform.Rotation.ToFloat():F6}, " +
            $"VY={body.Velocity.Y.ToFloat():F6}, " +
            $"W={body.AngularVelocity.ToFloat():F6}");

        Assert.True(
            Fixed32.Abs(
                body.AngularVelocity) <=
            Fixed32.FromFloat(0.01f));

        Assert.True(
            Fixed32.Abs(
                transform.Rotation) <=
            Fixed32.FromFloat(0.01f));
    }


    [Fact]
    public void CircleFloorContactCancelsDownwardVelocity()
    {
        using var world =
            new World();

        var floor =
            world.CreateEntity();

        var circle =
            world.CreateEntity();

        world.Add(
            floor,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromFloat(4f))));

        world.Add(
            floor,
            PhysicsBody2D.Static());

        world.Add(
            floor,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.FromInt(32),
                        Fixed32.One))));

        world.Add(
            circle,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromFloat(5.49f))));

        var body =
            PhysicsBody2D.Dynamic(
                Fixed32.One);

        body.Velocity =
            new FixedVector2(
                Fixed32.Zero,
                Fixed32.FromInt(-1));

        world.Add(
            circle,
            body);

        world.Add(
            circle,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        var settings =
            new PhysicsSettings2D
            {
                Gravity =
                    FixedVector2.Zero,

                PositionIterations = 0,
                VelocityIterations = 1
            };

        var physics =
            new PhysicsSystem2D(
                world,
                settings,
                new EventBus());

        physics.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.Zero,
                    Tick.Zero)));

        var result =
            world.Get<PhysicsBody2D>(
                circle);

        Assert.True(
            Fixed32.Abs(
                result.Velocity.Y) <=
            Fixed32.FromRatio(
                2,
                65536));
    }

    [Fact]
    public void CircleSettlesOnFloorAndSleeps()
    {
        using var world =
            new World();

        var floor =
            world.CreateEntity();

        var circle =
            world.CreateEntity();

        world.Add(
            floor,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromFloat(4f))));

        world.Add(
            floor,
            PhysicsBody2D.Static());

        world.Add(
            floor,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.FromInt(32),
                        Fixed32.One))));

        world.Add(
            circle,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromFloat(18f))));

        world.Add(
            circle,
            PhysicsBody2D.Dynamic(
                Fixed32.One));

        world.Add(
            circle,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        var settings =
            new PhysicsSettings2D
            {
                Gravity =
                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.FromFloat(-9.8f)),

                Substeps = 4,

                PositionIterations = 2,
                VelocityIterations = 4
            };

        var physics =
            new PhysicsSystem2D(
                world,
                settings,
                new EventBus());

        for (var i = 0; i < 240; i++)
        {
            physics.FixedUpdate(
                new FixedSystemContext(
                    new SimulationTime(
                        Fixed32.FromFloat(1f / 20f),
                        new Tick((ulong)i))));
        }

        var body =
            world.Get<PhysicsBody2D>(
                circle);

        Assert.True(
            body.IsSleeping,
            $"Velocity=({body.Velocity.X.ToFloat():F6}, {body.Velocity.Y.ToFloat():F6}), " +
            $"AngularVelocity={body.AngularVelocity.ToFloat():F6}, " +
            $"SleepTimer={body.SleepTimer.ToFloat():F6}");

        Assert.Equal(
            FixedVector2.Zero,
            body.Velocity);

        Assert.Equal(
            Fixed32.Zero,
            body.AngularVelocity);
    }

    [Fact]
    public void TriangleRestingContactDoesNotGainEnergy()
    {
        using var world =
            new World();

        var floor =
            world.CreateEntity();

        var triangle =
            world.CreateEntity();

        world.Add(
            floor,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromFloat(4f))));

        world.Add(
            floor,
            PhysicsBody2D.Static());

        world.Add(
            floor,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.FromInt(32),
                        Fixed32.One))));

        world.Add(
            triangle,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromFloat(5.5f))));

        var body =
            PhysicsBody2D.Dynamic(
                Fixed32.One,
                Fixed32.One);

        body.IsSleeping = true;

        world.Add(
            triangle,
            body);

        world.Add(
            triangle,
            new Collider2D(
                new PolygonShape2D(
                    new[]
                    {
                    new FixedVector2(
                        -Fixed32.One,
                        -Fixed32.One),

                    new FixedVector2(
                        Fixed32.One,
                        -Fixed32.One),

                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.One)
                    })));

        var settings =
            new PhysicsSettings2D
            {
                Gravity =
                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.FromFloat(-9.8f)),

                Substeps = 4,

                PositionIterations = 2,
                VelocityIterations = 4
            };

        var physics =
            new PhysicsSystem2D(
                world,
                settings,
                new EventBus());

        ref var triangleBody =
            ref world.Get<PhysicsBody2D>(
                triangle);

        triangleBody.WakeUp();

        for (var i = 0; i < 120; i++)
        {
            physics.FixedUpdate(
                new FixedSystemContext(
                    new SimulationTime(
                        Fixed32.FromFloat(
                            1f / 60f),
                        new Tick((ulong)i))));
        }

        var result =
            world.Get<PhysicsBody2D>(
                triangle);

        Assert.True(
            Fixed32.Abs(
                result.Velocity.X) <=
            Fixed32.FromFloat(0.01f));

        Assert.True(
            Fixed32.Abs(
                result.Velocity.Y) <=
            Fixed32.FromFloat(0.01f));

        Assert.True(
            Fixed32.Abs(
                result.AngularVelocity) <=
            Fixed32.FromFloat(0.01f));
    }

    [Fact]
    public void FastDynamicBody_DoesNotTunnelThroughThinFloor()
    {
        using var world =
            new World();

        var floor =
            world.CreateEntity();

        world.Add(
            floor,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            floor,
            PhysicsBody2D.Static());

        world.Add(
            floor,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.FromInt(10),
                        Fixed32.One))));

        var body =
            world.CreateEntity();

        world.Add(
            body,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromInt(2))));

        var physicsBody =
            PhysicsBody2D.Dynamic(
                Fixed32.One);

        physicsBody.Velocity =
            new FixedVector2(
                Fixed32.Zero,
                Fixed32.FromInt(-60));

        world.Add(
            body,
            physicsBody);

        world.Add(
            body,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.One,
                        Fixed32.One))));

        var settings =
            new PhysicsSettings2D
            {
                Gravity =
                    FixedVector2.Zero,

                Substeps =
                    4
            };

        var physics =
            new PhysicsSystem2D(
                world,
                settings,
                new EventBus());

        physics.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromFloat(0.05f),
                    Tick.Zero)));

        var transform =
            world.Get<WorldTransform2D>(
                body);

        var resultBody =
            world.Get<PhysicsBody2D>(
                body);

        Assert.True(
            transform.Position.Y >=
            Fixed32.FromFloat(0.5f));

        Assert.Equal(
            Fixed32.Zero,
            resultBody.Velocity.Y);
    }

    [Fact]
    public void TriangleOnFloor_UsesCenterOfBottomEdgeAsContactPoint()
    {
        using var world =
            new World();

        var triangle =
            world.CreateEntity();

        var floor =
            world.CreateEntity();

        world.Add(
            triangle,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromFloat(1.5f))));

        world.Add(
            triangle,
            PhysicsBody2D.Static());

        world.Add(
            triangle,
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
                        Fixed32.Zero,
                        Fixed32.One)
                    })));

        world.Add(
            floor,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            floor,
            PhysicsBody2D.Static());

        world.Add(
            floor,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.FromInt(10),
                        Fixed32.One))));

        var physics =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D
                {
                    Gravity =
                        FixedVector2.Zero,

                    Substeps = 1
                },
                new EventBus());

        physics.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.Zero,
                    Tick.Zero)));

        var contact =
            Assert.Single(
                physics.Contacts);

        Assert.Equal(
            Fixed32.Zero,
            contact.Contact.Position.X);
    }

    [Fact]
    public void TriangleSettlesOnFloorWithoutGainingEnergy()
    {
        using var world =
            new World();

        var floor =
            world.CreateEntity();

        var triangle =
            world.CreateEntity();

        world.Add(
            floor,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromFloat(4f))));

        world.Add(
            floor,
            PhysicsBody2D.Static());

        world.Add(
            floor,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.FromInt(32),
                        Fixed32.One))));

        world.Add(
            triangle,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromFloat(18f))));

        world.Add(
            triangle,
            PhysicsBody2D.Dynamic(
                Fixed32.One));

        world.Add(
            triangle,
            new Collider2D(
                new PolygonShape2D(
                    new[]
                    {
                    new FixedVector2(
                        -Fixed32.One,
                        -Fixed32.One),

                    new FixedVector2(
                        Fixed32.One,
                        -Fixed32.One),

                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.One)
                    })));

        var settings =
            new PhysicsSettings2D
            {
                Gravity =
                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.FromFloat(-9.8f)),

                Substeps = 4,

                PositionIterations = 2,
                VelocityIterations = 4
            };

        var physics =
            new PhysicsSystem2D(
                world,
                settings,
                new EventBus());

        for (var i = 0; i < 240; i++)
        {
            physics.FixedUpdate(
                new FixedSystemContext(
                    new SimulationTime(
                        Fixed32.FromFloat(
                            1f / 20f),
                        new Tick((ulong)i))));
        }

        var transform =
            world.Get<WorldTransform2D>(
                triangle);

        var body =
            world.Get<PhysicsBody2D>(
                triangle);

        var expectedY =
            Fixed32.FromFloat(5.5f);

        Assert.True(
            Fixed32.Abs(
                transform.Position.Y -
                expectedY) <=
            Fixed32.FromFloat(0.05f));

        Assert.True(
            Fixed32.Abs(
                body.Velocity.X) <=
            Fixed32.FromFloat(0.05f));

        Assert.True(
            Fixed32.Abs(
                body.Velocity.Y) <=
            Fixed32.FromFloat(0.05f));

        Assert.True(
            Fixed32.Abs(
                body.AngularVelocity) <=
            Fixed32.FromFloat(0.05f));

        Assert.True(
            Fixed32.Abs(
                transform.Rotation) <=
            Fixed32.FromFloat(0.05f));
    }

    [Fact]
    public void AlignedTriangleFloorContactDoesNotCreateAngularVelocity()
    {
        using var world =
            new World();

        var floor =
            world.CreateEntity();

        var triangle =
            world.CreateEntity();

        world.Add(
            floor,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromFloat(4f))));

        world.Add(
            floor,
            PhysicsBody2D.Static());

        world.Add(
            floor,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.FromInt(32),
                        Fixed32.One))));

        world.Add(
            triangle,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromFloat(5.5f))));

        world.Add(
            triangle,
            PhysicsBody2D.Dynamic(
                Fixed32.One));

        world.Add(
            triangle,
            new Collider2D(
                new PolygonShape2D(
                    new[]
                    {
                    new FixedVector2(
                        -Fixed32.One,
                        -Fixed32.One),

                    new FixedVector2(
                        Fixed32.One,
                        -Fixed32.One),

                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.One)
                    })));

        var settings =
            new PhysicsSettings2D
            {
                Gravity =
                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.FromFloat(-9.8f)),

                Substeps = 4,
                PositionIterations = 2,
                VelocityIterations = 4
            };

        var physics =
            new PhysicsSystem2D(
                world,
                settings,
                new EventBus());

        physics.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromFloat(1f / 20f),
                    Tick.Zero)));

        var body =
            world.Get<PhysicsBody2D>(
                triangle);

        var contact =
            Assert.Single(
                physics.Contacts);

        Assert.True(
            Fixed32.Abs(
                body.AngularVelocity) <=
            Fixed32.FromRatio(
                2,
                65536),
            $"AngularVelocity={body.AngularVelocity.ToFloat():F6}");

        Assert.True(
            Fixed32.Abs(
                contact.Contact.Position.X) <=
            Fixed32.FromRatio(
                2,
                65536),
            $"ContactX={contact.Contact.Position.X.ToFloat():F6}");

        Assert.True(
            contact.Contact.Normal.Y !=
            Fixed32.Zero,
            $"Normal={contact.Contact.Normal}");
    }

    [Fact]
    public void DynamicTriangle_RemainsStableOnFlatFloor()
    {
        using var world =
            new World();

        var floor =
            world.CreateEntity();

        world.Add(
            floor,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            floor,
            PhysicsBody2D.Static());

        world.Add(
            floor,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.FromInt(10),
                        Fixed32.One))));

        var triangle =
            world.CreateEntity();

        world.Add(
            triangle,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromInt(5))));

        world.Add(
            triangle,
            PhysicsBody2D.Dynamic(
                Fixed32.One));

        world.Add(
            triangle,
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
                        Fixed32.Zero,
                        Fixed32.One)
                    })));

        var settings =
            new PhysicsSettings2D
            {
                Gravity =
                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.FromFloat(-9.8f)),

                Substeps = 4
            };

        var physics =
            new PhysicsSystem2D(
                world,
                settings,
                new EventBus());

        var context =
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromFloat(0.05f),
                    Tick.Zero));

        for (var i = 0;
             i < 120;
             i++)
        {
            physics.FixedUpdate(
                context);
        }

        var transform =
            world.Get<WorldTransform2D>(
                triangle);

        var body =
            world.Get<PhysicsBody2D>(
                triangle);

        Assert.InRange(
            transform.Position.Y.ToFloat(),
            1.49f,
            1.51f);

        Assert.Equal(
            Fixed32.Zero,
            body.Velocity.Y);

        Assert.Equal(
            Fixed32.Zero,
            body.AngularVelocity);
    }

    [Fact]
    public void RotatedPolygon_UsesWorldRotationForCollision()
    {
        var world =
            new World();

        var polygonEntity =
            world.CreateEntity();

        var circleEntity =
            world.CreateEntity();

        world.Add(
            polygonEntity,
            new WorldTransform2D(
                FixedVector2.Zero)
            {
                Rotation =
                    Fixed32.HalfPi
            });

        world.Add(
            circleEntity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromFloat(1.5f))));

        world.Add(
            polygonEntity,
            PhysicsBody2D.Static());

        world.Add(
            circleEntity,
            PhysicsBody2D.Static());

        world.Add(
            polygonEntity,
            new Collider2D(
                new PolygonShape2D(
                    new[]
                    {
                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.FromInt(-1)),

                    new FixedVector2(
                        Fixed32.FromInt(2),
                        Fixed32.Zero),

                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.One)
                    })));

        world.Add(
            circleEntity,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.FromFloat(0.25f))));

        var physics =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D(),
                new EventBus());

        physics.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.Zero,
                    new Tick(1))));

        Assert.Single(
            physics.Contacts);
    }

    [Fact]
    public void RotatedPolygonCollider_CollidesWithAabbCollider()
    {
        using var world =
            new World();

        var polygonEntity =
            world.CreateEntity();

        var boxEntity =
            world.CreateEntity();

        world.Add(
            polygonEntity,
            new WorldTransform2D(
                FixedVector2.Zero)
            {
                Rotation =
                    Fixed32.Pi /
                    Fixed32.FromInt(4)
            });

        world.Add(
            boxEntity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromFloat(1.5f),
                    Fixed32.Zero)));

        world.Add(
            polygonEntity,
            PhysicsBody2D.Static());

        world.Add(
            boxEntity,
            PhysicsBody2D.Static());

        world.Add(
            polygonEntity,
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

        world.Add(
            boxEntity,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.One,
                        Fixed32.One))));

        var physics =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D(),
                new EventBus());

        physics.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.Zero,
                    Tick.Zero)));

        Assert.Single(
            physics.Contacts);

        Assert.True(
            physics.Contacts[0]
                .Contact
                .Penetration > Fixed32.Zero);
    }

    [Fact]
    public void ContactEvents_ProduceEnterStayAndExit()
    {
        using var world =
            new World();

        var first =
            world.CreateEntity();

        var second =
            world.CreateEntity();

        world.Add(
            first,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            second,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(1),
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
                new CircleShape2D(
                    Fixed32.One)));

        world.Add(
            second,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        var physics =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D(),
                new EventBus());

        var context =
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.Zero,
                    new Tick(1)));

        physics.FixedUpdate(
            context);

        var enter =
            Assert.Single(
                physics.ContactEvents);

        Assert.Equal(
            PhysicsContactType.Collision,
            enter.Type);

        Assert.Equal(
            PhysicsContactPhase.Enter,
            enter.Phase);

        physics.FixedUpdate(
            context);

        var stay =
            Assert.Single(
                physics.ContactEvents);

        Assert.Equal(
            PhysicsContactType.Collision,
            stay.Type);

        Assert.Equal(
            PhysicsContactPhase.Stay,
            stay.Phase);

        ref var secondTransform =
            ref world.Get<WorldTransform2D>(
                second);

        secondTransform.Position =
            new FixedVector2(
                Fixed32.FromInt(5),
                Fixed32.Zero);

        physics.FixedUpdate(
            context);

        var exit =
            Assert.Single(
                physics.ContactEvents);

        Assert.Equal(
            PhysicsContactType.Collision,
            exit.Type);

        Assert.Equal(
            PhysicsContactPhase.Exit,
            exit.Phase);
    }

    [Fact]
    public void TriggerEvents_ProduceTriggerEnterStayAndExit()
    {
        using var world =
            new World();

        var first =
            world.CreateEntity();

        var second =
            world.CreateEntity();

        world.Add(
            first,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            second,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(1),
                    Fixed32.Zero)));

        world.Add(
            first,
            PhysicsBody2D.Static());

        world.Add(
            second,
            PhysicsBody2D.Static());

        var firstCollider =
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One));

        firstCollider.IsTrigger =
            true;

        world.Add(
            first,
            firstCollider);

        world.Add(
            second,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        var physics =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D(),
                new EventBus());

        var context =
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.Zero,
                    new Tick(1)));

        physics.FixedUpdate(
            context);

        var enter =
            Assert.Single(
                physics.ContactEvents);

        Assert.Equal(
            PhysicsContactType.Trigger,
            enter.Type);

        Assert.Equal(
            PhysicsContactPhase.Enter,
            enter.Phase);

        Assert.Empty(
            physics.Contacts);

        physics.FixedUpdate(
            context);

        var stay =
            Assert.Single(
                physics.ContactEvents);

        Assert.Equal(
            PhysicsContactType.Trigger,
            stay.Type);

        Assert.Equal(
            PhysicsContactPhase.Stay,
            stay.Phase);

        ref var secondTransform =
            ref world.Get<WorldTransform2D>(
                second);

        secondTransform.Position =
            new FixedVector2(
                Fixed32.FromInt(5),
                Fixed32.Zero);

        physics.FixedUpdate(
            context);

        var exit =
            Assert.Single(
                physics.ContactEvents);

        Assert.Equal(
            PhysicsContactType.Trigger,
            exit.Type);

        Assert.Equal(
            PhysicsContactPhase.Exit,
            exit.Phase);

        Assert.Empty(
            physics.Contacts);
    }

    [Fact]
    public void DisablingCollider_ProducesExitEvent()
    {
        using var world =
            new World();

        var first =
            world.CreateEntity();

        var second =
            world.CreateEntity();

        world.Add(
            first,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            second,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(1),
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
                new CircleShape2D(
                    Fixed32.One)));

        world.Add(
            second,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        var physics =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D(),
                new EventBus());

        var context =
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.Zero,
                    new Tick(1)));

        physics.FixedUpdate(
            context);

        Assert.Equal(
            PhysicsContactPhase.Enter,
            Assert.Single(
                physics.ContactEvents).Phase);

        ref var collider =
            ref world.Get<Collider2D>(
                second);

        collider.Enabled =
            false;

        physics.FixedUpdate(
            context);

        var exit =
            Assert.Single(
                physics.ContactEvents);

        Assert.Equal(
            PhysicsContactType.Collision,
            exit.Type);

        Assert.Equal(
            PhysicsContactPhase.Exit,
            exit.Phase);

        Assert.Empty(
            physics.Contacts);
    }

    [Fact]
    public void DestroyingEntity_ProducesExitEvent()
    {
        using var world =
            new World();

        var first =
            world.CreateEntity();

        var second =
            world.CreateEntity();

        world.Add(
            first,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            second,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(1),
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
                new CircleShape2D(
                    Fixed32.One)));

        world.Add(
            second,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        var physics =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D(),
                new EventBus());

        var context =
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.Zero,
                    new Tick(1)));

        physics.FixedUpdate(
            context);

        Assert.Equal(
            PhysicsContactPhase.Enter,
            Assert.Single(
                physics.ContactEvents).Phase);

        Assert.True(
            world.DestroyEntity(
                second));

        physics.FixedUpdate(
            context);

        var exit =
            Assert.Single(
                physics.ContactEvents);

        Assert.Equal(
            PhysicsContactType.Collision,
            exit.Type);

        Assert.Equal(
            PhysicsContactPhase.Exit,
            exit.Phase);

        Assert.Empty(
            physics.Contacts);
    }

    [Fact]
    public void CircleCollider_CollidesWithPolygonCollider()
    {
        var world =
            new World();

        var circleEntity =
            world.CreateEntity();

        var polygonEntity =
            world.CreateEntity();

        world.Add(
            circleEntity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromFloat(1.5f),
                    Fixed32.Zero)));

        world.Add(
            polygonEntity,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            circleEntity,
            PhysicsBody2D.Static());

        world.Add(
            polygonEntity,
            PhysicsBody2D.Static());

        world.Add(
            circleEntity,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        world.Add(
            polygonEntity,
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

        var physics =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D(),
                new EventBus());

        physics.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromFloat(1f / 60f),
                    new Tick(1))));

        Assert.Single(
            physics.Contacts);

        Assert.Equal(
            new FixedVector2(
                Fixed32.FromInt(-1),
                Fixed32.Zero),
            physics.Contacts[0]
                .Contact
                .Normal);

        Assert.True(
            physics.Contacts[0]
                .Contact
                .Penetration > Fixed32.Zero);
    }

    [Fact]
    public void PolygonCollider_CollidesWithPolygonCollider()
    {
        var world =
            new World();

        var firstEntity =
            world.CreateEntity();

        var secondEntity =
            world.CreateEntity();

        world.Add(
            firstEntity,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            secondEntity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromFloat(1.5f),
                    Fixed32.Zero)));

        world.Add(
            firstEntity,
            PhysicsBody2D.Static());

        world.Add(
            secondEntity,
            PhysicsBody2D.Static());

        var polygon =
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
                });

        world.Add(
            firstEntity,
            new Collider2D(
                polygon));

        world.Add(
            secondEntity,
            new Collider2D(
                polygon));

        var physics =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D(),
                new EventBus());

        physics.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromFloat(1f / 60f),
                    new Tick(1))));

        Assert.Single(
            physics.Contacts);

        Assert.True(
            physics.Contacts[0]
                .Contact
                .Penetration > Fixed32.Zero);

        Assert.Equal(
            new FixedVector2(
                Fixed32.One,
                Fixed32.Zero),
            physics.Contacts[0]
                .Contact
                .Normal);
    }

    [Fact]
    public void PolygonCollider_DoesNotCollideWithSeparatedPolygonCollider()
    {
        var world =
            new World();

        var firstEntity =
            world.CreateEntity();

        var secondEntity =
            world.CreateEntity();

        world.Add(
            firstEntity,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            secondEntity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(4),
                    Fixed32.Zero)));

        world.Add(
            firstEntity,
            PhysicsBody2D.Static());

        world.Add(
            secondEntity,
            PhysicsBody2D.Static());

        var polygon =
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
                });

        world.Add(
            firstEntity,
            new Collider2D(
                polygon));

        world.Add(
            secondEntity,
            new Collider2D(
                polygon));

        var physics =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D(),
                new EventBus());

        physics.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromFloat(1f / 60f),
                    new Tick(1))));

        Assert.Empty(
            physics.Contacts);
    }

    [Fact]
    public void DynamicBody_SleepsAfterRemainingBelowThreshold()
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
            PhysicsBody2D.Dynamic(
                Fixed32.One));

        var settings =
            new PhysicsSettings2D
            {
                Gravity =
                    FixedVector2.Zero,

                SleepLinearVelocityThreshold =
                    Fixed32.FromFloat(0.01f),

                SleepAngularVelocityThreshold =
                    Fixed32.FromFloat(0.01f),

                SleepTime =
                    Fixed32.FromFloat(0.2f)
            };

        var physics =
            new PhysicsSystem2D(
                world,
                settings,
                new EventBus());

        var context =
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromFloat(0.1f),
                    new Tick(1)));

        physics.FixedUpdate(context);
        physics.FixedUpdate(context);

        Assert.True(
            world.Get<PhysicsBody2D>(
                entity).IsSleeping);

        Assert.Equal(
            FixedVector2.Zero,
            world.Get<PhysicsBody2D>(
                entity).Velocity);

        Assert.Equal(
            Fixed32.Zero,
            world.Get<PhysicsBody2D>(
                entity).AngularVelocity);
    }

    [Fact]
    public void Force_WakesSleepingBody()
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
            PhysicsBody2D.Dynamic(
                Fixed32.One));

        var body =
            world.Get<PhysicsBody2D>(
                entity);

        body.Sleep();
        world.Get<PhysicsBody2D>(
            entity) = body;

        ref var sleepingBody =
            ref world.Get<PhysicsBody2D>(
                entity);

        sleepingBody.AddForce(
            new FixedVector2(
                Fixed32.One,
                Fixed32.Zero));

        Assert.False(
            sleepingBody.IsSleeping);
    }

    [Fact]
    public void ActiveKinematicContact_WakesSleepingDynamicBody()
    {
        var world =
            new World();

        var dynamicEntity =
            world.CreateEntity();

        var kinematicEntity =
            world.CreateEntity();

        world.Add(
            dynamicEntity,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            dynamicEntity,
            PhysicsBody2D.Dynamic(
                Fixed32.One));

        world.Add(
            dynamicEntity,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        ref var dynamicBody =
            ref world.Get<PhysicsBody2D>(
                dynamicEntity);

        dynamicBody.Sleep();

        world.Add(
            kinematicEntity,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            kinematicEntity,
            PhysicsBody2D.Kinematic());

        world.Add(
            kinematicEntity,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        var physics =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D
                {
                    Gravity =
                        FixedVector2.Zero
                },
                new EventBus());

        physics.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromFloat(0.1f),
                    new Tick(1))));

        Assert.False(
            world.Get<PhysicsBody2D>(
                dynamicEntity).IsSleeping);
    }

    [Fact]
    public void CircleCollider_DoesNotCollideWithSeparatedPolygonCollider()
    {
        var world =
            new World();

        var circleEntity =
            world.CreateEntity();

        var polygonEntity =
            world.CreateEntity();

        world.Add(
            circleEntity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(4),
                    Fixed32.Zero)));

        world.Add(
            polygonEntity,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            circleEntity,
            PhysicsBody2D.Static());

        world.Add(
            polygonEntity,
            PhysicsBody2D.Static());

        world.Add(
            circleEntity,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        world.Add(
            polygonEntity,
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

        var physics =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D(),
                new EventBus());

        physics.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromFloat(1f / 60f),
                    new Tick(1))));

        Assert.Empty(
            physics.Contacts);
    }

    [Fact]
    public void PolygonCollider_CollidesWithAabbCollider()
    {
        var world =
            new World();

        var polygonEntity =
            world.CreateEntity();

        var boxEntity =
            world.CreateEntity();

        world.Add(
            polygonEntity,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            boxEntity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromFloat(1.5f),
                    Fixed32.Zero)));

        world.Add(
            polygonEntity,
            PhysicsBody2D.Static());

        world.Add(
            boxEntity,
            PhysicsBody2D.Static());

        world.Add(
            polygonEntity,
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
                        Fixed32.Zero,
                        Fixed32.One)
                    })));

        world.Add(
            boxEntity,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.FromInt(2),
                        Fixed32.FromInt(2)))));

        var events =
            new EventBus();

        var physics =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D(),
                events);

        physics.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromFloat(1f / 60f),
                    new Tick(1))));

        Assert.Single(
            physics.Contacts);

        Assert.True(
            physics.Contacts[0]
                .Contact
                .Penetration > Fixed32.Zero);
    }

    [Fact]
    public void CircleCollider_CollidesWithCircleCollider()
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
                    Fixed32.Zero,
                    Fixed32.Zero)));

        world.Add(
            second,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(1),
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
                new CircleShape2D(
                    Fixed32.One)));

        world.Add(
            second,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        var events =
            new EventBus();

        var physics =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D(),
                events);

        physics.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromFloat(1f / 60f),
                    new Tick(1))));

        Assert.Single(
            physics.Contacts);

        Assert.Equal(
            Fixed32.One,
            physics.Contacts[0]
                .Contact
                .Penetration);
    }

    [Fact]
    public void CircleCollider_CollidesWithAabbCollider()
    {
        var world =
            new World();

        var circleEntity =
            world.CreateEntity();

        var boxEntity =
            world.CreateEntity();

        world.Add(
            circleEntity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromFloat(1.5f),
                    Fixed32.Zero)));

        world.Add(
            boxEntity,
            new WorldTransform2D(
                FixedVector2.Zero));

        world.Add(
            circleEntity,
            PhysicsBody2D.Static());

        world.Add(
            boxEntity,
            PhysicsBody2D.Static());

        world.Add(
            circleEntity,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        world.Add(
            boxEntity,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.FromInt(2),
                        Fixed32.FromInt(2)))));

        var events =
            new EventBus();

        var physics =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D(),
                events);

        physics.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromFloat(1f / 60f),
                    new Tick(1))));

        Assert.Single(
            physics.Contacts);

        Assert.Equal(
            new FixedVector2(
                Fixed32.FromInt(-1),
                Fixed32.Zero),
            physics.Contacts[0]
                .Contact
                .Normal);
    }

    [Fact]
    public void CircleCollider_DoesNotCollideWhenSeparated()
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
                FixedVector2.Zero));

        world.Add(
            second,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(3),
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
                new CircleShape2D(
                    Fixed32.One)));

        world.Add(
            second,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        var events =
            new EventBus();

        var physics =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D(),
                events);

        physics.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromFloat(1f / 60f),
                    new Tick(1))));

        Assert.Empty(
            physics.Contacts);
    }

    [Fact]
    public void AngularDampingReducesAngularVelocity()
    {
        using var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new WorldTransform2D(
                FixedVector2.Zero));

        var body =
            PhysicsBody2D.Dynamic(
                Fixed32.One,
                Fixed32.One);

        body.AngularVelocity =
            Fixed32.FromInt(4);

        body.AngularDamping =
            Fixed32.One;

        world.Add(
            entity,
            body);

        var system =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D
                {
                    Gravity =
                        FixedVector2.Zero
                },
                new EventBus());

        system.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.One,
                    Tick.Zero)));

        Assert.Equal(
            Fixed32.FromInt(2),
            world.Get<PhysicsBody2D>(
                entity).AngularVelocity);
    }

    [Fact]
    public void OffCenterCollisionChangesAngularVelocity()
    {
        using var world =
            new World();

        var dynamicEntity =
            world.CreateEntity();

        var staticEntity =
            world.CreateEntity();

        world.Add(
            dynamicEntity,
            new WorldTransform2D(
                FixedVector2.Zero));

        var dynamicBody =
            PhysicsBody2D.Dynamic(
                Fixed32.One,
                Fixed32.One);

        dynamicBody.Velocity =
            new FixedVector2(
                Fixed32.One,
                Fixed32.Zero);

        world.Add(
            dynamicEntity,
            dynamicBody);

        var dynamicCollider =
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.FromInt(2),
                        Fixed32.FromInt(4))));

        dynamicCollider.Material =
            new PhysicsMaterial2D(
                Fixed32.Zero,
                Fixed32.One);

        world.Add(
            dynamicEntity,
            dynamicCollider);

        world.Add(
            staticEntity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromFloat(0.5f),
                    Fixed32.One)));

        var staticCollider =
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.FromInt(2),
                        Fixed32.FromInt(2))));

        staticCollider.Material =
            new PhysicsMaterial2D(
                Fixed32.Zero,
                Fixed32.One);

        world.Add(
            staticEntity,
            PhysicsBody2D.Static());

        world.Add(
            staticEntity,
            staticCollider);

        var settings =
            new PhysicsSettings2D
            {
                Gravity =
                    FixedVector2.Zero,
                PositionIterations = 0,
                VelocityIterations = 1
            };

        var system =
            new PhysicsSystem2D(
                world,
                settings,
                new EventBus());

        system.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.Zero,
                    Tick.Zero)));

        Assert.Equal(
            1.0f,
            world.Get<PhysicsBody2D>(
                dynamicEntity).AngularVelocity.ToFloat(),
            3);
    }

    [Fact]
    public void LinearDampingReducesVelocity()
    {
        using var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new WorldTransform2D(
                FixedVector2.Zero));

        var body =
            PhysicsBody2D.Dynamic(
                Fixed32.One);

        body.Velocity =
            new FixedVector2(
                Fixed32.FromInt(4),
                Fixed32.Zero);

        body.LinearDamping =
            Fixed32.One;

        world.Add(
            entity,
            body);

        var system =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D
                {
                    Gravity =
                        FixedVector2.Zero
                },
                new EventBus());

        system.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.One,
                    Tick.Zero)));

        Assert.Equal(
            Fixed32.FromInt(2),
            world.Get<PhysicsBody2D>(
                entity).Velocity.X);
    }

    [Fact]
    public void DynamicBodyMovesWorldTransformByVelocity()
    {
        using var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new WorldTransform2D(
                FixedVector2.Zero));

        var body =
            PhysicsBody2D.Dynamic(
                Fixed32.One);

        body.Velocity =
            new FixedVector2(
                Fixed32.FromInt(2),
                Fixed32.FromInt(3));

        world.Add(
            entity,
            body);

        var system =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D
                {
                    Gravity =
                        FixedVector2.Zero
                },
                new EventBus());

        system.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.One,
                    Tick.Zero)));

        var transform =
            world.Get<WorldTransform2D>(
                entity);

        Assert.Equal(
            Fixed32.FromInt(2),
            transform.Position.X);

        Assert.Equal(
            Fixed32.FromInt(3),
            transform.Position.Y);
    }

    [Fact]
    public void StaticBodyDoesNotMoveWorldTransform()
    {
        using var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(10),
                    Fixed32.FromInt(20))));

        var body =
            PhysicsBody2D.Static();

        body.Velocity =
            new FixedVector2(
                Fixed32.FromInt(5),
                Fixed32.FromInt(7));

        world.Add(
            entity,
            body);

        var system =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D
                {
                    Gravity =
                        FixedVector2.Zero
                },
                new EventBus());

        system.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.One,
                    Tick.Zero)));

        var transform =
            world.Get<WorldTransform2D>(
                entity);

        Assert.Equal(
            Fixed32.FromInt(10),
            transform.Position.X);

        Assert.Equal(
            Fixed32.FromInt(20),
            transform.Position.Y);
    }

    [Fact]
    public void CollisionCorrectsWorldTransformPosition()
    {
        using var world =
            new World();

        var dynamicEntity =
            world.CreateEntity();

        var staticEntity =
            world.CreateEntity();

        world.Add(
            dynamicEntity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.Zero)));

        world.Add(
            dynamicEntity,
            PhysicsBody2D.Dynamic(
                Fixed32.One));

        world.Add(
            dynamicEntity,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.One,
                        Fixed32.One))));

        world.Add(
            staticEntity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromFloat(0.5f),
                    Fixed32.Zero)));

        world.Add(
            staticEntity,
            PhysicsBody2D.Static());

        world.Add(
            staticEntity,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.One,
                        Fixed32.One))));

        var settings =
            new PhysicsSettings2D
            {
                Gravity =
                    FixedVector2.Zero,
                PositionIterations = 1,
                VelocityIterations = 0,
                PenetrationSlop = Fixed32.Zero,
                PositionCorrectionPercent = Fixed32.One
            };

        var system =
            new PhysicsSystem2D(
                world,
                settings,
                new EventBus());

        system.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.Zero,
                    Tick.Zero)));

        var transform =
            world.Get<WorldTransform2D>(
                dynamicEntity);

        Assert.Equal(
            Fixed32.FromFloat(-0.5f),
            transform.Position.X);

        Assert.Equal(
            Fixed32.Zero,
            transform.Position.Y);
    }

    [Fact]
    public void CollisionImpulseChangesWorldBodyVelocity()
    {
        using var world =
            new World();

        var first =
            world.CreateEntity();

        var second =
            world.CreateEntity();

        world.Add(
            first,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromFloat(-0.5f),
                    Fixed32.Zero)));

        var firstBody =
            PhysicsBody2D.Dynamic(
                Fixed32.One);

        firstBody.Velocity =
            new FixedVector2(
                Fixed32.One,
                Fixed32.Zero);

        world.Add(
            first,
            firstBody);

        var firstCollider =
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.One,
                        Fixed32.One)));

        firstCollider.Material =
            new PhysicsMaterial2D(
                Fixed32.Zero,
                Fixed32.One);

        world.Add(
            first,
            firstCollider);

        world.Add(
            second,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromFloat(0.5f),
                    Fixed32.Zero)));

        var secondBody =
            PhysicsBody2D.Dynamic(
                Fixed32.One);

        secondBody.Velocity =
            new FixedVector2(
                Fixed32.FromInt(-1),
                Fixed32.Zero);

        world.Add(
            second,
            secondBody);

        var secondCollider =
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.One,
                        Fixed32.One)));

        secondCollider.Material =
            new PhysicsMaterial2D(
                Fixed32.Zero,
                Fixed32.One);

        world.Add(
            second,
            secondCollider);

        var settings =
            new PhysicsSettings2D
            {
                Gravity =
                    FixedVector2.Zero,
                PositionIterations = 0,
                VelocityIterations = 1
            };

        var system =
            new PhysicsSystem2D(
                world,
                settings,
                new EventBus());

        system.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.Zero,
                    Tick.Zero)));

        Assert.Equal(
            Fixed32.FromInt(-1),
            world.Get<PhysicsBody2D>(
                first).Velocity.X);

        Assert.Equal(
            Fixed32.One,
            world.Get<PhysicsBody2D>(
                second).Velocity.X);
    }

    [Fact]
    public void DynamicBodyIntegratesAngularVelocity()
    {
        using var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new WorldTransform2D(
                FixedVector2.Zero));

        var body =
            PhysicsBody2D.Dynamic(
                Fixed32.One,
                Fixed32.FromInt(2));

        body.AngularVelocity =
            Fixed32.FromInt(3);

        world.Add(
            entity,
            body);

        var system =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D
                {
                    Gravity =
                        FixedVector2.Zero
                },
                new EventBus());

        system.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromInt(2),
                    Tick.Zero)));

        var transform =
            world.Get<WorldTransform2D>(
                entity);

        Assert.Equal(
            Fixed32.FromInt(6),
            transform.Rotation);

        Assert.Equal(
            Fixed32.FromInt(3),
            world.Get<PhysicsBody2D>(
                entity).AngularVelocity);
    }

    [Fact]
    public void TorqueUsesInverseInertia()
    {
        using var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new WorldTransform2D(
                FixedVector2.Zero));

        var body =
            PhysicsBody2D.Dynamic(
                Fixed32.One,
                Fixed32.FromInt(2));

        body.AddTorque(
            Fixed32.FromInt(4));

        world.Add(
            entity,
            body);

        var system =
            new PhysicsSystem2D(
                world,
                new PhysicsSettings2D
                {
                    Gravity =
                        FixedVector2.Zero
                },
                new EventBus());

        system.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromFloat(0.5f),
                    Tick.Zero)));

        var result =
            world.Get<PhysicsBody2D>(
                entity);

        Assert.Equal(
            Fixed32.FromInt(1),
            result.AngularVelocity);
    }
}