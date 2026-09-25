using Engine.Worlds.Spatial;

namespace Engine.Worlds.Persistence;

public sealed class MemoryChunkPersistence :
    IChunkPersistence
{
    private readonly object _sync =
        new();

    private readonly Dictionary<
        ChunkPosition,
        ChunkSaveState> _states =
        new();

    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _states.Count;
            }
        }
    }

    public void Save(
        ChunkSaveState state)
    {
        ArgumentNullException.ThrowIfNull(
            state);

        lock (_sync)
        {
            _states[state.Position] =
                new ChunkSaveState(
                    state.Position,
                    state.Residency,
                    state.Simulation,
                    state.Presentation,
                    state.Tiles);
        }
    }

    public bool TryLoad(
        ChunkPosition position,
        out ChunkSaveState? state)
    {
        lock (_sync)
        {
            if (!_states.TryGetValue(
                    position,
                    out var stored))
            {
                state = null;
                return false;
            }

            state =
new ChunkSaveState(
    stored.Position,
    stored.Residency,
    stored.Simulation,
    stored.Presentation,
    stored.Tiles);

            return true;
        }
    }

    public void Remove(
        ChunkPosition position)
    {
        lock (_sync)
        {
            _states.Remove(
                position);
        }
    }
}