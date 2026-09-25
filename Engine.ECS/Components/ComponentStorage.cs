using Engine.Core.Determinism;
using Engine.ECS.Entities;
using Engine.ECS.Persistence;
using Engine.Memory.Collections;

namespace Engine.ECS.Components;

internal sealed class ComponentStorage<T> :
    IComponentStorage
    where T : struct
{
    private ulong _changeVersion;
    private readonly PooledList<EntityId> _entities =
        new();

    private readonly PooledList<T> _components =
        new();

    private readonly SparseIndex _indices =
        new();

    private readonly PooledList<byte> _dirtyFlags =
        new();

    private readonly PooledList<EntityId> _dirtyEntities =
        new();

    private readonly PooledList<EntityId> _removedEntities =
        new();

    public bool IsDeterministic =>
        typeof(IDeterministicState)
            .IsAssignableFrom(
                typeof(T));

    public Type ComponentType =>
        typeof(T);

    public ulong ChangeVersion =>
    _changeVersion;
    public int Count =>
        _entities.Count;

    public object GetBoxed(
        EntityId entity)
    {
        return Get(entity);
    }

    public void Add(
        EntityId entity,
        T component)
    {
        if (TryGetIndex(
                entity,
                out var existingIndex))
        {
            _components[existingIndex] =
                component;

            MarkDirty(
                existingIndex,
                entity);

            return;
        }

        var denseIndex =
            _entities.Count;

        _entities.Add(
            entity);

        _components.Add(
            component);

        _indices.Set(
            entity.Index,
            denseIndex);

        _dirtyFlags.Add(
            1);

        _dirtyEntities.Add(
            entity);

        AdvanceChangeVersion();

        _deterministicOrderDirty =
            true;
    }

    public WorldComponentState CaptureState()
    {
        return new WorldComponentState<T>(
            _entities
                .AsReadOnlySpan()
                .ToArray(),
            _components
                .AsReadOnlySpan()
                .ToArray());
    }
    public bool Has(
        EntityId entity)
    {
        return TryGetIndex(
            entity,
            out _);
    }

    internal void RestorePersistenceState(
    WorldComponentState<T> state)
    {
        ArgumentNullException.ThrowIfNull(
            state);

        _entities
            .AsSpan()
            .Clear();

        _components
            .AsSpan()
            .Clear();

        _dirtyFlags
            .AsSpan()
            .Clear();

        _entities.Clear();
        _components.Clear();
        _dirtyFlags.Clear();
        _dirtyEntities.Clear();
        _removedEntities.Clear();
        _indices.Clear();

        _entities.AddRange(
            state.Entities);

        _components.AddRange(
            state.Components);

        for (var i = 0;
             i < _entities.Count;
             i++)
        {
            _indices.Set(
                _entities[i].Index,
                i);

            _dirtyFlags.Add(
                0);
        }

        _changeVersion = 0;
        _deterministicOrderDirty = true;
    }
    public ref T Get(
        EntityId entity)
    {
        if (!TryGetIndex(
                entity,
                out var index))
        {
            throw new KeyNotFoundException(
                $"Entity " +
                $"{entity.Index}:{entity.Generation} " +
                $"does not have component " +
                $"{typeof(T).Name}.");
        }

        MarkDirty(
            index,
            entity);

        return ref _components[index];
    }

    public bool Remove(
        EntityId entity)
    {
        if (!TryGetIndex(
                entity,
                out var index))
        {
            return false;
        }

        var lastIndex =
            _entities.Count - 1;

        if (index != lastIndex)
        {
            var movedEntity =
                _entities[lastIndex];

            _entities[index] =
                movedEntity;

            _components[index] =
                _components[lastIndex];

            _dirtyFlags[index] =
                _dirtyFlags[lastIndex];

            _indices.Set(
                movedEntity.Index,
                index);
        }

        _entities[lastIndex] =
            default;

        _components[lastIndex] =
            default;

        _dirtyFlags.RemoveAt(
            lastIndex);

        _entities.RemoveAt(
            lastIndex);

        _components.RemoveAt(
            lastIndex);

        _indices.Remove(
            entity.Index);

        _removedEntities.Add(
            entity);
        AdvanceChangeVersion();
        _deterministicOrderDirty =
    true;
        return true;
    }

    public void RemoveEntity(
        EntityId entity)
    {
        Remove(entity);
    }

    internal ref T GetByIndex(
        int index)
    {
        var entity =
            _entities[index];

        MarkDirty(
            index,
            entity);

        return ref _components[index];
    }

    internal void MarkDirtyByIndex(
        int index)
    {
        MarkDirty(
            index,
            _entities[index]);
    }

    public EntityId GetEntity(
        int index)
    {
        return _entities[index];
    }

    public ReadOnlySpan<EntityId> GetDirtyEntities()
    {
        return _dirtyEntities.AsReadOnlySpan();
    }



    public ReadOnlySpan<EntityId> GetRemovedEntities()
    {
        return _removedEntities.AsReadOnlySpan();
    }

    public void ClearDirty()
    {
        _dirtyFlags
            .AsSpan()
            .Clear();

        _dirtyEntities.Clear();
        _removedEntities.Clear();
    }

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        if (!IsDeterministic)
        {
            return;
        }

        EnsureDeterministicOrder();

        hasher.AddInt32(
            _deterministicEntities.Count);

        foreach (var entity
                 in _deterministicEntities
                     .AsReadOnlySpan())
        {
            hasher.AddUInt32(
                entity.Index);

            hasher.AddUInt32(
                entity.Generation);

            if (!TryGetIndex(
                    entity,
                    out var index))
            {
                throw new InvalidOperationException(
                    "Component storage index is inconsistent.");
            }

            if (_components[index]
                is IDeterministicState state)
            {
                state.AddToHash(
                    ref hasher);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Component type '{typeof(T).Name}' " +
                    "was marked deterministic but does not implement " +
                    "IDeterministicState.");
            }
        }
    }

    public IComponentStorageSnapshot CreateSnapshot()
    {
        return new ComponentStorageSnapshot<T>(
            _entities
                .AsReadOnlySpan()
                .ToArray(),
            _components
                .AsReadOnlySpan()
                .ToArray());
    }

    public void RestoreSnapshot(
        IComponentStorageSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(
            snapshot);

        if (snapshot is not
            ComponentStorageSnapshot<T> typedSnapshot)
        {
            throw new InvalidOperationException(
                $"Snapshot type " +
                $"'{snapshot.ComponentType.Name}' " +
                $"does not match storage type " +
                $"'{typeof(T).Name}'.");
        }

        _entities
            .AsSpan()
            .Clear();

        _components
            .AsSpan()
            .Clear();

        _dirtyFlags
            .AsSpan()
            .Clear();

        _entities.Clear();
        _components.Clear();
        _dirtyFlags.Clear();
        _dirtyEntities.Clear();
        _removedEntities.Clear();
        _indices.Clear();

        _entities.AddRange(
            typedSnapshot.Entities);

        _components.AddRange(
            typedSnapshot.Components);

        if (_entities.Count !=
            _components.Count)
        {
            throw new InvalidOperationException(
                "Component snapshot contains mismatched entity and component counts.");
        }

        for (var i = 0;
             i < _entities.Count;
             i++)
        {
            _indices.Set(
                _entities[i].Index,
                i);

            _dirtyFlags.Add(
                0);
        }

        _changeVersion = 0;

        _deterministicOrderDirty =
            true;
    }

    public void Dispose()
    {
        _entities.Dispose();
        _components.Dispose();
        _dirtyFlags.Dispose();
        _dirtyEntities.Dispose();
        _removedEntities.Dispose();
        _indices.Dispose();
        _deterministicEntities.Dispose();
    }

    private readonly PooledList<EntityId>
        _deterministicEntities =
        new();

    private bool _deterministicOrderDirty = true;

    private void MarkDirty(
        int index,
        EntityId entity)
    {
        if (_dirtyFlags[index] != 0)
        {
            return;
        }

        _dirtyFlags[index] =
            1;

        _dirtyEntities.Add(
            entity);

        AdvanceChangeVersion();
    }

    private void AdvanceChangeVersion()
    {
        _changeVersion =
            checked(
                _changeVersion + 1);
    }

    private void EnsureDeterministicOrder()
    {
        if (!_deterministicOrderDirty)
        {
            return;
        }

        _deterministicEntities.Clear();

        _deterministicEntities.AddRange(
            _entities.AsReadOnlySpan());

        _deterministicEntities
            .AsSpan()
            .Sort(
                EntityIdComparer.Instance);

        _deterministicOrderDirty =
            false;
    }

    private bool TryGetIndex(
        EntityId entity,
        out int index)
    {
        if (!_indices.TryGet(
                entity.Index,
                out index))
        {
            return false;
        }

        if ((uint)index >=
            (uint)_entities.Count)
        {
            index = 0;
            return false;
        }

        return _entities[index] ==
               entity;
    }

    private sealed class EntityIdComparer :
        IComparer<EntityId>
    {
        public static readonly EntityIdComparer Instance =
            new();

        public int Compare(
            EntityId left,
            EntityId right)
        {
            var indexComparison =
                left.Index.CompareTo(
                    right.Index);

            if (indexComparison != 0)
            {
                return indexComparison;
            }

            return left.Generation.CompareTo(
                right.Generation);
        }
    }
}