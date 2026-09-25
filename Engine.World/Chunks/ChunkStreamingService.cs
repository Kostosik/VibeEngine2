using Engine.Jobs.Jobs;
using Engine.Jobs.Scheduling;
using Engine.Worlds.Spatial;

namespace Engine.Worlds.Chunks;

public sealed class ChunkStreamingService
{
    private readonly World _world;

    private readonly ChunkStreamingPolicy _policy;

    private readonly IChunkLoader _loader;

    private readonly JobScheduler _scheduler;

    private readonly Dictionary<
        ChunkPosition,
        PendingLoad> _pendingLoads =
        new();

    public ChunkStreamingService(
        World world,
        ChunkStreamingPolicy policy,
        IChunkLoader loader,
        JobScheduler scheduler)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        ArgumentNullException.ThrowIfNull(
            policy);

        ArgumentNullException.ThrowIfNull(
            loader);

        ArgumentNullException.ThrowIfNull(
            scheduler);

        _world =
            world;

        _policy =
            policy;

        _loader =
            loader;

        _scheduler =
            scheduler;
    }

    public int PendingLoadCount =>
        _pendingLoads.Count;

    public ChunkStreamingRequest Update(
        ChunkStreamingInterest interest)
    {
        CompleteFinishedLoads();

        var request =
            _policy.Evaluate(
                interest);

        foreach (var position
                 in request.ResidentChunks)
        {
            EnsureLoading(
                position);
        }

        ApplyPresentation(
            request);

        return request;
    }

    public void FlushLoads()
    {
        foreach (var pending in
                 _pendingLoads.Values.ToArray())
        {
            _scheduler.Wait(
                pending.Handle);
        }

        CompleteFinishedLoads();
    }

    private void EnsureLoading(
        ChunkPosition position)
    {
        if (_pendingLoads.ContainsKey(
                position))
        {
            return;
        }

        var recordCreated =
            false;

        if (!_world.Chunks.TryGet(
                position,
                out var record))
        {
            record =
                _world.Chunks.RegisterUnloaded(
                    position);

            recordCreated =
                true;
        }

        if (record is null)
        {
            throw new InvalidOperationException(
                $"Chunk record for '{position}' is null.");
        }

        var residency =
            record.Lifecycle.Residency;

        if (residency ==
            ChunkResidencyState.Loaded)
        {
            return;
        }

        if (residency !=
            ChunkResidencyState.Unloaded)
        {
            throw new InvalidOperationException(
                $"Chunk '{position}' is in residency state '{residency}' " +
                "and cannot start loading.");
        }

        record.Lifecycle.BeginLoading();

        var result =
            new ChunkLoadResult();

        var job =
            new ChunkLoadJob(
                _loader,
                position,
                _world.ChunkSize,
                result);

        var handle =
            _scheduler.Schedule(
                job);

        _pendingLoads.Add(
            position,
            new PendingLoad(
                record,
                recordCreated,
                result,
                handle));
    }

    private void CompleteFinishedLoads()
    {
        foreach (var pair
                 in _pendingLoads.ToArray())
        {
            var position =
                pair.Key;

            var pending =
                pair.Value;

            if (!_scheduler.IsCompleted(
                    pending.Handle))
            {
                continue;
            }

            _pendingLoads.Remove(
                position);

            try
            {
                _scheduler.Wait(
                    pending.Handle);

                if (pending.Result.Data is null)
                {
                    throw new InvalidOperationException(
                        $"Chunk loader returned no data for '{position}'.");
                }

                var chunk =
                    new Chunk(
                        position,
                        pending.Result.Data);

                pending.Record.Attach(
                    chunk);

                pending.Result.Data =
                    null;
            }
            catch
            {
                pending.Record.Lifecycle.CancelLoading();

                if (pending.RecordCreated)
                {
                    _world.Chunks.Remove(
                        position);
                }

                pending.Result.Data?.Dispose();

                throw;
            }
        }
    }

    private void ApplyPresentation(
        ChunkStreamingRequest request)
    {
        var presentation =
            new HashSet<ChunkPosition>(
                request.PresentationChunks);

        foreach (var record
                 in _world.Chunks.Records)
        {
            var chunk =
                record.Chunk;

            if (chunk is null ||
                record.Lifecycle.Residency !=
                    ChunkResidencyState.Loaded)
            {
                continue;
            }

            record.Lifecycle.SetPresentation(
                presentation.Contains(
                    chunk.Position)
                    ? ChunkPresentationState.Relevant
                    : ChunkPresentationState.Irrelevant);
        }
    }

    private sealed class PendingLoad
    {
        public PendingLoad(
            ChunkRecord record,
            bool recordCreated,
            ChunkLoadResult result,
            JobHandle handle)
        {
            Record =
                record;

            RecordCreated =
                recordCreated;

            Result =
                result;

            Handle =
                handle;
        }

        public ChunkRecord Record { get; }

        public bool RecordCreated { get; }

        public ChunkLoadResult Result { get; }

        public JobHandle Handle { get; }
    }
}