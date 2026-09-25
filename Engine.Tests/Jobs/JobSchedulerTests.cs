using Engine.Jobs.Jobs;
using Engine.Jobs.Scheduling;
using System.Reflection.Metadata;

namespace Engine.Tests.Jobs;

public sealed class JobSchedulerTests
{
    private sealed class ActionParallelJob :
    IJobParallelFor
    {
        private readonly Action<int> _action;

        public ActionParallelJob(
            Action<int> action)
        {
            _action =
                action;
        }

        public void Execute(
            int index)
        {
            _action(
                index);
        }
    }
    private sealed class CountingParallelJob :
    IJobParallelFor
    {
        private readonly int[] _counters;

        public CountingParallelJob(
            int[] counters)
        {
            _counters =
                counters;
        }

        public void Execute(
            int index)
        {
            Interlocked.Increment(
                ref _counters[index]);
        }
    }
    [Fact]
    public void ParallelFor_WithZeroLength_CompletesImmediately()
    {
        using var scheduler =
            new JobScheduler(2);

        var executed =
            false;

        var job =
            new ActionParallelJob(
                _ =>
                    executed = true);

        var handle =
            scheduler.ParallelFor(
                job,
                0);

        Assert.True(
            scheduler.IsCompleted(
                handle));

        Assert.False(
            executed);
    }
    [Fact]
    public void ParallelFor_RespectsDependencies()
    {
        using var scheduler =
            new JobScheduler(2);

        using var release =
            new ManualResetEventSlim();

        var dependency =
            scheduler.Schedule(
                new BlockingJob(
                    release));

        var executedBeforeRelease =
            0;

        var job =
            new ActionParallelJob(
                _ =>
                    Interlocked.Increment(
                        ref executedBeforeRelease));

        var handle =
            scheduler.ParallelFor(
                job,
                100,
                10,
                dependency);

        Assert.Equal(
            0,
            Volatile.Read(
                ref executedBeforeRelease));

        release.Set();

        scheduler.Wait(
            handle);

        Assert.Equal(
            100,
            executedBeforeRelease);
    }
    [Fact]
    public void ParallelFor_ExecutesEveryIndexExactlyOnce()
    {
        using var scheduler =
            new JobScheduler(4);

        const int length = 1_000;

        var counters =
            new int[length];

        var job =
            new CountingParallelJob(
                counters);

        var handle =
            scheduler.ParallelFor(
                job,
                length,
                32);

        scheduler.Wait(
            handle);

        for (var index = 0;
             index < counters.Length;
             index++)
        {
            Assert.Equal(
                1,
                counters[index]);
        }
    }
    [Fact]
    public void CombineDependencies_PropagatesDependencyFailure()
    {
        using var scheduler =
            new JobScheduler(2);

        var dependency =
            scheduler.Schedule(
                new ThrowingJob());

        var group =
            scheduler.CombineDependencies(
                dependency);

        var dependentExecuted =
            false;

        var dependent =
            scheduler.Schedule(
                new ActionJob(
                    () =>
                        dependentExecuted = true),
                group);

        Assert.Throws<InvalidOperationException>(
            () =>
                scheduler.Wait(
                    dependent));

        Assert.False(
            dependentExecuted);
    }
    [Fact]
    public void CombineDependencies_CanBeUsedAsDependency()
    {
        using var scheduler =
            new JobScheduler(2);

        var firstExecuted =
            false;

        var secondExecuted =
            false;

        var first =
            scheduler.Schedule(
                new ActionJob(
                    () =>
                        firstExecuted = true));

        var second =
            scheduler.Schedule(
                new ActionJob(
                    () =>
                        secondExecuted = true));

        var group =
            scheduler.CombineDependencies(
                first,
                second);

        var dependentExecuted =
            false;

        var dependent =
            scheduler.Schedule(
                new ActionJob(
                    () =>
                        dependentExecuted = true),
                group);

        scheduler.Wait(
            dependent);

        Assert.True(
            firstExecuted);

        Assert.True(
            secondExecuted);

        Assert.True(
            dependentExecuted);
    }
    [Fact]
    public void CombineDependencies_CompletesAfterAllDependencies()
    {
        using var scheduler =
            new JobScheduler(2);

        using var firstRelease =
            new ManualResetEventSlim();

        using var secondRelease =
            new ManualResetEventSlim();

        using var groupDependentExecuted =
            new ManualResetEventSlim();

        var first =
            scheduler.Schedule(
                new BlockingJob(
                    firstRelease));

        var second =
            scheduler.Schedule(
                new BlockingJob(
                    secondRelease));

        var group =
            scheduler.CombineDependencies(
                first,
                second);

        var dependent =
            scheduler.Schedule(
                new SignalJob(
                    groupDependentExecuted),
                group);

        firstRelease.Set();

        scheduler.Wait(
            first);

        Assert.False(
            groupDependentExecuted.IsSet);

        secondRelease.Set();

        scheduler.Wait(
            dependent);

        Assert.True(
            groupDependentExecuted.IsSet);
    }
    private sealed class ActionJob :
    IJob
    {
        private readonly Action _action;

        public ActionJob(
            Action action)
        {
            _action =
                action;
        }

        public void Execute()
        {
            _action();
        }
    }
    private sealed class SignalJob :
    IJob
    {
        private readonly ManualResetEventSlim _signal;

        public SignalJob(
            ManualResetEventSlim signal)
        {
            _signal =
                signal;
        }

        public void Execute()
        {
            _signal.Set();
        }
    }

