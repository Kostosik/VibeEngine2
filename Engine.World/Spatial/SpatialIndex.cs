using Engine.ECS.Entities;

namespace Engine.Worlds.Spatial;

public sealed class SpatialIndex
{
    private readonly Dictionary<
        ChunkPosition,
        ChunkEntitySet> _chunkEntities = new();

    private readonly Dictionary<
        EntityId,
        HashSet<ChunkPosition>> _entityChunks = new();

    public int ChunkCount =>
        _chunkEntities.Count;

    public int EntityCount =>
        _entityChunks.Count;

    public bool Add(
        EntityId entity,
        ChunkPosition chunk)
    {
        ValidateEntity(entity);

        var entitySet =
            GetOrCreateChunkSet(chunk);

        if (!entitySet.Add(entity))
        {
            return false;
        }

        if (!_entityChunks.TryGetValue(
                entity,
                out var chunks))
        {
            chunks =
                new HashSet<ChunkPosition>();

            _entityChunks.Add(
                entity,
                chunks);
        }

        chunks.Add(chunk);

        return true;
    }

    public bool Remove(
        EntityId entity,
        ChunkPosition chunk)
    {
        ValidateEntity(entity);

        if (!_chunkEntities.TryGetValue(
                chunk,
                out var entitySet))
        {
            return false;
        }

        if (!entitySet.Remove(entity))
        {
            return false;
        }

        if (_entityChunks.TryGetValue(
                entity,
                out var chunks))
        {
            chunks.Remove(chunk);

            if (chunks.Count == 0)
            {
                _entityChunks.Remove(entity);
            }
        }

        if (entitySet.Count == 0)
        {
            _chunkEntities.Remove(chunk);
        }

        return true;
    }

    public bool RemoveEntity(
        EntityId entity)
    {
        ValidateEntity(entity);

        if (!_entityChunks.TryGetValue(
                entity,
                out var chunks))
        {
            return false;
        }

        foreach (var chunk in chunks)
        {
            if (_chunkEntities.TryGetValue(
                    chunk,
                    out var entitySet))
            {
                entitySet.Remove(entity);

                if (entitySet.Count == 0)
                {
                    _chunkEntities.Remove(chunk);
                }
            }
        }

        _entityChunks.Remove(entity);

        return true;
    }

    public bool Contains(
        EntityId entity,
        ChunkPosition chunk)
    {
        ValidateEntity(entity);

        return
            _chunkEntities.TryGetValue(
                chunk,
                out var entitySet) &&
            entitySet.Contains(entity);
    }

    public ReadOnlySpan<EntityId> GetEntities(
        ChunkPosition chunk)
    {
        if (!_chunkEntities.TryGetValue(
                chunk,
                out var entitySet))
        {
            return ReadOnlySpan<EntityId>.Empty;
        }

        return entitySet.AsReadOnlySpan();
    }

    public IReadOnlyCollection<ChunkPosition> GetChunks(
        EntityId entity)
    {
        ValidateEntity(entity);

        if (!_entityChunks.TryGetValue(
                entity,
                out var chunks))
        {
            return Array.Empty<ChunkPosition>();
        }

        return chunks;
    }

    public bool Move(
        EntityId entity,
        ChunkPosition from,
        ChunkPosition to)
    {
        ValidateEntity(entity);

        if (from == to)
        {
            return Contains(
                entity,
                from);
        }

        if (!Contains(
                entity,
                from))
        {
            return false;
        }

        if (Contains(
                entity,
                to))
        {
            throw new InvalidOperationException(
                $"Entity '{entity.Index}' is already indexed in " +
                $"chunk '{to}'.");
        }

        Remove(
            entity,
            from);

        Add(
            entity,
            to);

        return true;
    }

    private ChunkEntitySet GetOrCreateChunkSet(
        ChunkPosition chunk)
    {
        if (_chunkEntities.TryGetValue(
                chunk,
                out var existing))
        {
            return existing;
        }

        var entitySet =
            new ChunkEntitySet();

        _chunkEntities.Add(
            chunk,
            entitySet);

        return entitySet;
    }

    private static void ValidateEntity(
        EntityId entity)
    {
        if (!entity.IsValid)
        {
            throw new ArgumentException(
                "Entity ID is invalid.",
                nameof(entity));
        }
    }
}