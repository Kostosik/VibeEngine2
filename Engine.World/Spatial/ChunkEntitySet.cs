using System.Runtime.InteropServices;
using Engine.ECS.Entities;

namespace Engine.Worlds.Spatial;

internal sealed class ChunkEntitySet
{
    private readonly List<EntityId> _entities = new();
    private readonly Dictionary<EntityId, int> _indices = new();

    public int Count =>
        _entities.Count;

    public bool Contains(
        EntityId entity)
    {
        return _indices.ContainsKey(entity);
    }

    public bool Add(
        EntityId entity)
    {
        if (Contains(entity))
        {
            return false;
        }

        var index =
            _entities.Count;

        _entities.Add(entity);
        _indices.Add(entity, index);

        return true;
    }

    public bool Remove(
        EntityId entity)
    {
        if (!_indices.TryGetValue(
                entity,
                out var index))
        {
            return false;
        }

        var lastIndex =
            _entities.Count - 1;

        if (index != lastIndex)
        {
            var lastEntity =
                _entities[lastIndex];

            _entities[index] =
                lastEntity;

            _indices[lastEntity] =
                index;
        }

        _entities.RemoveAt(lastIndex);
        _indices.Remove(entity);

        return true;
    }

    public ReadOnlySpan<EntityId> AsReadOnlySpan()
    {
        return CollectionsMarshal
            .AsSpan(_entities);
    }
}