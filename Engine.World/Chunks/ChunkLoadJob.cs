using Engine.Jobs.Jobs;
using Engine.Worlds.Persistence;
using Engine.Worlds.Spatial;
using Engine.Worlds.Tiles;

namespace Engine.Worlds.Chunks;

internal sealed class ChunkLoadJob :
    IJob
{
    private readonly IChunkLoader _loader;

    private readonly ChunkPosition _position;

    private readonly ChunkSize _size;

    private readonly ChunkLoadResult _result;
    private readonly IChunkPersistence _persistence;
    public ChunkLoadJob(
        IChunkLoader loader,
        IChunkPersistence persistence,
        ChunkPosition position,
        ChunkSize size,
        ChunkLoadResult result)
    {
        ArgumentNullException.ThrowIfNull(
            loader);

        ArgumentNullException.ThrowIfNull(
            persistence);

        ArgumentNullException.ThrowIfNull(
            result);

        _loader =
            loader;

        _persistence =
            persistence;

        _position =
            position;

        _size =
            size;

        _result =
            result;
    }

    public void Execute()
    {
        if (_persistence.TryLoad(
                _position,
                out var persisted) &&
            persisted is not null)
        {
            var expectedTileCount =
                checked(
                    _size.Width *
                    _size.Height);

            if (persisted.Tiles.Length !=
                expectedTileCount)
            {
                throw new InvalidDataException(
                    $"Persisted chunk '{_position}' contains " +
                    $"'{persisted.Tiles.Length}' tiles, expected " +
                    $"'{expectedTileCount}'.");
            }

            var data =
                new ChunkData(
                    new TileStorage(
                        _size));

            for (var i = 0;
                 i < persisted.Tiles.Length;
                 i++)
            {
                data.Tiles.AsSpan()[i] =
                    persisted.Tiles[i];
            }

            _result.Data =
                data;

            _result.Simulation =
                persisted.Simulation;

            _result.Presentation =
                persisted.Presentation;

            return;
        }

        _result.Data =
            _loader.Load(
                _position,
                _size);
    }
}

internal sealed class ChunkLoadResult
{
    public ChunkData? Data { get; set; }

    public ChunkSimulationState Simulation { get; set; } =
        ChunkSimulationState.Simulating;

    public ChunkPresentationState Presentation { get; set; } =
        ChunkPresentationState.Irrelevant;
}