using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;
using Engine.ECS.Entities;
using Engine.Jobs.Jobs;
using Engine.Jobs.Scheduling;
using System.Diagnostics;

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
BenchmarkWorldPoolReuse(entityCount);
BenchmarkEntityChurn(entityCount);
BenchmarkEntityChurnTiming(entityCount);
BenchmarkPopulatedWorldMemory(entityCount);
BenchmarkMultiComponentQuery(entityCount);
BenchmarkQueryScaling();
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
BenchmarkWorldLifecycleMemory(entityCount);
BenchmarkPopulatedWorldMemory(entityCount);

BenchmarkContentPipeline();
Engine.Benchmarks.EngineRuntimeGameBenchmark.Run();
Console.WriteLine(
    "Baseline complete.");


static void Benchmark(
    string name,
    int warmupIterations,
    int iterations,
    Action action,
    bool measureCurrentThreadAllocations = false)
{
    const int sampleCount = 5;

    // Прогрев: JIT-компиляция и первичные накладные расходы
    // должны по возможности произойти до измерений.
    for (var i = 0; i < warmupIterations; i++)
    {
        action();
    }

    // Одна очистка перед серией замеров.
    // Не вызываем GC принудительно между измеряемыми сериями:
    // естественные сборки мусора тоже являются частью нагрузки.
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var timesMsPerOperation =
        new double[sampleCount];

    var allocatedBytesPerOperation =
        new double[sampleCount];

    long totalGen0Collections = 0;
    long totalGen1Collections = 0;
    long totalGen2Collections = 0;

    // Создаём таймер до измерений, чтобы не учитывать
    // создание самого объекта Stopwatch.
    var stopwatch = new Stopwatch();

    for (var sample = 0; sample < sampleCount; sample++)
    {
        // false: не просим счётчик выделений инициировать
        // принудительную сборку мусора.
        var allocatedBefore = measureCurrentThreadAllocations
            ? GC.GetAllocatedBytesForCurrentThread()
            : GC.GetTotalAllocatedBytes(precise: true);

        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);

        stopwatch.Restart();

        for (var i = 0; i < iterations; i++)
        {
            action();
        }

        stopwatch.Stop();

        var allocatedAfter = measureCurrentThreadAllocations
            ? GC.GetAllocatedBytesForCurrentThread()
            : GC.GetTotalAllocatedBytes(precise: true);

        timesMsPerOperation[sample] =
            stopwatch.Elapsed.TotalMilliseconds / iterations;

        allocatedBytesPerOperation[sample] =
            (allocatedAfter - allocatedBefore) / (double)iterations;

        totalGen0Collections +=
            GC.CollectionCount(0) - gen0Before;

        totalGen1Collections +=
            GC.CollectionCount(1) - gen1Before;

        totalGen2Collections +=
            GC.CollectionCount(2) - gen2Before;
    }

    var medianTimeMs =
        Median(timesMsPerOperation);

    var medianAllocatedBytes =
        Median(allocatedBytesPerOperation);

    var minTimeMs =
        timesMsPerOperation.Min();

    var maxTimeMs =
        timesMsPerOperation.Max();

    Console.WriteLine(name);

    Console.WriteLine(
        $"  Samples:       {sampleCount} x {iterations} operations");

    Console.WriteLine(
        $"  Median time:   {medianTimeMs:F3} ms/op");

    Console.WriteLine(
        $"  Time range:    {minTimeMs:F3} - {maxTimeMs:F3} ms/op");

    Console.WriteLine(
        $"  Median alloc:  {medianAllocatedBytes:N1} bytes/op");

    Console.WriteLine(
        $"  GC collections: Gen0={totalGen0Collections}, " +
        $"Gen1={totalGen1Collections}, Gen2={totalGen2Collections}");

    Console.WriteLine();
}

static double Median(double[] values)
{
    if (values.Length == 0)
    {
        throw new ArgumentException(
            "At least one sample is required.",
            nameof(values));
    }

    var sorted = (double[])values.Clone();
    Array.Sort(sorted);

    var middle = sorted.Length / 2;

    if (sorted.Length % 2 != 0)
    {
        return sorted[middle];
    }

    return (sorted[middle - 1] + sorted[middle]) / 2.0;
}

