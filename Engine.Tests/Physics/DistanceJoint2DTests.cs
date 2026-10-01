using Engine.Core.Events;
using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Physics;
using Engine.Physics.Components;
using Engine.Physics.Joints;

namespace Engine.Tests.Physics;

public sealed class DistanceJoint2DTests
{
    [Fact]
    public void DistanceJoint_CorrectsPositionToTargetLength()
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
                    Fixed32.FromInt(4),
                    Fixed32.Zero)));

        world.Add(
            first,
            PhysicsBody2D.Dynamic(
                Fixed32.One));

        world.Add(
            second,
            PhysicsBody2D.Static());

        world.Add(
            world.CreateEntity(),
            new DistanceJoint2D(
                first,
                second,
                Fixed32.FromInt(2)));

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

        var firstPosition =
            world.Get<WorldTransform2D>(
                first).Position;

        var secondPosition =
            world.Get<WorldTransform2D>(
                second).Position;

        Assert.Equal(
            Fixed32.FromInt(2),
            FixedVector2.Distance(
                firstPosition,
                secondPosition));
    }

    [Fact]
    public void DistanceJoint_ConstrainsRelativeVelocity()
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
                    Fixed32.FromInt(2),
                    Fixed32.Zero)));

        world.Add(
            first,
            PhysicsBody2D.Dynamic(
                Fixed32.One));

        world.Add(
            second,
            PhysicsBody2D.Dynamic(
                Fixed32.One));

        ref var firstBody =
            ref world.Get<PhysicsBody2D>(
                first);

        firstBody.Velocity =
            new FixedVector2(
                Fixed32.One,
                Fixed32.Zero);

        ref var secondBody =
            ref world.Get<PhysicsBody2D>(
                second);

        secondBody.Velocity =
            new FixedVector2(
                Fixed32.FromInt(-1),
                Fixed32.Zero);

        world.Add(
            world.CreateEntity(),
            new DistanceJoint2D(
                first,
                second,
                Fixed32.FromInt(2)));

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
                    Fixed32.Zero,
                    new Tick(1))));

        var relativeVelocity =
            world.Get<PhysicsBody2D>(
                second).Velocity -
            world.Get<PhysicsBody2D>(
                first).Velocity;

        Assert.Equal(
            Fixed32.Zero,
            relativeVelocity.X);
    }

    [Fact]
    public void DistanceJoint_RejectsInvalidEntities()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new DistanceJoint2D(
                    default,
                    new EntityId(
                        1,
                        1),
                    Fixed32.One));
    }

    [Fact]
    public void DistanceJoint_RejectsNonPositiveLength()
    {
        var first =
            new EntityId(
                1,
                1);

        var second =
            new EntityId(
                2,
                1);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DistanceJoint2D(
                    first,
                    second,
                    Fixed32.Zero));
    }
}