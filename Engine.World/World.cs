using Engine.Core.Determinism;
using Engine.Worlds.Chunks;
using Engine.Worlds.Persistence;
using Engine.Worlds.Spatial;
using Engine.Worlds.Tiles;

namespace Engine.Worlds;

public sealed class World :
    IDeterministicState,IDisposable
{
    private bool _disposed;
    private readonly ChunkRegistry _chunks =
        new();
    private readonly IChunkPersistence _chunkPersistence;
    public IChunkPersistence ChunkPersistence =>
    _chunkPersistence;

    private readonly Engine.ECS.World _ecsWorld;
    public Engine.ECS.World EcsWorld =>
    _ecsWorld;

    internal void RestoreChunk(
       Persistence.ChunkSaveState state)
    {
        EnsureNotDisposed();

        ArgumentNullException.ThrowIfNull(
            state);

        if (state.Residency ==
            ChunkResidencyState.Unloaded)
        {
            _chunks.RegisterUnloaded(
                state.Position);

            try
            {
                _chunkPersistence.Save(
                    state);
            }
            catch
            {
                _chunks.Remove(
                    state.Position);

                throw;
            }

            return;
        }

        var expectedTileCount =
            checked(
                ChunkSize.Width *
                ChunkSize.Height);

        if (state.Tiles.Length !=
            expectedTileCount)
        {
            throw new InvalidDataException(
                $"Chunk '{state.Position}' contains " +
                $"'{state.Tiles.Length}' tiles, expected " +
                $"'{expectedTileCount}'.");
        }

        var record =
    _chunks.RegisterUnloaded(
        state.Position);

        record.Lifecycle.BeginLoading();

        var chunk =
            new Chunk(
                state.Position,
                ChunkSize);

        state.Tiles
            .AsSpan()
            .CopyTo(
                chunk.Tiles.AsSpan());

        var attached =
            false;

        try
        {
            record.Attach(
                chunk);

            attached =
                true;

            record.Lifecycle.SetSimulation(
                state.Simulation);

            record.Lifecycle.SetPresentation(
                state.Presentation);
        }
        catch
        {
            if (attached)
            {
                record.Lifecycle.SetSimulation(
                    ChunkSimulationState.Suspended);

                record.Lifecycle.BeginUnloading();

                record.Detach();
            }
            else
            {
                chunk.Dispose();

                _chunks.Remove(
                    state.Position);
            }

            throw;
        }
    }
    public IEnumerable<Chunk> GetChunks() =>
        _chunks.LoadedChunks;
    public IEnumerable<ChunkRecord> GetChunkRecords()
    {
        return _chunks.Records;
    }
    public SpatialIndex SpatialIndex { get; }

    public SpatialEntityManager SpatialEntities { get; }

    public ChunkRegistry Chunks =>
        _chunks;

    public World(
        ChunkSize chunkSize,
        Engine.ECS.World ecsWorld,
        IChunkPersistence? chunkPersistence = null)
    {
        ArgumentNullException.ThrowIfNull(
            ecsWorld);

        ChunkSize =
            chunkSize;

        _ecsWorld =
            ecsWorld;
        _chunkPersistence =
    chunkPersistence ??
    new MemoryChunkPersistence();

        SpatialIndex =
            new SpatialIndex();

        SpatialEntities =
            new SpatialEntityManager(
                this,
                _ecsWorld);
    }

    public ChunkSize ChunkSize { get; }

    public int ChunkCount =>
        _chunks.LoadedCount;

    public Chunk CreateChunk(
        ChunkPosition position)
    {
        EnsureNotDisposed();
        if (_chunks.TryGet(
                position,
                out _))
        {
            throw new InvalidOperationException(
                $"Chunk '{position}' already exists.");
        }

        var chunk =
            new Chunk(
                position,
                ChunkSize);

        _chunks.Register(
            position,
            chunk);

        return chunk;
    }

    public bool TryGetChunk(
        ChunkPosition position,
        out Chunk? chunk)
    {
        return _chunks.TryGetLoaded(
            position,
            out chunk);
    }

    public Chunk GetOrCreateChunk(
        ChunkPosition position)
    {
        if (_chunks.TryGetLoaded(
                position,
                out var existing))
        {
            return existing;
        }

        if (_chunks.TryGet(
                position,
                out _))
        {
            throw new InvalidOperationException(
                $"Chunk '{position}' is registered but is not loaded.");
        }

        return CreateChunk(
            position);
    }

    public bool RemoveChunk(
        ChunkPosition position)
    {
        EnsureNotDisposed();
        if (!_chunks.TryGet(
                position,
                out var record))
        {
            return false;
        }

        if (record.Chunk is not null)
        {
            if (!record
                    .Lifecycle
                    .Simulation
                    .Equals(
                        ChunkSimulationState.Suspended))
            {
                throw new InvalidOperationException(
                    $"Chunk '{position}' cannot be removed while simulation is active.");
            }

            if (!SpatialIndex
                    .GetEntities(position)
                    .IsEmpty)
            {
                throw new InvalidOperationException(
                    $"Chunk '{position}' cannot be removed while it contains indexed entities.");
            }

            record.Lifecycle.BeginUnloading();

            record.Detach();
        }

        var removed =
            _chunks.Remove(
                position);

        if (removed)
        {
            _chunkPersistence.Remove(
                position);
        }

        return removed;
    }

    public bool UnloadChunk(
        ChunkPosition position)
    {
        EnsureNotDisposed();
        if (!_chunks.TryGet(
                position,
                out var record))
        {
            return false;
        }

        if (record.Chunk is null)
        {
            return false;
        }

        if (!SpatialIndex
                .GetEntities(position)
                .IsEmpty)
        {
            throw new InvalidOperationException(
                $"Chunk '{position}' cannot be unloaded while it contains indexed entities.");
        }

        var state =
            Persistence.WorldPersistence.CaptureChunk(
                this,
                record);

        if (record.Lifecycle.Simulation !=
            ChunkSimulationState.Suspended)
        {
            record.Lifecycle.SetSimulation(
                ChunkSimulationState.Suspended);
        }

        var simulation =
            record.Lifecycle.Simulation;

        var presentation =
            record.Lifecycle.Presentation;

        record.Lifecycle.BeginUnloading();

        record.Detach();

        _chunkPersistence.Save(
            new ChunkSaveState(
                state.Position,
                ChunkResidencyState.Unloaded,
                simulation,
                presentation,
                state.Tiles));

        return true;
    }

    public ChunkPosition GetChunkPosition(
        WorldPosition position)
    {
        return new ChunkPosition(
            FloorDivide(
                position.X,
                ChunkSize.Width),
            FloorDivide(
                position.Y,
                ChunkSize.Height));
    }

    public LocalPosition GetLocalPosition(
        WorldPosition position)
    {
        return new LocalPosition(
            FloorModulo(
                position.X,
                ChunkSize.Width),
            FloorModulo(
                position.Y,
                ChunkSize.Height));
    }

    public WorldPosition ToWorldPosition(
        ChunkPosition chunkPosition,
        LocalPosition localPosition)
    {
        ValidateLocalPosition(
            localPosition);

        return new WorldPosition(
            checked(
                chunkPosition.X *
                ChunkSize.Width +
                localPosition.X),

            checked(
                chunkPosition.Y *
                ChunkSize.Height +
                localPosition.Y));
    }

    public SpatialQuery<T> Query<T>(
        ChunkPosition chunk)
        where T : struct
    {
        return new SpatialQuery<T>(
            _ecsWorld,
            SpatialIndex.GetEntities(
                chunk));
    }

    public bool TryGetNeighbor(
        ChunkPosition position,
        int offsetX,
        int offsetY,
        out Chunk? neighbor)
    {
        var neighborPosition =
            new ChunkPosition(
                checked(position.X + offsetX),
                checked(position.Y + offsetY));

        return TryGetChunk(
            neighborPosition,
            out neighbor);
    }

    private void ValidateLocalPosition(
        LocalPosition position)
    {
        if (position.X < 0 ||
            position.X >= ChunkSize.Width)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position));
        }

        if (position.Y < 0 ||
            position.Y >= ChunkSize.Height)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position));
        }
    }

    private static int FloorDivide(
        int value,
        int divisor)
    {
        var quotient =
            value / divisor;

        var remainder =
            value % divisor;

        if (remainder != 0 &&
            value < 0)
        {
            quotient--;
        }

        return quotient;
    }

    private static int FloorModulo(
        int value,
        int divisor)
    {
        var remainder =
            value % divisor;

        if (remainder < 0)
        {
            remainder += divisor;
        }

        return remainder;
    }

    public void AddToHash(
    ref DeterministicStateHasher hasher)
    {
        EnsureNotDisposed();
        hasher.AddInt32(ChunkSize.Width);
        hasher.AddInt32(ChunkSize.Height);

        // Порядок словаря не должен влиять на hash.
        var records =
            _chunks.Records
                .OrderBy(
                    record =>
                        record.Lifecycle.Position.X)
                .ThenBy(
                    record =>
                        record.Lifecycle.Position.Y)
                .ToArray();

        hasher.AddInt32(
            records.Length);

        foreach (var record in records)
        {
            var lifecycle =
                record.Lifecycle;

            var position =
                lifecycle.Position;

            if (lifecycle.Residency !=
                    ChunkResidencyState.Loaded &&
                lifecycle.Residency !=
                    ChunkResidencyState.Unloaded)
            {
                throw new InvalidOperationException(
                    $"Chunk '{position}' cannot be hashed " +
                    $"while its residency is '{lifecycle.Residency}'.");
            }

            hasher.AddInt32(position.X);
            hasher.AddInt32(position.Y);

            // Симуляция влияет на детерминированное состояние.
            // Presentation и Residency намеренно не хешируем.
            hasher.AddInt32(
                (int)lifecycle.Simulation);

            if (lifecycle.Residency ==
                ChunkResidencyState.Loaded)
            {
                if (record.Chunk is null)
                {
                    throw new InvalidOperationException(
                        $"Loaded chunk '{position}' has no runtime data.");
                }

                AddTilesToHash(
                    ref hasher,
                    record.Chunk.Tiles.AsReadOnlySpan());
            }
            else
            {
                if (record.Chunk is not null)
                {
                    throw new InvalidOperationException(
                        $"Unloaded chunk '{position}' still has runtime data.");
                }

                if (!_chunkPersistence.TryLoad(
                        position,
                        out var persisted) ||
                    persisted is null)
                {
                    throw new InvalidOperationException(
                        $"Unloaded chunk '{position}' has no persisted state.");
                }

                if (persisted.Position != position)
                {
                    throw new InvalidOperationException(
                        $"Persisted chunk position does not match '{position}'.");
                }

                AddTilesToHash(
                    ref hasher,
                    persisted.Tiles);
            }
        }
    }

    private void AddTilesToHash(
        ref DeterministicStateHasher hasher,
        ReadOnlySpan<Tile> tiles)
    {
        var expectedTileCount =
            checked(
                ChunkSize.Width *
                ChunkSize.Height);

        if (tiles.Length != expectedTileCount)
        {
            throw new InvalidOperationException(
                $"Chunk contains '{tiles.Length}' tiles, " +
                $"expected '{expectedTileCount}'.");
        }

        hasher.AddInt32(
            tiles.Length);

        foreach (var tile in tiles)
        {
            hasher.AddUInt32(
                tile.Value);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Сначала удаляем пространственные ссылки,
        // иначе снятие чанков может быть запрещено.
        SpatialIndex.Clear();

        foreach (var record in
                 _chunks.Records.ToArray())
        {
            var lifecycle =
                record.Lifecycle;

            var position =
                lifecycle.Position;

            switch (lifecycle.Residency)
            {
                case ChunkResidencyState.Loaded:
                    if (record.Chunk is null)
                    {
                        throw new InvalidOperationException(
                            $"Loaded chunk '{position}' has no runtime data.");
                    }

                    if (lifecycle.Simulation !=
                        ChunkSimulationState.Suspended)
                    {
                        lifecycle.SetSimulation(
                            ChunkSimulationState.Suspended);
                    }

                    lifecycle.BeginUnloading();
                    record.Detach();
                    break;

                case ChunkResidencyState.Unloading:
                    if (record.Chunk is not null)
                    {
                        record.Detach();
                    }
                    else
                    {
                        lifecycle.CompleteUnloading();
                    }

                    break;

                case ChunkResidencyState.Loading:
                    if (record.Chunk is not null)
                    {
                        throw new InvalidOperationException(
                            $"Loading chunk '{position}' unexpectedly has runtime data.");
                    }

                    lifecycle.CancelLoading();
                    break;

                case ChunkResidencyState.Unloaded:
                    if (record.Chunk is not null)
                    {
                        throw new InvalidOperationException(
                            $"Unloaded chunk '{position}' unexpectedly has runtime data.");
                    }

                    break;

                default:
                    throw new InvalidOperationException(
                        $"Chunk '{position}' has unsupported residency state.");
            }

            // Удаляем запись из реестра напрямую.
            // Не вызываем World.RemoveChunk(): он удаляет данные
            // из IChunkPersistence, которые могут пережить World.
            _chunks.Remove(position);
        }

        _disposed = true;
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}