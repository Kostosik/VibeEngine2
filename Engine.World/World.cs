using Engine.ECS;
using Engine.Worlds.Chunks;
using Engine.Worlds.Persistence;
using Engine.Worlds.Spatial;

namespace Engine.Worlds;

public sealed class World
{
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
        ArgumentNullException.ThrowIfNull(
            state);

        if (state.Residency ==
            ChunkResidencyState.Unloaded)
        {
            _chunks.RegisterUnloaded(
                state.Position);

            _chunkPersistence.Save(
                state);

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

        try
        {
            record.Attach(
                chunk);

            record.Lifecycle.SetSimulation(
                state.Simulation);

            record.Lifecycle.SetPresentation(
                state.Presentation);
        }
        catch
        {
            chunk.Dispose();

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

        _chunkPersistence.Save(
            state);

        if (record.Lifecycle.Simulation !=
            ChunkSimulationState.Suspended)
        {
            record.Lifecycle.SetSimulation(
                ChunkSimulationState.Suspended);
        }

        record.Lifecycle.BeginUnloading();

        record.Detach();

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
            chunkPosition.X * ChunkSize.Width +
            localPosition.X,
            chunkPosition.Y * ChunkSize.Height +
            localPosition.Y);
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
                position.X + offsetX,
                position.Y + offsetY);

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
}