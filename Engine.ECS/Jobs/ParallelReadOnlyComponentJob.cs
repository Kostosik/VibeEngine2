using Engine.ECS.Components;
using Engine.ECS.Queries;
using Engine.Jobs.Jobs;

namespace Engine.ECS.Jobs;

internal sealed class ParallelReadOnlyComponentJob<T> :
    IJobParallelFor
    where T : struct
{
    private readonly ComponentStorage<T> _storage;

    private readonly ReadOnlyComponentJob<T> _job;

    public ParallelReadOnlyComponentJob(
        ComponentStorage<T> storage,
        ReadOnlyComponentJob<T> job)
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

        ref readonly var component =
            ref _storage.GetByIndexReadOnly(
                index);

        _job(
            entity,
            in component);
    }
}