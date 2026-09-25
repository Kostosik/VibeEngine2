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
                    record =>
                        record.Lifecycle.Position.X)
                .ThenBy(
                    record =>
                        record.Lifecycle.Position.Y)
                .Select(
                    record =>
                        CaptureChunk(
                            world,
                            record))
                .ToArray();

        return new WorldSaveState(
            world.ChunkSize,
            world.EcsWorld.CaptureState(),
            chunks);
    }

    internal static ChunkSaveState CaptureChunk(
        World world,
        ChunkRecord record)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        ArgumentNullException.ThrowIfNull(
            record);

        var lifecycle =
            record.Lifecycle;

        if (lifecycle.Residency ==
            ChunkResidencyState.Unloaded)
        {
            if (world.ChunkPersistence.TryLoad(
                    lifecycle.Position,
                    out var persisted) &&
                persisted is not null)
            {
                return persisted;
            }

            throw new InvalidOperationException(
                $"Unloaded chunk '{lifecycle.Position}' has no persisted state.");
        }

        if (lifecycle.Residency !=
            ChunkResidencyState.Loaded)
        {
            throw new InvalidOperationException(
                $"Chunk '{lifecycle.Position}' cannot be saved while " +
                $"its residency state is '{lifecycle.Residency}'.");
        }

        var chunk =
            record.Chunk;

        if (chunk is null)
        {
            throw new InvalidOperationException(
                $"Loaded chunk '{lifecycle.Position}' has no runtime data.");
        }

        var values =
            chunk.Tiles.AsValueReadOnlySpan();

        var tiles =
            new Tile[values.Length];

        for (var i = 0;
             i < values.Length;
             i++)
        {
            tiles[i] =
                new Tile(
                    values[i]);
        }

        return new ChunkSaveState(
            lifecycle.Position,
            ChunkResidencyState.Loaded,
            lifecycle.Simulation,
            lifecycle.Presentation,
            tiles);
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

        if (world.ChunkCount != 0 ||
            world.GetChunkRecords().Any())
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