using Engine.Jobs.Jobs;

namespace Engine.Jobs.Scheduling;

internal sealed class ParallelForBatchJob :
    IJob
{
    private readonly IJobParallelFor _job;

    private readonly int _startIndex;

    private readonly int _endIndex;

    public ParallelForBatchJob(
        IJobParallelFor job,
        int startIndex,
        int endIndex)
    {
        ArgumentNullException.ThrowIfNull(
            job);

        if (startIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startIndex));
        }

        if (endIndex < startIndex)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endIndex));
        }

        _job =
            job;

        _startIndex =
            startIndex;

        _endIndex =
            endIndex;
    }

    public void Execute()
    {
        for (var index = _startIndex;
             index < _endIndex;
             index++)
        {
            _job.Execute(
                index);
        }
    }
}