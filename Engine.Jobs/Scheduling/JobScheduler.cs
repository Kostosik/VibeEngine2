using System.Collections.Concurrent;
using Engine.Jobs.Jobs;

namespace Engine.Jobs.Scheduling;

public sealed class JobScheduler :
    IDisposable
{
    private readonly BlockingCollection<JobWorkItem> _queue =
        new();

    private readonly ManualResetEventSlim _allJobsCompleted =
        new(true);

    private readonly object _sync =
        new();

    private readonly object _identity =
        new();

    private readonly Thread[] _workers;

    private int _nextJobId;

    private int _outstandingJobs;

    private bool _disposed;

    public JobScheduler(
        int workerCount = 0)
    {
        if (workerCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(workerCount));
        }

        if (workerCount == 0)
        {
            workerCount =
                Math.Max(
                    1,
                    Environment.ProcessorCount - 1);
        }

        _workers =
            new Thread[workerCount];

        for (var i = 0;
             i < _workers.Length;
             i++)
        {
            var worker =
                new Thread(
                    WorkerLoop)
                {
                    IsBackground = true,
                    Name = $"Engine Job Worker {i + 1}"
                };

            _workers[i] =
                worker;

            worker.Start();
        }
    }

    public int WorkerCount =>
        _workers.Length;

    public int PendingJobCount =>
        _queue.Count;

    public JobScope CreateScope()
    {
        EnsureNotDisposed();

        return new JobScope(
            this);
    }

    public JobHandle Schedule(
        IJob job,
        params JobHandle[] dependencies)
    {
        ArgumentNullException.ThrowIfNull(
            job);

        ArgumentNullException.ThrowIfNull(
            dependencies);

        lock (_sync)
        {
            EnsureNotDisposed();

            var node =
                CreateNode(
                    job);

            ValidateDependencies(
                node,
                dependencies);

            ActivateNode(
                node);

            RegisterDependencies(
                node,
                dependencies);

            StartNodeIfReady(
                node);

            return new JobHandle(
                node);
        }
    }

    public JobHandle ParallelFor(
    IJobParallelFor job,
    int length,
    int batchSize = 64,
    params JobHandle[] dependencies)
    {
        ArgumentNullException.ThrowIfNull(
            job);

        ArgumentNullException.ThrowIfNull(
            dependencies);

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length));
        }

        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batchSize));
        }

        if (length == 0)
        {
            return CombineDependencies();
        }

        JobHandle dependencyHandle =
            default;

        if (dependencies.Length > 0)
        {
            dependencyHandle =
                CombineDependencies(
                    dependencies);
        }

        var batchCount =
            (length + batchSize - 1) /
            batchSize;

        var handles =
            new JobHandle[batchCount];

        for (var batch = 0;
             batch < batchCount;
             batch++)
        {
            var startIndex =
                batch *
                batchSize;

            var endIndex =
                Math.Min(
                    startIndex + batchSize,
                    length);

            var batchJob =
                new ParallelForBatchJob(
                    job,
                    startIndex,
                    endIndex);

            handles[batch] =
                dependencies.Length > 0
                    ? Schedule(
                        batchJob,
                        dependencyHandle)
                    : Schedule(
                        batchJob);
        }

        return CombineDependencies(
            handles);
    }

    public JobHandle CombineDependencies(
        params JobHandle[] dependencies)
    {
        ArgumentNullException.ThrowIfNull(
            dependencies);

        lock (_sync)
        {
            EnsureNotDisposed();

            var node =
                CreateNode(
                    null);

            ValidateDependencies(
                node,
                dependencies);

            ActivateNode(
                node);

            RegisterDependencies(
                node,
                dependencies);

            StartNodeIfReady(
                node);

            return new JobHandle(
                node);
        }
    }

    public void Wait(
        JobHandle handle)
    {
        EnsureNotDisposed();

        if (!handle.IsValid)
        {
            return;
        }

        var node =
            GetNode(
                handle);

        while (!node.Completion.IsCompleted)
        {
            if (_queue.TryTake(
                    out var workItem,
                    10))
            {
                Execute(
                    workItem.Node);

                continue;
            }

            Thread.Yield();
        }

        node.Completion.Wait();
    }

    public bool IsCompleted(
        JobHandle handle)
    {
        EnsureNotDisposed();

        if (!handle.IsValid)
        {
            return true;
        }

        var node =
            GetNode(
                handle);

        return node.Completion.IsCompleted;
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed =
                true;
        }

        _allJobsCompleted.Wait();

        _queue.CompleteAdding();

        foreach (var worker in _workers)
        {
            worker.Join();
        }

        _queue.Dispose();

        _allJobsCompleted.Dispose();
    }

    private JobNode CreateNode(
        IJob? job)
    {
        var id =
            Interlocked.Increment(
                ref _nextJobId);

        return new JobNode(
            id,
            _identity,
            job,
            new JobCompletion());
    }

    private void ActivateNode(
        JobNode node)
    {
        if (_outstandingJobs == 0)
        {
            _allJobsCompleted.Reset();
        }

        _outstandingJobs++;
    }

    private void StartNodeIfReady(
        JobNode node)
    {
        if (node.RemainingDependencies != 0)
        {
            return;
        }

        if (node.DependencyException is not null)
        {
            CompleteNodes(
                (
                    node,
                    node.DependencyException));

            return;
        }

        if (node.Job is null)
        {
            CompleteNodes(
                (
                    node,
                    null));

            return;
        }

        if (!Enqueue(
                node))
        {
            CompleteNodes(
                (
                    node,
                    new InvalidOperationException(
                        "Job could not be queued.")));
        }
    }

    private void WorkerLoop()
    {
        try
        {
            foreach (var workItem in _queue.GetConsumingEnumerable())
            {
                Execute(
                    workItem.Node);
            }
        }
        catch (ObjectDisposedException)
        {
            // Scheduler shutdown.
        }
    }

    private void Execute(
        JobNode node)
    {
        var job =
            node.Job;

        if (job is null)
        {
            lock (_sync)
            {
                CompleteNodes(
                    (
                        node,
                        new InvalidOperationException(
                            $"Job node '{node.Id}' does not contain executable work.")));
            }

            return;
        }

        Exception? exception = null;

        try
        {
            job.Execute();
        }
        catch (Exception caughtException)
        {
            exception =
                caughtException;
        }

        lock (_sync)
        {
            node.ClearJob();

            CompleteNodes(
                (
                    node,
                    exception));
        }
    }

    private void CompleteNodes(
        params (JobNode Node, Exception? Exception)[] initial)
    {
        var pending =
            new Queue<(
                JobNode Node,
                Exception? Exception)>();

        foreach (var completion in initial)
        {
            pending.Enqueue(
                completion);
        }

        while (pending.Count > 0)
        {
            var current =
                pending.Dequeue();

            var completed =
                current.Exception is null
                    ? current.Node.Completion.Complete()
                    : current.Node.Completion.Fail(
                        current.Exception);

            if (!completed)
            {
                continue;
            }

            current.Node.ClearJob();

            foreach (var dependent in current.Node.Dependents)
            {
                if (current.Exception is not null &&
                    dependent.DependencyException is null)
                {
                    dependent.DependencyException =
                        current.Exception;
                }

                dependent.RemainingDependencies--;

                if (dependent.RemainingDependencies != 0)
                {
                    continue;
                }

                if (dependent.DependencyException is not null)
                {
                    pending.Enqueue(
                        (
                            dependent,
                            dependent.DependencyException));

                    continue;
                }

                if (dependent.Job is null)
                {
                    pending.Enqueue(
                        (
                            dependent,
                            null));

                    continue;
                }

                if (!Enqueue(
                        dependent))
                {
                    pending.Enqueue(
                        (
                            dependent,
                            new InvalidOperationException(
                                "Job could not be queued.")));
                }
            }

            current.Node.Dependents.Clear();

            _outstandingJobs--;

            if (_outstandingJobs == 0)
            {
                _allJobsCompleted.Set();
            }
        }
    }

    private void RegisterDependencies(
        JobNode node,
        IReadOnlyList<JobHandle> dependencies)
    {
        var uniqueDependencies =
            new HashSet<int>();

        foreach (var dependencyHandle in dependencies)
        {
            if (!uniqueDependencies.Add(
                    dependencyHandle.Node.Id))
            {
                continue;
            }

            var dependency =
                dependencyHandle.Node;

            if (dependency.Completion.IsCompleted)
            {
                if (!dependency.Completion.IsSuccessful &&
                    node.DependencyException is null)
                {
                    node.DependencyException =
                        dependency.Completion.Failure ??
                        new InvalidOperationException(
                            $"Job '{dependency.Id}' failed.");
                }

                continue;
            }

            node.RemainingDependencies++;

            dependency.Dependents.Add(
                node);
        }
    }

    private void ValidateDependencies(
        JobNode node,
        IReadOnlyList<JobHandle> dependencies)
    {
        foreach (var dependencyHandle in dependencies)
        {
            if (!dependencyHandle.IsValid)
            {
                throw new ArgumentException(
                    "Job dependencies must be valid handles.",
                    nameof(dependencies));
            }

            var dependency =
                GetNode(
                    dependencyHandle);

            if (dependency.Id == node.Id)
            {
                throw new InvalidOperationException(
                    "A job cannot depend on itself.");
            }
        }
    }

    private bool Enqueue(
        JobNode node)
    {
        try
        {
            _queue.Add(
                new JobWorkItem(
                    node));

            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private JobNode GetNode(
        JobHandle handle)
    {
        var node =
            handle.Node;

        if (!ReferenceEquals(
                node.Owner,
                _identity))
        {
            throw new InvalidOperationException(
                "Job handle belongs to a different scheduler.");
        }

        return node;
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }

    internal bool Owns(
    JobHandle handle)
    {
        if (!handle.IsValid)
        {
            return false;
        }

        var node =
            handle.Node;

        return ReferenceEquals(
            node.Owner,
            _identity);
    }
}