    private sealed class BlockingJob :
    IJob
    {
        private readonly ManualResetEventSlim _release;

        public BlockingJob(
            ManualResetEventSlim release)
        {
            _release =
                release;
        }

        public void Execute()
        {
            _release.Wait();
        }
    }

    [Fact]
    public void DependentJob_IsNotExecutedWhenDependencyFails()
    {
        using var scheduler =
            new JobScheduler(2);

        var executed =
            false;

        var dependency =
            scheduler.Schedule(
                new ThrowingJob());

        var dependent =
            scheduler.Schedule(
                new ActionJob(
                    () =>
                        executed = true),
                dependency);

        Assert.Throws<InvalidOperationException>(
            () =>
                scheduler.Wait(
                    dependency));

        Assert.Throws<InvalidOperationException>(
            () =>
                scheduler.Wait(
                    dependent));

        Assert.False(
            executed);
    }

    [Fact]
    public void DependentJob_WaitsForAllDependencies()
    {
        using var scheduler =
            new JobScheduler(2);

        using var firstRelease =
            new ManualResetEventSlim();

        using var secondRelease =
            new ManualResetEventSlim();

        using var dependentExecuted =
            new ManualResetEventSlim();

        var first =
            scheduler.Schedule(
                new BlockingJob(
                    firstRelease));

        var second =
            scheduler.Schedule(
                new BlockingJob(
                    secondRelease));

        var dependent =
            scheduler.Schedule(
                new SignalJob(
                    dependentExecuted),
                first,
                second);

        firstRelease.Set();

        scheduler.Wait(
            first);

        Assert.False(
            dependentExecuted.IsSet);

        secondRelease.Set();

        scheduler.Wait(
            dependent);

        Assert.True(
            dependentExecuted.IsSet);
    }

    [Fact]
    public void DependentJob_DoesNotExecuteBeforeDependency()
    {
        using var scheduler =
            new JobScheduler(2);

        using var dependencyRelease =
            new ManualResetEventSlim();

        using var dependentExecuted =
            new ManualResetEventSlim();

        var dependency =
            scheduler.Schedule(
                new BlockingJob(
                    dependencyRelease));

        var dependent =
            scheduler.Schedule(
                new SignalJob(
                    dependentExecuted),
                dependency);

        Assert.False(
            dependentExecuted.IsSet);

        dependencyRelease.Set();

        scheduler.Wait(
            dependent);

        Assert.True(
            dependentExecuted.IsSet);
    }

