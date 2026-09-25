using Engine.Core.Determinism;
using Engine.ECS.Commands;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.ECS.Inspection;
using Engine.ECS.Jobs;
using Engine.ECS.Persistence;
using Engine.ECS.Queries;
using Engine.Jobs.Jobs;
using Engine.Jobs.Scheduling;

namespace Engine.ECS;

public sealed class World :
    IDeterministicState,
    IDisposable
{
    private readonly List<(
    JobScheduler Scheduler,
    JobHandle Handle)> _activeJobs = new();

    private readonly object _jobSync = new();

    private readonly EntityStore _entities =
        new();

    private readonly Dictionary<
        Type,
        IComponentStorage> _componentStorages =
        new();

    private readonly List<IComponentStorage>
        _deterministicStorages =
        new();

    private readonly CommandBuffer _commandBuffer;

    private bool _disposed;

    public IWorldInspector Inspector { get; }

    public World()
    {
        _commandBuffer =
            new CommandBuffer(this);

        Inspector =
            new WorldInspector(this);
    }

    public int EntityCount
    {
        get
        {
            EnsureNotDisposed();

            return _entities.Count;
        }
    }

    public CommandBuffer Commands
    {
        get
        {
            EnsureNotDisposed();

            return _commandBuffer;
        }
    }

    public event Action<EntityId>? EntityDestroyed;

    public EntityId CreateEntity()
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs();

        return _entities.Create();
    }

    public bool Exists(
        EntityId entity)
    {
        EnsureNotDisposed();

        return _entities.Exists(
            entity);
    }

    public EcsWorldState CaptureState()
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs();

        var components =
            _componentStorages.Values
                .OrderBy(
                    storage =>
                        storage.ComponentType.FullName
                        ?? storage.ComponentType.Name,
                    StringComparer.Ordinal)
                .Select(
                    storage =>
                        storage.CaptureState())
                .ToArray();

        return new EcsWorldState(
            _entities.CaptureState(),
            components);
    }

    public void RestoreState(
        EcsWorldState state)
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs();

        ArgumentNullException.ThrowIfNull(
            state);

        _entities.RestoreState(
            state.Entities);

        foreach (var storage in
                 _componentStorages.Values)
        {
            storage.Dispose();
        }

        _componentStorages.Clear();
        _deterministicStorages.Clear();

        var componentTypes =
            new HashSet<Type>();

        foreach (var componentState in
                 state.Components)
        {
            if (!componentTypes.Add(
                    componentState.ComponentType))
            {
                throw new InvalidOperationException(
                    $"Component type '{componentState.ComponentType.Name}' " +
                    "appears more than once in the world state.");
            }

            componentState.Restore(
                this);
        }

        RebuildDeterministicStorages();

        _commandBuffer.Clear();
    }

    internal void RestoreComponentState<T>(
        WorldComponentState<T> state)
        where T : struct
    {
        EnsureNotDisposed();

        if (state.Entities.Length !=
            state.Components.Length)
        {
            throw new InvalidOperationException(
                $"Component state for '{typeof(T).Name}' " +
                "contains mismatched entity and component counts.");
        }

        var storage =
            GetOrCreateStorage<T>();

        storage.RestorePersistenceState(
            state);
    }
    public bool DestroyEntity(
        EntityId entity)
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs();

        if (!_entities.Destroy(
                entity))
        {
            return false;
        }

        foreach (var storage in
                 _componentStorages.Values)
        {
            storage.RemoveEntity(
                entity);
        }

        EntityDestroyed?.Invoke(
            entity);

        return true;
    }

    public void Add<T>(
        EntityId entity,
        T component)
        where T : struct
    {
        EnsureNotDisposed();
        EnsureEntityExists(entity);
        EnsureNoActiveJobs();
        GetOrCreateStorage<T>()
            .Add(
                entity,
                component);
    }

    public bool Has<T>(
        EntityId entity)
        where T : struct
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs();
        if (!_entities.Exists(
                entity))
        {
            return false;
        }

        return TryGetStorage<T>(
                   out var storage) &&
               storage.Has(entity);
    }

    public ref T Get<T>(
        EntityId entity)
        where T : struct
    {
        EnsureNotDisposed();
        EnsureEntityExists(entity);
        EnsureNoActiveJobs();
        if (!TryGetStorage<T>(
                out var storage))
        {
            throw new KeyNotFoundException(
                $"Component storage " +
                $"{typeof(T).Name} does not exist.");
        }

        return ref storage.Get(
            entity);
    }

    public bool Remove<T>(
        EntityId entity)
        where T : struct
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs();
        if (!_entities.Exists(
                entity))
        {
            return false;
        }

        return TryGetStorage<T>(
                   out var storage) &&
               storage.Remove(
                   entity);
    }

    public Query<T> Query<T>()
        where T : struct
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs() ;
        TryGetStorage<T>(
            out var storage);

        return new Query<T>(
            storage);
    }

    public Query<T1, T2> Query<T1, T2>()
        where T1 : struct
        where T2 : struct
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs();
        TryGetStorage<T1>(
            out var first);

        TryGetStorage<T2>(
            out var second);

        return new Query<T1, T2>(
            first,
            second);
    }

    public ReadOnlySpan<EntityId> GetDirtyEntities<T>()
    where T : struct
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs();

        return TryGetStorage<T>(
                   out var storage)
            ? storage.GetDirtyEntities()
            : ReadOnlySpan<EntityId>.Empty;
    }

    public ulong GetChangeVersion<T>()
    where T : struct
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs();

        return TryGetStorage<T>(
                   out var storage)
            ? storage.ChangeVersion
            : 0;
    }

    public ReadOnlySpan<EntityId> GetRemovedEntities<T>()
        where T : struct
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs();

        return TryGetStorage<T>(
                   out var storage)
            ? storage.GetRemovedEntities()
            : ReadOnlySpan<EntityId>.Empty;
    }

    public void ClearDirty<T>()
        where T : struct
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs();

        if (TryGetStorage<T>(
                out var storage))
        {
            storage.ClearDirty();
        }
    }

    public JobHandle ScheduleParallel<T>(
    JobScheduler scheduler,
    ComponentJob<T> job,
    int batchSize = 64,
    params JobHandle[] dependencies)
    where T : struct
    {
        EnsureNotDisposed();

        ArgumentNullException.ThrowIfNull(
            scheduler);

        ArgumentNullException.ThrowIfNull(
            job);

        ArgumentNullException.ThrowIfNull(
            dependencies);

        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batchSize));
        }

        if (!TryGetStorage<T>(
                out var storage))
        {
            return scheduler.CombineDependencies(
                dependencies);
        }

        var parallelJob =
            new ParallelComponentJob<T>(
                storage,
                job);

        var handle =
            scheduler.ParallelFor(
                parallelJob,
                storage.Count,
                batchSize,
                dependencies);

        TrackJob(
            scheduler,
            handle);

        JobScope.Current?.Track(
            handle);

        return handle;
    }

    public JobHandle ScheduleParallel<T1, T2>(
        JobScheduler scheduler,
        ComponentPairJob<T1, T2> job,
        int batchSize = 64,
        params JobHandle[] dependencies)
        where T1 : struct
        where T2 : struct
    {
        EnsureNotDisposed();

        ArgumentNullException.ThrowIfNull(
            scheduler);

        ArgumentNullException.ThrowIfNull(
            job);

        ArgumentNullException.ThrowIfNull(
            dependencies);

        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batchSize));
        }

        if (!TryGetStorage<T1>(
                out var first) ||
            !TryGetStorage<T2>(
                out var second))
        {
            return scheduler.CombineDependencies(
                dependencies);
        }

        var parallelJob =
            new ParallelComponentPairJob<T1, T2>(
                first,
                second,
                job);

        var handle =
            scheduler.ParallelFor(
                parallelJob,
                parallelJob.Count,
                batchSize,
                dependencies);

        TrackJob(
            scheduler,
            handle);

        JobScope.Current?.Track(
            handle);

        return handle;
    }

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        EnsureNotDisposed();

        _entities.AddToHash(
            ref hasher);

        hasher.AddInt32(
            _deterministicStorages.Count);

        foreach (var storage in
                 _deterministicStorages)
        {
            hasher.AddString(
                storage.ComponentType.FullName
                ?? storage.ComponentType.Name);

            storage.AddToHash(
                ref hasher);
        }
    }

    public void ApplyCommands()
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs();
        _commandBuffer.Apply(
            this);
    }

    internal EntityId ReserveEntity()
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs();
        return _entities.Reserve();
    }

    internal void ActivateEntity(
        EntityId entity)
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs();
        if (!_entities.Activate(
                entity))
        {
            throw new InvalidOperationException(
                $"Entity {entity.Index} is already active.");
        }
    }

    public WorldSnapshot CreateSnapshot()
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs();
        if (_commandBuffer.Count != 0)
        {
            throw new InvalidOperationException(
                "World snapshot can only be created " +
                "when the command buffer is empty.");
        }

        var entities =
            _entities.CreateSnapshot();

        var componentStorages =
            _componentStorages
                .Values
                .OrderBy(
                    storage =>
                        storage.ComponentType.FullName
                        ?? storage.ComponentType.Name,
                    StringComparer.Ordinal)
                .Select(
                    storage =>
                        storage.CreateSnapshot())
                .ToList();

        return new WorldSnapshot(
            this,
            entities,
            componentStorages);
    }

    public void RestoreSnapshot(
        WorldSnapshot snapshot)
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs() ;  
        ArgumentNullException.ThrowIfNull(
            snapshot);

        if (!snapshot.BelongsTo(
                this))
        {
            throw new InvalidOperationException(
                "World snapshot belongs to another World.");
        }

        _entities.RestoreSnapshot(
            snapshot.Entities);

        var snapshotStorages =
            snapshot.ComponentStorages
                .ToDictionary(
                    storage =>
                        storage.ComponentType);

        var currentTypes =
            _componentStorages.Keys.ToList();

        foreach (var type in currentTypes)
        {
            if (snapshotStorages.ContainsKey(type))
            {
                continue;
            }

            if (_componentStorages.Remove(
                    type,
                    out var storage))
            {
                storage.Dispose();
            }
        }

        foreach (var snapshotStorage
                 in snapshot.ComponentStorages)
        {
            if (!_componentStorages.TryGetValue(
                    snapshotStorage.ComponentType,
                    out var storage))
            {
                throw new InvalidOperationException(
                    $"Component storage " +
                    $"'{snapshotStorage.ComponentType.Name}' " +
                    "does not exist in the current World.");
            }

            storage.RestoreSnapshot(
                snapshotStorage);
        }

        RebuildDeterministicStorages();

        _commandBuffer.Clear();
    }

    internal IReadOnlyList<EntityId>
        GetEntitiesForInspection()
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs();
        return _entities.GetActiveEntities();
    }

    internal IReadOnlyList<Type>
        GetComponentTypesForInspection(
            EntityId entity)
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs();
        if (!_entities.Exists(
                entity))
        {
            throw new InvalidOperationException(
                $"Entity {entity.Index} does not exist.");
        }

        var types =
            new List<Type>();

        foreach (var storage in
                 _componentStorages.Values)
        {
            if (storage.Has(
                    entity))
            {
                types.Add(
                    storage.ComponentType);
            }
        }

        types.Sort(
            static (left, right) =>
                StringComparer.Ordinal.Compare(
                    left.FullName ?? left.Name,
                    right.FullName ?? right.Name));

        return types;
    }

    internal bool TryGetComponentForInspection(
        EntityId entity,
        Type componentType,
        out object? component)
    {
        EnsureNotDisposed();
        EnsureNoActiveJobs();
        if (!_entities.Exists(
                entity))
        {
            component = null;
            return false;
        }

        if (!_componentStorages.TryGetValue(
                componentType,
                out var storage))
        {
            component = null;
            return false;
        }

        if (!storage.Has(
                entity))
        {
            component = null;
            return false;
        }

        component =
            storage.GetBoxed(
                entity);

        return true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        EnsureNoActiveJobs();
        _commandBuffer.Clear();

       
        foreach (var storage in
                 _componentStorages.Values)
        {
            storage.Dispose();
        }

        _componentStorages.Clear();
        _deterministicStorages.Clear();

        _entities.Dispose();

        _disposed =
            true;
    }

    private ComponentStorage<T>
        GetOrCreateStorage<T>()
        where T : struct
    {
        var type =
            typeof(T);

        if (_componentStorages.TryGetValue(
                type,
                out var existing))
        {
            return (ComponentStorage<T>)existing;
        }

        var storage =
            new ComponentStorage<T>();

        _componentStorages.Add(
            type,
            storage);

        if (storage.IsDeterministic)
        {
            RebuildDeterministicStorages();
        }

        return storage;
    }

    private bool TryGetStorage<T>(
        out ComponentStorage<T>? storage)
        where T : struct
    {
        if (_componentStorages.TryGetValue(
                typeof(T),
                out var existing))
        {
            storage =
                (ComponentStorage<T>)existing;

            return true;
        }

        storage = null;

        return false;
    }

    private void EnsureEntityExists(
        EntityId entity)
    {
        if (!_entities.Exists(
                entity))
        {
            throw new InvalidOperationException(
                $"Entity {entity.Index} does not exist.");
        }
    }

    private void RebuildDeterministicStorages()
    {
        _deterministicStorages.Clear();

        foreach (var storage in
                 _componentStorages.Values)
        {
            if (storage.IsDeterministic)
            {
                _deterministicStorages.Add(
                    storage);
            }
        }

        _deterministicStorages.Sort(
            static (left, right) =>
                StringComparer.Ordinal.Compare(
                    left.ComponentType.FullName,
                    right.ComponentType.FullName));
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }

    private void TrackJob(
    JobScheduler scheduler,
    JobHandle handle)
    {
        lock (_jobSync)
        {
            _activeJobs.Add(
                (
                    scheduler,
                    handle));
        }
    }

    private void EnsureNoActiveJobs()
    {
        lock (_jobSync)
        {
            for (var i = _activeJobs.Count - 1;
                 i >= 0;
                 i--)
            {
                var active =
                    _activeJobs[i];

                try
                {
                    if (!active.Scheduler.IsCompleted(
                            active.Handle))
                    {
                        continue;
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Disposing JobScheduler waits
                    // for all outstanding jobs.
                }

                _activeJobs.RemoveAt(i);
            }

            if (_activeJobs.Count != 0)
            {
                throw new InvalidOperationException(
                    "World cannot be accessed while parallel jobs are running.");
            }
        }
    }
}