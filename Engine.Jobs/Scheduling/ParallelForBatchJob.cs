using Engine.Jobs.Jobs;

namespace Engine.Jobs.Scheduling;

internal sealed class ParallelForWorkerJob :
    IJob
{
    private readonly ParallelForState _state;

    public ParallelForWorkerJob(
        ParallelForState state)
    {
        ArgumentNullException.ThrowIfNull(
            state);

        _state =
            state;
    }

    public void Execute()
    {
        _state.Execute();
    }
}

internal sealed class ParallelForState
{
    private readonly IJobParallelFor _job;

    private readonly int _length;

    private readonly int _batchSize;

    private readonly int _batchCount;

    private int _nextBatch;

    public ParallelForState(
        IJobParallelFor job,
        int length,
        int batchSize)
    {
        ArgumentNullException.ThrowIfNull(
            job);

        _job =
            job;

        _length =
            length;

        _batchSize =
            batchSize;

        _batchCount =
            length == 0
                ? 0
                : (length - 1) /
                  batchSize +
                  1;
    }

    public void Execute()
    {
        while (true)
        {
            var batch =
                Interlocked.Increment(
                    ref _nextBatch) - 1;

            if ((uint)batch >=
                (uint)_batchCount)
            {
                return;
            }

            var startIndex =
                batch *
                _batchSize;

            var remaining =
                _length -
                startIndex;

            var count =
                Math.Min(
                    remaining,
                    _batchSize);

            var endIndex =
                startIndex +
                count;

            for (var index = startIndex;
                 index < endIndex;
                 index++)
            {
                _job.Execute(
                    index);
            }
        }
    }
}