using Engine.Core.Determinism;
using Engine.ECS.Persistence;
using Engine.Memory.Collections;

namespace Engine.ECS.Entities;

internal sealed class EntityStore :
    IDisposable
{
    private uint _nextIndex = 1;

    private readonly PooledList<uint> _generations =
        new();

    private readonly PooledList<int> _activePositions =
        new();

    private readonly PooledList<uint> _activeEntities =
        new();

    private readonly PooledList<uint> _freeIndices =
        new();

    public int Count =>
        _activeEntities.Count;

    public EntityId Create()
    {
        var entity =
            Reserve();

        Activate(
            entity);

        return entity;
    }

    public EntityId Reserve()
    {
        uint index;

        if (_freeIndices.Count > 0)
        {
            var freePosition =
                _freeIndices.Count - 1;

            index =
                _freeIndices[freePosition];

            _freeIndices.RemoveAt(
                freePosition);
        }
        else
        {
            if (_nextIndex == uint.MaxValue)
            {
                throw new InvalidOperationException(
                    "Entity index limit has been reached.");
            }

            index =
                _nextIndex++;

            _generations.Add(
                1);

            _activePositions.Add(
                0);
        }

        var generation =
            _generations[
                checked((int)index - 1)];

        return new EntityId(
            index,
            generation);
    }

    public bool Activate(
        EntityId entity)
    {
        ValidateEntityId(
            entity);

        var position =
            GetPositionIndex(
                entity.Index);

        if (_generations[position] !=
            entity.Generation)
        {
            return false;
        }

        if (_activePositions[position] != 0)
        {
            return false;
        }

        var denseIndex =
            _activeEntities.Count;

        _activeEntities.Add(
            entity.Index);

        _activePositions[position] =
            denseIndex + 1;

        return true;
    }

    internal EntityStoreState CaptureState()
    {
        return new EntityStoreState(
            _nextIndex,
            _generations
                .AsReadOnlySpan()
                .ToArray(),
            _activeEntities
                .AsReadOnlySpan()
                .ToArray(),
            _freeIndices
                .AsReadOnlySpan()
                .ToArray());
    }

    internal void RestoreState(
        EntityStoreState state)
    {
        ArgumentNullException.ThrowIfNull(
            state);

        Clear();

        if (state.Generations.Length >
            0 &&
            state.NextIndex !=
                (uint)state.Generations.Length + 1)
        {
            throw new InvalidOperationException(
                "Entity state has an invalid next index.");
        }

        if (state.Generations.Length == 0 &&
            state.NextIndex != 1)
        {
            throw new InvalidOperationException(
                "Empty entity state must have next index 1.");
        }

        foreach (var generation in
                 state.Generations)
        {
            if (generation == 0)
            {
                throw new InvalidOperationException(
                    "Entity generation must be greater than zero.");
            }

            _generations.Add(
                generation);

            _activePositions.Add(
                0);
        }

        var activeSet =
            new HashSet<uint>();

        foreach (var index in
                 state.ActiveIndices)
        {
            ValidateStateIndex(
                index,
                state.Generations.Length);

            if (!activeSet.Add(index))
            {
                throw new InvalidOperationException(
                    $"Entity index '{index}' appears more than once in active state.");
            }

            var denseIndex =
                _activeEntities.Count;

            _activeEntities.Add(
                index);

            _activePositions[
                GetPositionIndex(index)] =
                denseIndex + 1;
        }

        var freeSet =
            new HashSet<uint>();

        foreach (var index in
                 state.FreeIndices)
        {
            ValidateStateIndex(
                index,
                state.Generations.Length);

            if (!freeSet.Add(index))
            {
                throw new InvalidOperationException(
                    $"Entity index '{index}' appears more than once in free state.");
            }

            if (activeSet.Contains(index))
            {
                throw new InvalidOperationException(
                    $"Entity index '{index}' cannot be both active and free.");
            }

            _freeIndices.Add(
                index);
        }

        _nextIndex =
            state.NextIndex;
    }

    private static void ValidateStateIndex(
        uint index,
        int generationCount)
    {
        if (index == 0 ||
            index > (uint)generationCount)
        {
            throw new InvalidOperationException(
                $"Entity index '{index}' is outside the serialized entity state.");
        }
    }
    public bool Exists(
        EntityId entity)
    {
        if (!entity.IsValid)
        {
            return false;
        }

        if (entity.Index >
            (uint)_generations.Count)
        {
            return false;
        }

        var position =
            GetPositionIndex(
                entity.Index);

        return _generations[position] ==
                   entity.Generation &&
               _activePositions[position] != 0;
    }

    public bool Destroy(
        EntityId entity)
    {
        if (!Exists(entity))
        {
            return false;
        }

        var positionIndex =
            GetPositionIndex(
                entity.Index);

        var denseIndex =
            _activePositions[positionIndex] - 1;

        var lastDenseIndex =
            _activeEntities.Count - 1;

        if (denseIndex != lastDenseIndex)
        {
            var movedIndex =
                _activeEntities[lastDenseIndex];

            _activeEntities[denseIndex] =
                movedIndex;

            var movedPosition =
                GetPositionIndex(
                    movedIndex);

            _activePositions[movedPosition] =
                denseIndex + 1;
        }

        _activeEntities[lastDenseIndex] =
            0;

        _activeEntities.RemoveAt(
            lastDenseIndex);

        _activePositions[positionIndex] =
            0;

        var nextGeneration =
            _generations[positionIndex] + 1;

        if (nextGeneration == 0)
        {
            nextGeneration = 1;
        }

        _generations[positionIndex] =
            nextGeneration;

        _freeIndices.Add(
            entity.Index);

        return true;
    }

    public void Clear()
    {
        _generations.Clear();
        _activePositions.Clear();
        _activeEntities.Clear();
        _freeIndices.Clear();

        _nextIndex =
            1;
    }

    internal void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        hasher.AddUInt32(
            _nextIndex);

        hasher.AddInt32(
            _generations.Count);

        for (var i = 0;
             i < _generations.Count;
             i++)
        {
            var index =
                (uint)i + 1;

            hasher.AddUInt32(
                index);

            hasher.AddUInt32(
                _generations[i]);

            hasher.AddBool(
                _activePositions[i] != 0);
        }

        hasher.AddInt32(
            _freeIndices.Count);

        for (var i = 0;
             i < _freeIndices.Count;
             i++)
        {
            hasher.AddUInt32(
                _freeIndices[i]);
        }
    }

    internal EntityStoreSnapshot CreateSnapshot()
    {
        var generations =
            new Dictionary<uint, uint>(
                _generations.Count);

        var activeEntities =
            new HashSet<uint>();

        for (var i = 0;
             i < _generations.Count;
             i++)
        {
            var index =
                (uint)i + 1;

            generations.Add(
                index,
                _generations[i]);

            if (_activePositions[i] != 0)
            {
                activeEntities.Add(
                    index);
            }
        }

        return new EntityStoreSnapshot(
            _nextIndex,
            generations,
            activeEntities,
            _freeIndices
                .AsReadOnlySpan()
                .ToArray());
    }

    internal void RestoreSnapshot(
        EntityStoreSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(
            snapshot);

        Clear();

        var indices =
            snapshot.Generations
                .Keys
                .ToArray();

        Array.Sort(
            indices);

        var expectedIndex =
            1u;

        foreach (var index in indices)
        {
            if (index != expectedIndex)
            {
                throw new InvalidOperationException(
                    "Entity snapshot contains non-contiguous generation indices.");
            }

            _generations.Add(
                snapshot.Generations[index]);

            _activePositions.Add(
                0);

            expectedIndex++;
        }

        _nextIndex =
            snapshot.NextIndex;

        var activeIndices =
            snapshot.ActiveEntities
                .ToArray();

        Array.Sort(
            activeIndices);

        foreach (var index in activeIndices)
        {
            if (index == 0 ||
                index > (uint)_generations.Count)
            {
                throw new InvalidOperationException(
                    $"Entity snapshot contains invalid active entity index '{index}'.");
            }

            if (_activePositions[
                    GetPositionIndex(index)] != 0)
            {
                throw new InvalidOperationException(
                    $"Entity snapshot contains duplicate active entity '{index}'.");
            }

            var denseIndex =
                _activeEntities.Count;

            _activeEntities.Add(
                index);

            _activePositions[
                GetPositionIndex(index)] =
                denseIndex + 1;
        }

        var freeIndices =
            new HashSet<uint>();

        foreach (var index in
                 snapshot.FreeIndices)
        {
            if (index == 0 ||
                index > (uint)_generations.Count)
            {
                throw new InvalidOperationException(
                    $"Entity snapshot contains invalid free entity index '{index}'.");
            }

            if (_activePositions[
                    GetPositionIndex(index)] != 0)
            {
                throw new InvalidOperationException(
                    $"Entity snapshot marks active entity '{index}' as free.");
            }

            if (!freeIndices.Add(index))
            {
                throw new InvalidOperationException(
                    $"Entity snapshot contains duplicate free entity '{index}'.");
            }

            _freeIndices.Add(
                index);
        }
    }

    internal IReadOnlyList<EntityId> GetActiveEntities()
    {
        var entities =
            new List<EntityId>(
                _activeEntities.Count);

        foreach (var index in
                 _activeEntities.AsReadOnlySpan())
        {
            var generation =
                _generations[
                    GetPositionIndex(index)];

            entities.Add(
                new EntityId(
                    index,
                    generation));
        }

        entities.Sort(
            static (left, right) =>
                left.Index.CompareTo(
                    right.Index));

        return entities;
    }

    public void Dispose()
    {
        _generations.Dispose();
        _activePositions.Dispose();
        _activeEntities.Dispose();
        _freeIndices.Dispose();
    }

    private int GetPositionIndex(
        uint index)
    {
        return checked(
            (int)index - 1);
    }

    private void ValidateEntityId(
        EntityId entity)
    {
        if (!entity.IsValid)
        {
            throw new ArgumentException(
                "Entity ID must be valid.",
                nameof(entity));
        }

        if (entity.Index >
            (uint)_generations.Count)
        {
            throw new ArgumentException(
                "Entity ID does not exist.",
                nameof(entity));
        }
    }
}