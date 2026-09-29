using Engine.ECS.Persistence;

namespace Engine.Tests.ECS;

public sealed class WorldStateValidationTests
{
    [Fact]
    public void RestoreState_DuplicateComponentType_DoesNotMutateWorld()
    {
        using var world =
            new Engine.ECS.World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new FirstComponent
            {
                Value = 10
            });

        world.Add(
            entity,
            new SecondComponent
            {
                Value = 20
            });

        var state =
            world.CaptureState();

        var malformedState =
            new EcsWorldState(
                state.Entities,
                new[]
                {
                    state.Components[0],
                    state.Components[0],
                    state.Components[1]
                });

        world.Get<FirstComponent>(entity).Value = 100;

        world.Get<SecondComponent>(entity).Value = 200;

        Assert.Throws<InvalidOperationException>(
            () =>
                world.RestoreState(malformedState));

        Assert.True(
            world.Has<FirstComponent>(entity));

        Assert.True(
            world.Has<SecondComponent>(entity));

        Assert.Equal(
            100,
            world.Get<FirstComponent>(entity).Value);

        Assert.Equal(
            200,
            world.Get<SecondComponent>(entity).Value);
    }

    private struct FirstComponent
    {
        public int Value;
    }

    private struct SecondComponent
    {
        public int Value;
    }
}