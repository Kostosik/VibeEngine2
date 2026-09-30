using Engine.Core.Events;
using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.Physics;
using Engine.Physics.Components;
using Engine.Physics.Materials;
using Engine.Physics.Shapes;

namespace Engine.Tests.Physics;

public sealed class PhysicsSystem2DTests
{
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