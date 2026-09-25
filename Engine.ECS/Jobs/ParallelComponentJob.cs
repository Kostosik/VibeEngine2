using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.ECS.Queries;
using Engine.Jobs.Jobs;

namespace Engine.ECS.Jobs;

internal sealed class ParallelComponentJob<T> :
    IJobParallelFor
    where T : struct
{
    private readonly ComponentStorage<T> _storage;

    private readonly ComponentJob<T> _job;

    public ParallelComponentJob(
        ComponentStorage<T> storage,
        ComponentJob<T> job)
    {
        ArgumentNullException.ThrowIfNull(
            storage);

        ArgumentNullException.ThrowIfNull(
            job);

        _storage =
            storage;

        _job =
            job;
    }

    public void Execute(
        int index)
    {
        var entity =
            _storage.GetEntity(
                index);

        ref var component =
            ref _storage.GetByIndex(
                index);

        _job(
            entity,
            ref component);
    }
}