using Engine.Worlds.Chunks;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Worlds.Chunks;

public sealed class ChunkStreamingPolicyTests
{
    [Fact]
    public void Evaluate_CreatesIndependentResidencyAndPresentationRegions()
    {
        var interest =
            new ChunkStreamingInterest(
                new ChunkPosition(
                    10,
                    20));

        var policy =
            new ChunkStreamingPolicy(
                preloadRadius: 2,
                presentationRadius: 1);

        var request =
            policy.Evaluate(
                interest);

        Assert.Equal(
            25,
            request.ResidentChunks.Count);

        Assert.Equal(
            9,
            request.PresentationChunks.Count);

        Assert.Contains(
            new ChunkPosition(
                10,
                20),
            request.ResidentChunks);

        Assert.Contains(
            new ChunkPosition(
                10,
                20),
            request.PresentationChunks);

        foreach (var position
                 in request.PresentationChunks)
        {
            Assert.Contains(
                position,
                request.ResidentChunks);
        }
    }

    [Fact]
    public void Evaluate_DoesNotDefineSimulationState()
    {
        var policy =
            new ChunkStreamingPolicy(
                preloadRadius: 3,
                presentationRadius: 1);

        var request =
            policy.Evaluate(
                new ChunkStreamingInterest(
                    new ChunkPosition(
                        0,
                        0)));

        Assert.NotEmpty(
            request.ResidentChunks);

        Assert.NotEmpty(
            request.PresentationChunks);
    }
}