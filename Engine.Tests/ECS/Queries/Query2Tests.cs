namespace Engine.Tests.ECS.Queries;

public sealed class Query2Tests
{
    [Fact]
    public void QueryPair_ProcessesOnlyEntitiesWithBothComponents()
    {
        using var world =
            new Engine.ECS.World();

        var firstOnly =
            world.CreateEntity();

        var secondOnly =
            world.CreateEntity();

        var both =
            world.CreateEntity();

        world.Add(
            firstOnly,
            new FirstComponent());

        world.Add(
            secondOnly,
            new SecondComponent());

        world.Add(
            both,
            new FirstComponent());

        world.Add(
            both,
            new SecondComponent());

        var processed =
            0;

        foreach (var item in
                 world.Query<
                     FirstComponent,
                     SecondComponent>())
        {
            Assert.Equal(
                both,
                item.Entity);

            processed++;
        }

        Assert.Equal(
            1,
            processed);
    }

    private struct FirstComponent
    {
    }

    private struct SecondComponent
    {
    }
}