using Engine.ECS;
using Engine.ECS.Components;
using Engine.Physics.Components;
using Engine.Tooling.Validation;

namespace Engine.Tests.Validation;

public sealed class PhysicsValidatorTests
{
    [Fact]
    public void Validate_DynamicBodyWithParent_ReturnsError()
    {
        var world =
            new World();

        var parent =
            world.CreateEntity();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            PhysicsBody2D.Dynamic(
                Engine.Core.Math.Fixed32.One));

        world.Add(
            entity,
            new TransformParent2D(
                parent));

        var validator =
            new PhysicsValidator(
                world);

        var result =
            validator.Validate(
                new ValidationContext());

        Assert.False(
            result.IsValid);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "PHYSICS_SIMULATED_BODY_HAS_PARENT");
    }

    [Fact]
    public void Validate_KinematicBodyWithParent_ReturnsError()
    {
        var world =
            new World();

        var parent =
            world.CreateEntity();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            PhysicsBody2D.Kinematic());

        world.Add(
            entity,
            new TransformParent2D(
                parent));

        var validator =
            new PhysicsValidator(
                world);

        var result =
            validator.Validate(
                new ValidationContext());

        Assert.False(
            result.IsValid);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "PHYSICS_SIMULATED_BODY_HAS_PARENT");
    }

    [Fact]
    public void Validate_StaticBodyWithParent_ReturnsValidResult()
    {
        var world =
            new World();

        var parent =
            world.CreateEntity();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            PhysicsBody2D.Static());

        world.Add(
            entity,
            new TransformParent2D(
                parent));

        var validator =
            new PhysicsValidator(
                world);

        var result =
            validator.Validate(
                new ValidationContext());

        Assert.True(
            result.IsValid);

        Assert.Empty(
            result.Issues);
    }
}