static void BenchmarkWorldLifecycleMemory(int entityCount)
{
    const int warmupCycles = 2;
    const int measuredCycles = 5;

    // Прогреваем путь создания и освобождения мира.
    for (var i = 0; i < warmupCycles; i++)
    {
        CreateAndDisposeWorld(entityCount);
    }

    using var process = Process.GetCurrentProcess();

    CollectGarbageForSnapshot();

    var managedBefore =
        GC.GetTotalMemory(forceFullCollection: false);

    process.Refresh();

    var workingSetBefore =
        process.WorkingSet64;

    var privateBytesBefore =
        process.PrivateMemorySize64;

    // Измеряем несколько полных циклов жизненного цикла.
    for (var i = 0; i < measuredCycles; i++)
    {
        CreateAndDisposeWorld(entityCount);
    }

    CollectGarbageForSnapshot();

    var managedAfter =
        GC.GetTotalMemory(forceFullCollection: false);

    process.Refresh();

    var workingSetAfter =
        process.WorkingSet64;

    var privateBytesAfter =
        process.PrivateMemorySize64;

    Console.WriteLine("ECS World lifecycle memory");

    Console.WriteLine(
        $"  Entities per world: {entityCount:N0}");

    Console.WriteLine(
        $"  Warmup cycles:      {warmupCycles}");

    Console.WriteLine(
        $"  Measured cycles:    {measuredCycles}");

    Console.WriteLine(
        $"  Managed before:     {managedBefore:N0} bytes");

    Console.WriteLine(
        $"  Managed after:      {managedAfter:N0} bytes");

    Console.WriteLine(
        $"  Managed delta:      {managedAfter - managedBefore:N0} bytes");

    Console.WriteLine(
        $"  Working set before: {workingSetBefore:N0} bytes");

    Console.WriteLine(
        $"  Working set after:  {workingSetAfter:N0} bytes");

    Console.WriteLine(
        $"  Working set delta:  {workingSetAfter - workingSetBefore:N0} bytes");

    Console.WriteLine(
        $"  Private before:     {privateBytesBefore:N0} bytes");

    Console.WriteLine(
        $"  Private after:      {privateBytesAfter:N0} bytes");

    Console.WriteLine(
        $"  Private delta:      {privateBytesAfter - privateBytesBefore:N0} bytes");

    Console.WriteLine();
}


static void BenchmarkMultiComponentQuery(int entityCount)
{
    const int secondComponentStride = 2;

    const int warmupIterations = 5;
    const int iterations = 20;

    using var world = new Engine.ECS.World();

    // Все сущности имеют первый компонент.
    // Второй компонент есть только у каждой второй сущности.
    for (var i = 0; i < entityCount; i++)
    {
        var entity = world.CreateEntity();

        world.Add(
            entity,
            new TestComponent
            {
                Value = i
            });

        if (i % secondComponentStride == 0)
        {
            world.Add(
                entity,
                new TestComponent2
                {
                    Value = i * 2
                });
        }
    }

    Console.WriteLine();
    Console.WriteLine("ECS Multi-Component Query");
    Console.WriteLine($"  Entities: {entityCount:N0}");
    Console.WriteLine(
        $"  Entities with second component: " +
        $"{(entityCount + 1) / 2:N0}");

    Benchmark(
        "ECS Query Single Component",
        warmupIterations,
        iterations,
        () =>
        {
            world.ClearDirty<TestComponent>();

            long sum = 0;

            foreach (var item in world.Query<TestComponent>())
            {
                sum += item.Component.Value;
            }

            GC.KeepAlive(sum);
        });

    Benchmark(
        "ECS Query Component Pair",
        warmupIterations,
        iterations,
        () =>
        {
            world.ClearDirty<TestComponent>();
            world.ClearDirty<TestComponent2>();

            long sum = 0;

            foreach (
                var item in world.Query<TestComponent, TestComponent2>())
            {
                sum += item.First.Value;
                sum += item.Second.Value;
            }

            GC.KeepAlive(sum);
        });
}


static void CreateAndDisposeWorld(int entityCount)
{
    using var world =
        new Engine.ECS.World();

    for (var i = 0; i < entityCount; i++)
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
}


