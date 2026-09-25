using Engine.Core.Systems;
using Engine.Jobs.Jobs;
using Engine.Jobs.Scheduling;
using System.Runtime.ExceptionServices;

namespace Engine.Jobs.Integration;

public sealed class JobSystemExecutionStrategy :
    ISystemExecutionStrategy
{
    private readonly JobScheduler _scheduler;

    public JobSystemExecutionStrategy(
        JobScheduler scheduler)
    {
        ArgumentNullException.ThrowIfNull(
            scheduler);

        _scheduler =
            scheduler;
    }

    public void Execute<TSystem>(
        IReadOnlyList<IReadOnlyList<TSystem>> batches,
        Action<TSystem> execute)
        where TSystem : class
    {
        ArgumentNullException.ThrowIfNull(
            batches);

        ArgumentNullException.ThrowIfNull(
            execute);

        JobHandle previousBatch =
            default;

        foreach (var batch in batches)
        {
            if (batch.Count == 0)
            {
                continue;
            }

            var handles =
                new JobHandle[batch.Count];

            for (var i = 0;
                 i < batch.Count;
                 i++)
            {
                var job =
                    new SystemJob<TSystem>(
                        batch[i],
                        execute,
                        _scheduler);

                handles[i] =
                    previousBatch.IsValid
                        ? _scheduler.Schedule(
                            job,
                            previousBatch)
                        : _scheduler.Schedule(
                            job);
            }

            previousBatch =
                handles.Length == 1
                    ? handles[0]
                    : _scheduler.CombineDependencies(
                        handles);
        }

        if (previousBatch.IsValid)
        {
            _scheduler.Wait(
                previousBatch);
        }
    }

    private sealed class SystemJob<TSystem> :
        IJob
        where TSystem : class
    {
        private readonly TSystem _system;

        private readonly Action<TSystem> _execute;

        private readonly JobScheduler _scheduler;

        public SystemJob(
            TSystem system,
            Action<TSystem> execute,
            JobScheduler scheduler)
        {
            _system =
                system;

            _execute =
                execute;

            _scheduler =
                scheduler;
        }

        public void Execute()
        {
            var scope =
                _scheduler.CreateScope();

            using var activation =
                scope.Enter();

            ExceptionDispatchInfo? failure =
                null;

            try
            {
                _execute(
                    _system);
            }
            catch (Exception exception)
            {
                failure =
                    ExceptionDispatchInfo.Capture(
                        exception);
            }

            try
            {
                scope.Wait();
            }
            catch when (failure is not null)
            {
                // Preserve the original system exception.
            }

            failure?.Throw();
        }
    }
}