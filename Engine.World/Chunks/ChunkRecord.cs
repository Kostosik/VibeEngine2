using Engine.Worlds.Spatial;

namespace Engine.Worlds.Chunks;

public sealed class ChunkRecord
{
    private Chunk? _chunk;

    internal ChunkRecord(
        ChunkLifecycle lifecycle)
    {
        ArgumentNullException.ThrowIfNull(
            lifecycle);

        Lifecycle =
            lifecycle;
    }

    public ChunkLifecycle Lifecycle { get; }

    public Chunk? Chunk =>
        _chunk;

    internal void Attach(
        Chunk chunk)
    {
        ArgumentNullException.ThrowIfNull(
            chunk);

        if (chunk.Position !=
            Lifecycle.Position)
        {
            throw new ArgumentException(
                "Chunk position does not match the lifecycle position.",
                nameof(chunk));
        }

        if (_chunk is not null)
        {
            throw new InvalidOperationException(
                $"Chunk '{Lifecycle.Position}' is already attached.");
        }

        if (Lifecycle.Residency !=
            ChunkResidencyState.Loading)
        {
            throw new InvalidOperationException(
                $"Chunk '{Lifecycle.Position}' must be loading before it can be attached.");
        }

        _chunk =
            chunk;

        Lifecycle.CompleteLoading();
    }

    internal Chunk Detach()
    {
        if (_chunk is null)
        {
            throw new InvalidOperationException(
                $"Chunk '{Lifecycle.Position}' is not attached.");
        }

        if (Lifecycle.Residency !=
            ChunkResidencyState.Unloading)
        {
            throw new InvalidOperationException(
                $"Chunk '{Lifecycle.Position}' must be unloading before it can be detached.");
        }

        var chunk =
            _chunk;

        _chunk = null;

        chunk.Dispose();

        Lifecycle.CompleteUnloading();

        return chunk;
    }
}