static void BenchmarkPopulatedWorldMemory(int entityCount)
{
    using var process = Process.GetCurrentProcess();

    CollectGarbageForSnapshot();

    var managedBefore = GC.GetTotalMemory(false);
    var allocatedBefore = GC.GetTotalAllocatedBytes(precise: false);

    process.Refresh();
    var workingSetBefore = process.WorkingSet64;
    var privateBefore = process.PrivateMemorySize64;

    Engine.ECS.World? world = null;

    long allocatedDuringCreation = 0;
    long managedWithWorld = 0;
    long workingSetWithWorld = 0;
    long privateWithWorld = 0;

    try
    {
        world = new Engine.ECS.World();

        for (var i = 0; i < entityCount; i++)
        {
            var entity = world.CreateEntity();

            world.Add(
                entity,
                new TestComponent
                {
                    Value = i
                });
        }

        allocatedDuringCreation =
            GC.GetTotalAllocatedBytes(precise: false)
            - allocatedBefore;

        CollectGarbageForSnapshot();

        managedWithWorld = GC.GetTotalMemory(false);

        process.Refresh();
        workingSetWithWorld = process.WorkingSet64;
        privateWithWorld = process.PrivateMemorySize64;

        // Мир должен оставаться живым до окончания измерения.
        GC.KeepAlive(world);
    }
    finally
    {
        world?.Dispose();
        world = null;
    }

    CollectGarbageForSnapshot();

    var managedAfterDispose = GC.GetTotalMemory(false);

    process.Refresh();
    var workingSetAfterDispose = process.WorkingSet64;
    var privateAfterDispose = process.PrivateMemorySize64;

    Console.WriteLine("ECS Populated World Memory");

    Console.WriteLine($"  Entities: {entityCount:N0}");

    Console.WriteLine(
        $"  Allocated during creation: {allocatedDuringCreation:N0} bytes");

    Console.WriteLine(
        $"  Allocated per entity: " +
        $"{allocatedDuringCreation / (double)entityCount:N1} bytes");

    Console.WriteLine(
        $"  Retained managed delta: " +
        $"{managedWithWorld - managedBefore:N0} bytes");

    Console.WriteLine(
        $"  Managed delta after dispose: " +
        $"{managedAfterDispose - managedBefore:N0} bytes");

    Console.WriteLine(
        $"  Working set delta with world: " +
        $"{workingSetWithWorld - workingSetBefore:N0} bytes");

    Console.WriteLine(
        $"  Working set delta after dispose: " +
        $"{workingSetAfterDispose - workingSetBefore:N0} bytes");

    Console.WriteLine(
        $"  Private memory delta with world: " +
        $"{privateWithWorld - privateBefore:N0} bytes");

    Console.WriteLine(
        $"  Private memory delta after dispose: " +
        $"{privateAfterDispose - privateBefore:N0} bytes");

    Console.WriteLine();
}


static void BenchmarkQueryScaling()
{
    const int warmupIterations = 5;
    const int iterations = 20;

    int[] entityCounts =
    {
        10_000,
        50_000,
        100_000
    };

    foreach (var entityCount in entityCounts)
    {
        using var world = new Engine.ECS.World();

        for (var i = 0; i < entityCount; i++)
        {
            var entity = world.CreateEntity();

            world.Add(
                entity,
                new TestComponent
                {
                    Value = i
                });

            if (i % 2 == 0)
            {
                world.Add(
                    entity,
                    new TestComponent2
                    {
                        Value = i * 2
                    });
            }
        }

        Console.WriteLine();
        Console.WriteLine(
            $"ECS Query Scaling: {entityCount:N0} entities");

        Benchmark(
            "Single Component",
            warmupIterations,
            iterations,
            () =>
            {
                world.ClearDirty<TestComponent>();

                long sum = 0;

                foreach (var item in world.Query<TestComponent>())
                {
                    sum += item.Component.Value;
                }

                GC.KeepAlive(sum);
            });

        Benchmark(
            "Component Pair (50% match)",
            warmupIterations,
            iterations,
            () =>
            {
                world.ClearDirty<TestComponent>();
                world.ClearDirty<TestComponent2>();

                long sum = 0;

                foreach (
                    var item in world.Query<TestComponent, TestComponent2>())
                {
                    sum += item.First.Value;
                    sum += item.Second.Value;
                }

                GC.KeepAlive(sum);
            });
    }

    Console.WriteLine();
}

