using Engine.Jobs.Jobs;

namespace Engine.Jobs.Scheduling;

internal sealed class JobRecord
{
    public JobRecord(
        int id,
        IJob job,
        JobCompletion completion)
    {
        Id =
            id;

        Job =
            job;

        Completion =
            completion;
    }

    public int Id { get; }

    public IJob Job { get; }

    public JobCompletion Completion { get; }

    public int RemainingDependencies { get; set; }

    public bool DependencyFailed { get; set; }

    public List<JobRecord> Dependents { get; } = new();
}