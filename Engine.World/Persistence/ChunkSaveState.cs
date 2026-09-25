using Engine.Worlds.Chunks;
using Engine.Worlds.Spatial;
using Engine.Worlds.Tiles;

namespace Engine.Worlds.Persistence;

public sealed class ChunkSaveState
{
    public ChunkSaveState(
        ChunkPosition position,
        ChunkResidencyState residency,
        ChunkSimulationState simulation,
        ChunkPresentationState presentation,
        Tile[] tiles)
    {
        ArgumentNullException.ThrowIfNull(
            tiles);

        if (residency !=
                ChunkResidencyState.Loaded &&
            residency !=
                ChunkResidencyState.Unloaded)
        {
            throw new ArgumentException(
                "Only loaded or unloaded chunks can be saved.",
                nameof(residency));
        }

        Position =
            position;

        Residency =
            residency;

        Simulation =
            simulation;

        Presentation =
            presentation;

        Tiles =
            tiles.ToArray();
    }

    public ChunkPosition Position { get; }

    public ChunkResidencyState Residency { get; }

    public ChunkSimulationState Simulation { get; }

    public ChunkPresentationState Presentation { get; }

    public Tile[] Tiles { get; }
}