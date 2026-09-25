using Engine.ECS;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Tooling.Validation;

namespace Engine.Tests.Validation;

public sealed class WorldValidatorTests
{
    [Fact]
    public void Validate_EmptyWorld_ReturnsValidResult()
    {
        var world =
            new World();

        var validator =
            new WorldValidator(world);

        var result =
            validator.Validate(
                new ValidationContext());

        Assert.True(
            result.IsValid);

        Assert.Empty(
            result.Issues);
    }

    [Fact]
    public void Validate_ValidWorld_ReturnsValidResult()
    {
        var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new Transform2D());

        var validator =
            new WorldValidator(world);

        var result =
            validator.Validate(
                new ValidationContext());

        Assert.True(
            result.IsValid);

        Assert.Empty(
            result.Issues);
    }

    [Fact]
    public void Validate_UsesInspectorEntities()
    {
        var world =
            new World();

        world.CreateEntity();
        world.CreateEntity();

        var validator =
            new WorldValidator(world);

        var result =
            validator.Validate(
                new ValidationContext());

        Assert.True(
            result.IsValid);

        Assert.Empty(
            result.Issues);
    }
}