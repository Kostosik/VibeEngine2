using Engine.Worlds.Chunks;
using Engine.Worlds.Spatial;
using Engine.Worlds.Tiles;

namespace Engine.Worlds.Persistence;

public sealed class ChunkSaveState
{
    public ChunkSaveState(
        ChunkPosition position,
        ChunkSimulationState simulation,
        ChunkPresentationState presentation,
        Tile[] tiles)
    {
        ArgumentNullException.ThrowIfNull(
            tiles);

        Position = position;
        Simulation = simulation;
        Presentation = presentation;
        Tiles = tiles.ToArray();
    }

    public ChunkPosition Position { get; }

    public ChunkSimulationState Simulation { get; }

    public ChunkPresentationState Presentation { get; }

    public Tile[] Tiles { get; }
}