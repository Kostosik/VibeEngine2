namespace Engine.Jobs.Scheduling;

internal readonly struct JobWorkItem
{
    public JobWorkItem(
        JobNode node)
    {
        Node =
            node;
    }

    public JobNode Node { get; }
}