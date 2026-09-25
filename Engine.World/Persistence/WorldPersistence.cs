using Engine.Worlds.Chunks;
using Engine.Worlds.Tiles;

namespace Engine.Worlds.Persistence;

public static class WorldPersistence
{
    public static WorldSaveState Capture(
        World world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        var chunks =
            world
                .GetChunkRecords()
                .OrderBy(
                    record => record.Lifecycle.Position.X)
                .ThenBy(
                    record => record.Lifecycle.Position.Y)
                .Select(
                    CaptureChunk)
                .ToArray();

        return new WorldSaveState(
            world.ChunkSize,
            world.EcsWorld.CaptureState(),
            chunks);
    }

    private static ChunkSaveState CaptureChunk(
        ChunkRecord record)
    {
        var lifecycle =
            record.Lifecycle;

        if (lifecycle.Residency !=
            ChunkResidencyState.Loaded)
        {
            throw new InvalidOperationException(
                $"Chunk '{lifecycle.Position}' must be loaded before saving.");
        }

        var chunk =
            record.Chunk;

        if (chunk is null)
        {
            throw new InvalidOperationException(
                $"Loaded chunk '{lifecycle.Position}' has no runtime data.");
        }

        return new ChunkSaveState(
            lifecycle.Position,
            lifecycle.Simulation,
            lifecycle.Presentation,
            chunk.Tiles
                .AsValueReadOnlySpan()
                .ToArray()
                .Select(
                    value => new Tile(value))
                .ToArray());
    }

    public static void Restore(
        World world,
        WorldSaveState state)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        ArgumentNullException.ThrowIfNull(
            state);

        if (state.ChunkSize !=
            world.ChunkSize)
        {
            throw new InvalidOperationException(
                "World chunk size does not match the saved world.");
        }

        if (world.ChunkCount != 0)
        {
            throw new InvalidOperationException(
                "World must not contain chunks when restoring a save state.");
        }

        world.EcsWorld.RestoreState(
            state.Ecs);

        foreach (var chunk in
                 state.Chunks)
        {
            world.RestoreChunk(
                chunk);
        }
    }
}