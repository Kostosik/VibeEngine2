using System.Diagnostics;
using Engine.ECS.Entities;
using Engine.Jobs.Jobs;
using Engine.Jobs.Scheduling;

const int entityCount =
    100_000;

const int iterations =
    20;

const int warmupIterations =
    5;

const int batchSize =
    256;

const int workerCount =
    4;

Console.WriteLine(
    "VibeEngine Performance Baseline");

Console.WriteLine(
    $"Runtime: {Environment.Version}");

Console.WriteLine(
    $"OS: {Environment.OSVersion}");

Console.WriteLine(
    $"ProcessorCount: {Environment.ProcessorCount}");

Console.WriteLine(
    $"Entities: {entityCount}");

Console.WriteLine(
    $"Iterations: {iterations}");

Console.WriteLine(
    $"Workers: {workerCount}");

Console.WriteLine(
    $"BatchSize: {batchSize}");

Console.WriteLine();

using var world =
    new Engine.ECS.World();

for (var i = 0;
     i < entityCount;
     i++)
{
    var entity =
        world.CreateEntity();

    world.Add(
        entity,
        new TestComponent
        {
            Value = i
        });
}

using var scheduler =
    new JobScheduler(
        workerCount);

Benchmark(
    "ECS Query",
    warmupIterations,
    iterations,
    () =>
    {
        world.ClearDirty<TestComponent>();

        var sum =
            0L;

        foreach (var item in
                 world.Query<TestComponent>())
        {
            sum +=
                item.Component.Value;
        }

        GC.KeepAlive(
            sum);
    });

Benchmark(
    "JobScheduler ParallelFor",
    warmupIterations,
    iterations,
    () =>
    {
        var handle =
            scheduler.ParallelFor(
                new IncrementJob(),
                entityCount,
                batchSize);

        scheduler.Wait(
            handle);
    });

world.ClearDirty<TestComponent>();

Benchmark(
    "JobScheduler ParallelFor NoOp",
    warmupIterations,
    iterations,
    () =>
    {
        var handle =
            scheduler.ParallelFor(
                new NoOpJob(),
                entityCount,
                batchSize);

        scheduler.Wait(
            handle);
    });

Benchmark(
    "JobScheduler ParallelFor AtomicIncrement",
    warmupIterations,
    iterations,
    () =>
    {
        var job =
            new AtomicIncrementJob();

        var handle =
            scheduler.ParallelFor(
                job,
                entityCount,
                batchSize);

        scheduler.Wait(
            handle);

        GC.KeepAlive(
            job);
    });

Benchmark(
    "JobScheduler ParallelFor AtomicAppend",
    warmupIterations,
    iterations,
    () =>
    {
        var job =
            new AtomicAppendJob(
                entityCount);

        var handle =
            scheduler.ParallelFor(
                job,
                entityCount,
                batchSize);

        scheduler.Wait(
            handle);

        GC.KeepAlive(
            job);
    });

Benchmark(
    "ECS ScheduleParallel",
    warmupIterations,
    iterations,
    () =>
    {
        world.ClearDirty<TestComponent>();

        var handle =
            world.ScheduleParallel(
                scheduler,
                (
                    EntityId entity,
                    ref TestComponent component) =>
                {
                    component.Value++;
                },
                batchSize);

        scheduler.Wait(
            handle);
    });

Benchmark(
    "ECS ScheduleParallel NoOp",
    warmupIterations,
    iterations,
    () =>
    {
        world.ClearDirty<TestComponent>();

        var handle =
            world.ScheduleParallel(
                scheduler,
                (
                    EntityId entity,
                    ref TestComponent component) =>
                {
                },
                batchSize);

        scheduler.Wait(
            handle);
    });

var batchSizes =
    new[]
    {
        64,
        256,
        1024,
        4096
    };

foreach (var currentBatchSize in
         batchSizes)
{
    Benchmark(
        $"JobScheduler ParallelFor NoOp Batch={currentBatchSize}",
        warmupIterations,
        iterations,
        () =>
        {
            var handle =
                scheduler.ParallelFor(
                    new NoOpJob(),
                    entityCount,
                    currentBatchSize);

            scheduler.Wait(
                handle);
        });
}

Console.WriteLine(
    "Baseline complete.");

static void Benchmark(
    string name,
    int warmupIterations,
    int iterations,
    Action action)
{
    for (var i = 0;
         i < warmupIterations;
         i++)
    {
        action();
    }

    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var allocatedBefore =
        GC.GetTotalAllocatedBytes(
            true);

    var gen0Before =
        GC.CollectionCount(0);

    var gen1Before =
        GC.CollectionCount(1);

    var gen2Before =
        GC.CollectionCount(2);

    var stopwatch =
        Stopwatch.StartNew();

    for (var i = 0;
         i < iterations;
         i++)
    {
        action();
    }

    stopwatch.Stop();

    var allocatedAfter =
        GC.GetTotalAllocatedBytes(
            true);

    var gen0After =
        GC.CollectionCount(0);

    var gen1After =
        GC.CollectionCount(1);

    var gen2After =
        GC.CollectionCount(2);

    var totalMs =
        stopwatch.Elapsed.TotalMilliseconds;

    var averageMs =
        totalMs /
        iterations;

    var allocatedBytes =
        allocatedAfter -
        allocatedBefore;

    Console.WriteLine(
        name);

    Console.WriteLine(
        $"  Total:       {totalMs:F3} ms");

    Console.WriteLine(
        $"  Average:     {averageMs:F3} ms");

    Console.WriteLine(
        $"  Allocated:   {allocatedBytes:N0} bytes");

    Console.WriteLine(
        $"  Gen0:        {gen0After - gen0Before}");

    Console.WriteLine(
        $"  Gen1:        {gen1After - gen1Before}");

    Console.WriteLine(
        $"  Gen2:        {gen2After - gen2Before}");

    Console.WriteLine();
}

struct TestComponent
{
    public int Value;
}

sealed class NoOpJob :
    IJobParallelFor
{
    public void Execute(
        int index)
    {
    }
}

sealed class AtomicIncrementJob :
    IJobParallelFor
{
    private int _counter;

    public void Execute(
        int index)
    {
        Interlocked.Increment(
            ref _counter);
    }
}

sealed class AtomicAppendJob :
    IJobParallelFor
{
    private readonly EntityId[] _values;

    private int _count;

    public AtomicAppendJob(
        int capacity)
    {
        _values =
            new EntityId[capacity];
    }

    public void Execute(
        int index)
    {
        var position =
            Interlocked.Increment(
                ref _count) - 1;

        _values[position] =
            new EntityId(
                (uint)index + 1,
                1);
    }
}

sealed class IncrementJob :
    IJobParallelFor
{
    public void Execute(
        int index)
    {
        Thread.SpinWait(
            1);
    }
}