static void BenchmarkEntityChurn(int entityCount)
{
    const int cycles = 5;

    using var world = new Engine.ECS.World();

    // Массив создаётся вне измеряемых циклов,
    // чтобы не включать его выделение в каждый замер.
    var entities = new EntityId[entityCount];

    var stopwatch = new Stopwatch();

    Console.WriteLine("ECS Entity Churn");

    Console.WriteLine(
        $"  Entities per cycle: {entityCount:N0}");

    Console.WriteLine(
        $"  Cycles: {cycles}");

    for (var cycle = 1; cycle <= cycles; cycle++)
    {
        var allocatedBefore =
            GC.GetTotalAllocatedBytes(precise: true);

        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);

        stopwatch.Restart();

        // Создание сущностей и компонентов.
        for (var i = 0; i < entityCount; i++)
        {
            var entity = world.CreateEntity();

            world.Add(
                entity,
                new TestComponent
                {
                    Value = i
                });

            entities[i] = entity;
        }

        if (world.EntityCount != entityCount)
        {
            throw new InvalidOperationException(
                "Unexpected entity count after creation.");
        }

        // Уничтожение тех же сущностей.
        for (var i = 0; i < entityCount; i++)
        {
            if (!world.DestroyEntity(entities[i]))
            {
                throw new InvalidOperationException(
                    $"Failed to destroy entity at index {i}.");
            }
        }

        // Очищаем накопленные метаданные изменений.
        world.ClearDirty<TestComponent>();

        stopwatch.Stop();

        var allocatedBytes =
            GC.GetTotalAllocatedBytes(precise: true)
            - allocatedBefore;

        var gen0 =
            GC.CollectionCount(0) - gen0Before;

        var gen1 =
            GC.CollectionCount(1) - gen1Before;

        var gen2 =
            GC.CollectionCount(2) - gen2Before;

        if (world.EntityCount != 0)
        {
            throw new InvalidOperationException(
                "World is not empty after destruction.");
        }

        Console.WriteLine($"  Cycle {cycle}:");

        Console.WriteLine(
            $"    Time: {stopwatch.Elapsed.TotalMilliseconds:F3} ms");

        Console.WriteLine(
            $"    Allocated: {allocatedBytes:N0} bytes");

        Console.WriteLine(
            $"    Allocated per entity: " +
            $"{allocatedBytes / (double)entityCount:N1} bytes");

        Console.WriteLine(
            $"    GC: Gen0={gen0}, Gen1={gen1}, Gen2={gen2}");

        Console.WriteLine(
            $"    Entities remaining: {world.EntityCount}");
    }

    Console.WriteLine();
}


static void BenchmarkEntityChurnTiming(int entityCount)
{
    const int warmupCycles = 5;
    const int measuredCycles = 9;

    using var world = new Engine.ECS.World();
    var entities = new EntityId[entityCount];

    void RunCycle()
    {
        for (var i = 0; i < entityCount; i++)
        {
            var entity = world.CreateEntity();

            world.Add(
                entity,
                new TestComponent { Value = i });

            entities[i] = entity;
        }

        for (var i = 0; i < entityCount; i++)
        {
            if (!world.DestroyEntity(entities[i]))
            {
                throw new InvalidOperationException(
                    $"Failed to destroy entity at index {i}.");
            }
        }

        world.ClearDirty<TestComponent>();

        if (world.EntityCount != 0)
        {
            throw new InvalidOperationException(
                "World is not empty after destruction.");
        }
    }

    // Прогреваем JIT и буферы до измерений.
    for (var i = 0; i < warmupCycles; i++)
    {
        RunCycle();
    }

    var times = new double[measuredCycles];
    var allocated = new long[measuredCycles];
    var stopwatch = new Stopwatch();

    for (var i = 0; i < measuredCycles; i++)
    {
        var allocatedBefore =
            GC.GetTotalAllocatedBytes(precise: true);

        stopwatch.Restart();
        RunCycle();
        stopwatch.Stop();

        var allocatedAfter =
            GC.GetTotalAllocatedBytes(precise: true);

        times[i] = stopwatch.Elapsed.TotalMilliseconds;
        allocated[i] = allocatedAfter - allocatedBefore;
    }

    var sortedTimes = (double[])times.Clone();
    Array.Sort(sortedTimes);

    var sortedAllocations = (long[])allocated.Clone();
    Array.Sort(sortedAllocations);

    long totalAllocated = 0;

    foreach (var value in allocated)
    {
        totalAllocated += value;
    }

    Console.WriteLine("ECS Entity Churn Timing");
    Console.WriteLine($"  Entities: {entityCount:N0}");
    Console.WriteLine($"  Warmup cycles: {warmupCycles}");
    Console.WriteLine($"  Measured cycles: {measuredCycles}");

    Console.WriteLine(
        $"  Median time: {sortedTimes[measuredCycles / 2]:F3} ms");

    Console.WriteLine(
        $"  Time range: {sortedTimes[0]:F3} - " +
        $"{sortedTimes[measuredCycles - 1]:F3} ms");

    Console.WriteLine(
        $"  Median allocated: " +
        $"{sortedAllocations[measuredCycles / 2]:N0} bytes/cycle");

    Console.WriteLine(
        $"  Total allocated: {totalAllocated:N0} bytes");

    Console.WriteLine();
}


