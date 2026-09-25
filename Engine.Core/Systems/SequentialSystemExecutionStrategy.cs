namespace Engine.Core.Systems;

internal sealed class SequentialSystemExecutionStrategy :
    ISystemExecutionStrategy
{
    public void Execute<TSystem>(
        IReadOnlyList<IReadOnlyList<TSystem>> batches,
        Action<TSystem> execute)
        where TSystem : class
    {
        ArgumentNullException.ThrowIfNull(
            batches);

        ArgumentNullException.ThrowIfNull(
            execute);

        foreach (var batch in batches)
        {
            foreach (var system in batch)
            {
                execute(
                    system);
            }
        }
    }
}