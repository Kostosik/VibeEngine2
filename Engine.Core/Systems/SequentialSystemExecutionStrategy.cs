namespace Engine.Core.Systems;

internal sealed class SequentialSystemExecutionStrategy :
    ISystemExecutionStrategy,
    IStatefulSystemExecutionStrategy
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

    public void Execute<TSystem, TState>(
        IReadOnlyList<IReadOnlyList<TSystem>> batches,
        TState state,
        Action<TState, TSystem> execute)
        where TSystem : class
    {
        ArgumentNullException.ThrowIfNull(batches);
        ArgumentNullException.ThrowIfNull(execute);

        for (var batchIndex = 0;
             batchIndex < batches.Count;
             batchIndex++)
        {
            var batch = batches[batchIndex];

            for (var systemIndex = 0;
                 systemIndex < batch.Count;
                 systemIndex++)
            {
                execute(
                    state,
                    batch[systemIndex]);
            }
        }
    }
}