    [Fact]
    public void Schedule_ExecutesJob()
    {
        using var scheduler =
            new JobScheduler(
                1);

        var job =
            new RecordingJob();

        var handle =
            scheduler.Schedule(
                job);

        scheduler.Wait(
            handle);

        Assert.Equal(
            1,
            job.ExecutionCount);
    }

    [Fact]
    public void IsCompleted_IsFalseUntilJobFinishes()
    {
        using var scheduler =
            new JobScheduler(
                1);

        using var started =
            new ManualResetEventSlim();

        using var release =
            new ManualResetEventSlim();

        var job =
            new ControlledJob(
                started,
                release);

        var handle =
            scheduler.Schedule(
                job);

        Assert.True(
            started.Wait(
                TimeSpan.FromSeconds(2)));

        Assert.False(
            scheduler.IsCompleted(
                handle));

        release.Set();

        scheduler.Wait(
            handle);

        Assert.True(
            scheduler.IsCompleted(
                handle));
    }

    [Fact]
    public void JobException_IsPropagatedByWait()
    {
        using var scheduler =
            new JobScheduler(
                1);

        var handle =
            scheduler.Schedule(
                new ThrowingJob());

        Assert.Throws<InvalidOperationException>(
            () =>
                scheduler.Wait(
                    handle));
    }

    [Fact]
    public void InvalidHandle_CompletesImmediately()
    {
        using var scheduler =
            new JobScheduler(
                1);

        var handle =
            default(JobHandle);

        scheduler.Wait(
            handle);

        Assert.True(
            scheduler.IsCompleted(
                handle));
    }

    [Fact]
    public void Schedule_AfterDispose_Throws()
    {
        var scheduler =
            new JobScheduler(
                1);

        scheduler.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () =>
                scheduler.Schedule(
                    new RecordingJob()));
    }

    [Fact]
    public void WorkerCount_IsConfigured()
    {
        using var scheduler =
            new JobScheduler(
                3);

        Assert.Equal(
            3,
            scheduler.WorkerCount);
    }

    private sealed class RecordingJob :
        IJob
    {
        public int ExecutionCount { get; private set; }

        public void Execute()
        {
            ExecutionCount++;
        }
    }

    private sealed class ThrowingJob :
        IJob
    {
        public void Execute()
        {
            throw new InvalidOperationException(
                "Job failed.");
        }
    }

    private sealed class ControlledJob :
        IJob
    {
        private readonly ManualResetEventSlim _started;
        private readonly ManualResetEventSlim _release;

        public ControlledJob(
            ManualResetEventSlim started,
            ManualResetEventSlim release)
        {
            _started =
                started;

            _release =
                release;
        }

        public void Execute()
        {
            _started.Set();

            _release.Wait();
        }
    }

    [Fact]
    public void Schedule_ExecutesJobsOnMultipleWorkers()
    {
        using var scheduler =
            new JobScheduler(
                2);

        using var firstStarted =
            new ManualResetEventSlim();

        using var secondStarted =
            new ManualResetEventSlim();

        using var release =
            new ManualResetEventSlim();

        var first =
            new SynchronizationJob(
                firstStarted,
                release);

        var second =
            new SynchronizationJob(
                secondStarted,
                release);

        var firstHandle =
            scheduler.Schedule(
                first);

        var secondHandle =
            scheduler.Schedule(
                second);

        Assert.True(
            firstStarted.Wait(
                TimeSpan.FromSeconds(2)));

        Assert.True(
            secondStarted.Wait(
                TimeSpan.FromSeconds(2)));

        release.Set();

        scheduler.Wait(
            firstHandle);

        scheduler.Wait(
            secondHandle);

        Assert.True(
            first.Executed);

        Assert.True(
            second.Executed);
    }

    private sealed class SynchronizationJob :
    IJob
    {
        private readonly ManualResetEventSlim _started;
        private readonly ManualResetEventSlim _release;

        public SynchronizationJob(
            ManualResetEventSlim started,
            ManualResetEventSlim release)
        {
            _started =
                started;

            _release =
                release;
        }

        public bool Executed { get; private set; }

        public void Execute()
        {
            _started.Set();

            _release.Wait();

            Executed =
                true;
        }
    }
}