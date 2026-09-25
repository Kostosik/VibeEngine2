namespace Engine.Core.Systems;

public interface ISystemExecutionStrategy
{
    void Execute<TSystem>(
        IReadOnlyList<IReadOnlyList<TSystem>> batches,
        Action<TSystem> execute)
        where TSystem : class;
}