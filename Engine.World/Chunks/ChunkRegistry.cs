using Engine.Worlds.Spatial;

namespace Engine.Worlds.Chunks;

public sealed class ChunkRegistry
{
    private readonly Dictionary<
        ChunkPosition,
        ChunkRecord> _records =
        new();

    public int Count =>
        _records.Count;

    public int LoadedCount =>
        _records.Count(
            pair =>
                pair.Value.Chunk is not null &&
                pair.Value.Lifecycle.Residency ==
                    ChunkResidencyState.Loaded);

    public IEnumerable<ChunkRecord> Records =>
        _records.Values;

    public IEnumerable<Chunk> LoadedChunks =>
        _records.Values
            .Select(
                record => record.Chunk)
            .Where(
                chunk => chunk is not null)
            .Cast<Chunk>();

    public ChunkRecord Register(
        ChunkPosition position,
        Chunk chunk)
    {
        ArgumentNullException.ThrowIfNull(
            chunk);

        if (chunk.Position !=
            position)
        {
            throw new ArgumentException(
                "Chunk position does not match the registry position.",
                nameof(chunk));
        }

        if (_records.ContainsKey(
                position))
        {
            throw new InvalidOperationException(
                $"Chunk '{position}' is already registered.");
        }

        var lifecycle =
            new ChunkLifecycle(
                position);

        lifecycle.BeginLoading();

        var record =
            new ChunkRecord(
                lifecycle);

        record.Attach(
            chunk);

        _records.Add(
            position,
            record);

        return record;
    }

    public ChunkRecord RegisterUnloaded(
        ChunkPosition position)
    {
        if (_records.ContainsKey(
                position))
        {
            throw new InvalidOperationException(
                $"Chunk '{position}' is already registered.");
        }

        var lifecycle =
            new ChunkLifecycle(
                position);

        var record =
            new ChunkRecord(
                lifecycle);

        _records.Add(
            position,
            record);

        return record;
    }

    public bool TryGet(
        ChunkPosition position,
        out ChunkRecord? record)
    {
        return _records.TryGetValue(
            position,
            out record);
    }

    public bool TryGetLoaded(
        ChunkPosition position,
        out Chunk? chunk)
    {
        if (_records.TryGetValue(
                position,
                out var record) &&
            record.Lifecycle.Residency ==
                ChunkResidencyState.Loaded &&
            record.Chunk is not null)
        {
            chunk =
                record.Chunk;

            return true;
        }

        chunk = null;

        return false;
    }

    public ChunkRecord Get(
        ChunkPosition position)
    {
        if (!_records.TryGetValue(
                position,
                out var record))
        {
            throw new KeyNotFoundException(
                $"Chunk '{position}' is not registered.");
        }

        return record;
    }

    public bool Remove(
        ChunkPosition position)
    {
        if (!_records.TryGetValue(
                position,
                out var record))
        {
            return false;
        }

        if (record.Lifecycle.Residency !=
            ChunkResidencyState.Unloaded)
        {
            throw new InvalidOperationException(
                $"Chunk '{position}' must be unloaded before it can be removed from the registry.");
        }

        return _records.Remove(
            position);
    }
}