using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.Physics.Components;
using Engine.Physics.Shapes;
using Engine.Tooling.Validation;

namespace Engine.Tests.Validation;

public sealed class PhysicsValidatorTests
{
    [Fact]
    public void Validate_ColliderWithoutPhysicsBody_ReturnsError()
    {
        using var world = new World();

        var entity = world.CreateEntity();

        world.Add(
            entity,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.One,
                        Fixed32.One))));

        var validator = new PhysicsValidator(world);

        var result = validator.Validate(new ValidationContext());

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code == "PHYSICS_COLLIDER_MISSING_BODY");
    }

    [Fact]
    public void Validate_BodyWithoutWorldTransform_ReturnsError()
    {
        using var world = new World();

        var entity = world.CreateEntity();

        world.Add(
            entity,
            PhysicsBody2D.Dynamic(Fixed32.One));

        var validator = new PhysicsValidator(world);

        var result = validator.Validate(new ValidationContext());

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code == "PHYSICS_BODY_MISSING_WORLD_TRANSFORM");
    }

    [Fact]
    public void Validate_DynamicBodyWithParent_ReturnsError()
    {
        using var world = new World();

        var parent = world.CreateEntity();
        var entity = world.CreateEntity();

        world.Add(entity, new WorldTransform2D());
        world.Add(entity, PhysicsBody2D.Dynamic(Fixed32.One));
        world.Add(entity, new TransformParent2D(parent));

        var validator = new PhysicsValidator(world);

        var result = validator.Validate(new ValidationContext());

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code == "PHYSICS_SIMULATED_BODY_HAS_PARENT");
    }

    [Fact]
    public void Validate_KinematicBodyWithParent_ReturnsError()
    {
        using var world = new World();

        var parent = world.CreateEntity();
        var entity = world.CreateEntity();

        world.Add(entity, new WorldTransform2D());
        world.Add(entity, PhysicsBody2D.Kinematic());
        world.Add(entity, new TransformParent2D(parent));

        var validator = new PhysicsValidator(world);

        var result = validator.Validate(new ValidationContext());

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code == "PHYSICS_SIMULATED_BODY_HAS_PARENT");
    }

    [Fact]
    public void Validate_StaticBodyWithParent_ReturnsValidResult()
    {
        using var world = new World();

        var parent = world.CreateEntity();
        var entity = world.CreateEntity();

        world.Add(entity, new WorldTransform2D());
        world.Add(entity, PhysicsBody2D.Static());
        world.Add(entity, new TransformParent2D(parent));

        var validator = new PhysicsValidator(world);

        var result = validator.Validate(new ValidationContext());

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }
}