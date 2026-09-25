using Engine.ECS.Components;
using Engine.ECS.Queries;
using Engine.Jobs.Jobs;

namespace Engine.ECS.Jobs;

internal sealed class ParallelComponentPairJob<T1, T2> :
    IJobParallelFor
    where T1 : struct
    where T2 : struct
{
    private readonly ComponentStorage<T1> _first;

    private readonly ComponentStorage<T2> _second;

    private readonly ComponentPairJob<T1, T2> _job;

    private readonly bool _iterateFirst;

    public ParallelComponentPairJob(
        ComponentStorage<T1> first,
        ComponentStorage<T2> second,
        ComponentPairJob<T1, T2> job)
    {
        ArgumentNullException.ThrowIfNull(
            first);

        ArgumentNullException.ThrowIfNull(
            second);

        ArgumentNullException.ThrowIfNull(
            job);

        _first =
            first;

        _second =
            second;

        _job =
            job;

        _iterateFirst =
            first.Count <=
            second.Count;
    }

    public int Count =>
        _iterateFirst
            ? _first.Count
            : _second.Count;

    public void Execute(
        int index)
    {
        if (_iterateFirst)
        {
            var entity =
                _first.GetEntity(
                    index);

            ref var first =
                ref _first.GetByIndex(
                    index);

            ref var second =
                ref _second.Get(
                    entity);

            _job(
                entity,
                ref first,
                ref second);

            return;
        }

        var secondEntity =
            _second.GetEntity(
                index);

        ref var secondComponent =
            ref _second.GetByIndex(
                index);

        ref var firstComponent =
            ref _first.Get(
                secondEntity);

        _job(
            secondEntity,
            ref firstComponent,
            ref secondComponent);
    }
}