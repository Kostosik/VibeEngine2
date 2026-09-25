using Engine.Worlds.Spatial;

namespace Engine.Worlds.Chunks;

public sealed class ChunkStreamingPolicy
{
    public ChunkStreamingPolicy(
        int preloadRadius,
        int presentationRadius)
    {
        if (preloadRadius < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(preloadRadius));
        }

        if (presentationRadius < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(presentationRadius));
        }

        if (presentationRadius >
            preloadRadius)
        {
            throw new ArgumentException(
                "Presentation radius cannot exceed preload radius.",
                nameof(presentationRadius));
        }

        PreloadRadius =
            preloadRadius;

        PresentationRadius =
            presentationRadius;
    }

    public int PreloadRadius { get; }

    public int PresentationRadius { get; }

    public ChunkStreamingRequest Evaluate(
        ChunkStreamingInterest interest)
    {
        var preload =
            BuildRegion(
                interest.Center,
                PreloadRadius);

        var presentation =
            BuildRegion(
                interest.Center,
                PresentationRadius);

        return new ChunkStreamingRequest(
            preload,
            presentation);
    }

    private static ChunkPosition[] BuildRegion(
        ChunkPosition center,
        int radius)
    {
        var size =
            checked(
                radius * 2 + 1);

        var count =
            checked(
                size * size);

        var result =
            new ChunkPosition[count];

        var index =
            0;

        for (var y = -radius;
             y <= radius;
             y++)
        {
            for (var x = -radius;
                 x <= radius;
                 x++)
            {
                result[index++] =
                    new ChunkPosition(
                        checked(center.X + x),
                        checked(center.Y + y));
            }
        }

        return result;
    }
}