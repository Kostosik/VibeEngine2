using Engine.Worlds.Chunks;
using Engine.Worlds.Spatial;

namespace Engine.Tests.WorldTest.Chunks;

public sealed class ChunkLifecycleTests
{
    [Fact]
    public void SimulatingChunk_RemainsValidWithoutPresentation()
    {
        var lifecycle =
            new ChunkLifecycle(
                new ChunkPosition(
                    10,
                    20));

        lifecycle.BeginLoading();
        lifecycle.CompleteLoading();

        lifecycle.SetSimulation(
            ChunkSimulationState.Simulating);

        lifecycle.SetPresentation(
            ChunkPresentationState.Irrelevant);

        Assert.Equal(
            ChunkResidencyState.Loaded,
            lifecycle.Residency);

        Assert.Equal(
            ChunkSimulationState.Simulating,
            lifecycle.Simulation);

        Assert.Equal(
            ChunkPresentationState.Irrelevant,
            lifecycle.Presentation);
    }

    [Fact]
    public void SimulatingChunk_CannotBeUnloaded()
    {
        var lifecycle =
            new ChunkLifecycle(
                new ChunkPosition(
                    10,
                    20));

        lifecycle.BeginLoading();
        lifecycle.CompleteLoading();

        lifecycle.SetSimulation(
            ChunkSimulationState.Simulating);

        Assert.Throws<InvalidOperationException>(
            lifecycle.BeginUnloading);
    }
}