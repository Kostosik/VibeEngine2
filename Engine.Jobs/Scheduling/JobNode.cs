using Engine.Jobs.Jobs;

namespace Engine.Jobs.Scheduling;

internal sealed class JobNode
{
    private IJob? _job;

    public JobNode(
        int id,
        object owner,
        IJob? job,
        JobCompletion completion)
    {
        Id =
            id;

        Owner =
            owner;

        _job =
            job;

        Completion =
            completion;
    }

    public int Id { get; }

    public object Owner { get; }

    public IJob? Job =>
        _job;

    public JobCompletion Completion { get; }

    public int RemainingDependencies { get; set; }

    public Exception? DependencyException { get; set; }

    public List<JobNode> Dependents { get; } = new();

    public void ClearJob()
    {
        _job =
            null;
    }
}