static void BenchmarkContentPipeline()
{
    const int assetCount = 32;
    const int assetSizeBytes = 4096;
    const int warmupIterations = 5;
    const int iterations = 20;

    var root = Path.Combine(
        Path.GetTempPath(),
        $"VibeEngine2Benchmarks-{Guid.NewGuid():N}");

    Directory.CreateDirectory(root);

    try
    {
        var payload = new byte[assetSizeBytes];

        for (var i = 0; i < payload.Length; i++)
        {
            payload[i] = (byte)(i % 251);
        }

        var paths = new AssetPath[assetCount];

        // Создаём тестовые файлы вне измеряемых участков.
        for (var i = 0; i < assetCount; i++)
        {
            var name = $"asset-{i:D3}.probe";

            File.WriteAllBytes(
                Path.Combine(root, name),
                payload);

            paths[i] = new AssetPath(name);
        }

        var source = new FileAssetSource(root);
        var catalog = new FileContentCatalog(root);
        var registry = new ContentLoaderRegistry();

        registry.Register(new ProbeContentAssetLoader());

        using (var content = new ContentManager(
                   source,
                   catalog,
                   registry))
        {
            Console.WriteLine();
            Console.WriteLine("Content Pipeline");
            Console.WriteLine($"  Assets: {assetCount}");
            Console.WriteLine($"  Asset size: {assetSizeBytes:N0} bytes");

            // Сохраняем одну внешнюю ссылку, чтобы ресурс
            // оставался в кэше во время измерения cache hit.
            var cachedPath = paths[0];
            var cachedAsset =
                content.Load<ProbeContentAsset>(cachedPath);

            Benchmark(
                "ContentManager Cache Hit + Unload",
                warmupIterations,
                10_000,
                () =>
                {
                    var asset =
                        content.Load<ProbeContentAsset>(cachedPath);

                    if (asset.Length != assetSizeBytes)
                    {
                        throw new InvalidOperationException(
                            "Cached asset has an unexpected size.");
                    }

                    if (!content.Unload<ProbeContentAsset>(cachedPath))
                    {
                        throw new InvalidOperationException(
                            "Failed to release cached asset reference.");
                    }

                    GC.KeepAlive(asset);
                }, measureCurrentThreadAllocations: true);

            if (!content.Unload<ProbeContentAsset>(cachedPath))
            {
                throw new InvalidOperationException(
                    "Failed to unload the retained asset.");
            }

            GC.KeepAlive(cachedAsset);

            // Кэш ContentManager очищается после каждой операции.
            // Файловые данные при этом могут оставаться в кэше ОС.
            var filePath = paths[1];

            Benchmark(
                "ContentManager File Load + Unload",
                warmupIterations,
                iterations,
                () =>
                {
                    var asset =
                        content.Load<ProbeContentAsset>(filePath);

                    if (asset.Length != assetSizeBytes)
                    {
                        throw new InvalidOperationException(
                            "Loaded asset has an unexpected size.");
                    }

                    if (!content.Unload<ProbeContentAsset>(filePath))
                    {
                        throw new InvalidOperationException(
                            "Failed to unload file asset.");
                    }

                    GC.KeepAlive(asset);
                });

            var nextAsset = 0;

            Benchmark(
                "ContentManager Load + Unload (32 Assets)",
                warmupIterations,
                iterations,
                () =>
                {
                    var path =
                        paths[nextAsset++ % paths.Length];

                    var asset =
                        content.Load<ProbeContentAsset>(path);

                    if (asset.Length != assetSizeBytes)
                    {
                        throw new InvalidOperationException(
                            "Loaded asset has an unexpected size.");
                    }

                    if (!content.Unload<ProbeContentAsset>(path))
                    {
                        throw new InvalidOperationException(
                            "Failed to unload asset from the set.");
                    }

                    GC.KeepAlive(asset);
                });

            Benchmark(
    "ContentManager Batch Load + Unload (32 Assets)",
    warmupIterations,
    iterations,
    () =>
    {
        // Сначала загружаем весь набор.
        for (var i = 0; i < paths.Length; i++)
        {
            var asset =
                content.Load<ProbeContentAsset>(paths[i]);

            if (asset.Length != assetSizeBytes)
            {
                throw new InvalidOperationException(
                    $"Unexpected asset size at index {i}.");
            }
        }

        // Затем освобождаем все внешние ссылки.
        for (var i = 0; i < paths.Length; i++)
        {
            if (!content.Unload<ProbeContentAsset>(paths[i]))
            {
                throw new InvalidOperationException(
                    $"Failed to unload asset at index {i}.");
            }
        }
    });
        }
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    Console.WriteLine();
}


static void BenchmarkWorldPoolReuse(int entityCount)
{
    const int cycles = 5;

    CollectGarbageForSnapshot();

    var managedBaseline = GC.GetTotalMemory(false);

    Console.WriteLine("ECS World Pool Reuse");

    Console.WriteLine(
        $"  Entities per world: {entityCount:N0}");

    for (var cycle = 1; cycle <= cycles; cycle++)
    {
        var allocatedBefore =
            GC.GetTotalAllocatedBytes(precise: false);

        {
            using var world = new Engine.ECS.World();

            for (var i = 0; i < entityCount; i++)
            {
                var entity = world.CreateEntity();

                world.Add(
                    entity,
                    new TestComponent
                    {
                        Value = i
                    });
            }
        }

        var allocatedBytes =
            GC.GetTotalAllocatedBytes(precise: false)
            - allocatedBefore;

        CollectGarbageForSnapshot();

        var managedAfter =
            GC.GetTotalMemory(false);

        Console.WriteLine(
            $"  Cycle {cycle}:");

        Console.WriteLine(
            $"    Allocated: {allocatedBytes:N0} bytes");

        Console.WriteLine(
            $"    Allocated/entity: " +
            $"{allocatedBytes / (double)entityCount:N1} bytes");

        Console.WriteLine(
            $"    Managed delta from baseline: " +
            $"{managedAfter - managedBaseline:N0} bytes");
    }

    Console.WriteLine();
}


static void CollectGarbageForSnapshot()
{
    GC.Collect();

    GC.WaitForPendingFinalizers();

    GC.Collect();
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


sealed class ProbeContentAsset : IDisposable
{
    private byte[]? _bytes;

    public ProbeContentAsset(byte[] bytes)
    {
        _bytes = bytes;
    }

    public int Length => _bytes?.Length ?? 0;

    public void Dispose()
    {
        _bytes = null;
    }
}

sealed class ProbeContentAssetLoader :
    IContentLoader<ProbeContentAsset>
{
    public Type AssetType => typeof(ProbeContentAsset);

    public bool CanLoad(ContentAsset asset)
    {
        return string.Equals(
            asset.Extension,
            ".probe",
            StringComparison.OrdinalIgnoreCase);
    }

    public ProbeContentAsset Load(
        AssetPath path,
        IContentLoadContext context)
    {
        return new ProbeContentAsset(
            context.ReadBytes(path).ToArray());
    }

    object IContentLoader.Load(
        AssetPath path,
        IContentLoadContext context)
    {
        return Load(path, context);
    }
}


struct TestComponent2
{
    public int Value;
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