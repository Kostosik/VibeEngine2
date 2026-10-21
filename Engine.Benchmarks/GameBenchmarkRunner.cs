
using System.Diagnostics;

namespace Engine.Benchmarks;

internal static class GameBenchmarkRunner
{
    public static void Run(
        string scenarioName,
        Action initialize,
        Action update,
        Action fixedUpdate,
        Action shutdown,
        int warmupFrames = 120,
        int measuredFrames = 600,
        int fixedUpdatesPerFrame = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            scenarioName);

        ArgumentNullException.ThrowIfNull(initialize);
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(fixedUpdate);
        ArgumentNullException.ThrowIfNull(shutdown);

        if (warmupFrames < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(warmupFrames));
        }

        if (measuredFrames <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(measuredFrames));
        }

        if (fixedUpdatesPerFrame < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fixedUpdatesPerFrame));
        }

        var updateTimes = new double[measuredFrames];
        var fixedUpdateTimes = new double[measuredFrames];
        var frameTimes = new double[measuredFrames];

        var initialized = false;

        double initializationMs = 0;
        double shutdownMs = 0;

        long allocatedBytes = 0;
        long updateAllocatedBytes = 0;
        long fixedUpdateAllocatedBytes = 0;
        long managedBefore = 0;
        long managedAfterFrames = 0;
        long managedAfterShutdown = 0;

        long gen0Collections = 0;
        long gen1Collections = 0;
        long gen2Collections = 0;

        try
        {
            var lifecycleWatch = Stopwatch.StartNew();

            initialize();
            initialized = true;

            lifecycleWatch.Stop();
            initializationMs =
                lifecycleWatch.Elapsed.TotalMilliseconds;

            // Прогрев до начала измерений.
            for (var frame = 0; frame < warmupFrames; frame++)
            {
                update();

                for (var i = 0; i < fixedUpdatesPerFrame; i++)
                {
                    fixedUpdate();
                }
            }

            CollectGarbage();

            managedBefore = GC.GetTotalMemory(
                forceFullCollection: false);

            var allocatedBefore =
                GC.GetTotalAllocatedBytes(precise: true);

            var gen0Before = GC.CollectionCount(0);
            var gen1Before = GC.CollectionCount(1);
            var gen2Before = GC.CollectionCount(2);

            // Измеряем только рабочий участок.
            for (var frame = 0; frame < measuredFrames; frame++)
            {
                var frameStart = Stopwatch.GetTimestamp();

                var updateAllocatedBefore =
                    GC.GetTotalAllocatedBytes(precise: true);

                var updateStart = Stopwatch.GetTimestamp();

                update();

                updateTimes[frame] =
                    Stopwatch.GetElapsedTime(updateStart)
                        .TotalMilliseconds;

                updateAllocatedBytes +=
                    GC.GetTotalAllocatedBytes(precise: true)
                    - updateAllocatedBefore;

                var fixedAllocatedBefore =
                    GC.GetTotalAllocatedBytes(precise: true);

                var fixedStart = Stopwatch.GetTimestamp();

                for (var i = 0; i < fixedUpdatesPerFrame; i++)
                {
                    fixedUpdate();
                }

                fixedUpdateTimes[frame] =
                    Stopwatch.GetElapsedTime(fixedStart)
                        .TotalMilliseconds;

                fixedUpdateAllocatedBytes +=
                    GC.GetTotalAllocatedBytes(precise: true)
                    - fixedAllocatedBefore;

                frameTimes[frame] =
                    Stopwatch.GetElapsedTime(frameStart)
                        .TotalMilliseconds;
            }

            allocatedBytes =
                GC.GetTotalAllocatedBytes(precise: true)
                - allocatedBefore;

            gen0Collections =
                GC.CollectionCount(0) - gen0Before;

            gen1Collections =
                GC.CollectionCount(1) - gen1Before;

            gen2Collections =
                GC.CollectionCount(2) - gen2Before;

            // Снимаем показатели удерживаемой памяти
            // после измерений, пока сценарий ещё работает.
            CollectGarbage();

            managedAfterFrames = GC.GetTotalMemory(
                forceFullCollection: false);
        }
        finally
        {
            if (initialized)
            {
                var lifecycleWatch = Stopwatch.StartNew();

                shutdown();

                lifecycleWatch.Stop();
                shutdownMs =
                    lifecycleWatch.Elapsed.TotalMilliseconds;

                CollectGarbage();

                managedAfterShutdown = GC.GetTotalMemory(
                    forceFullCollection: false);
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Game Benchmark: {scenarioName}");
        Console.WriteLine($"  Warmup frames: {warmupFrames}");
        Console.WriteLine($"  Measured frames: {measuredFrames}");
        Console.WriteLine(
            $"  Fixed updates per frame: {fixedUpdatesPerFrame}");

        Console.WriteLine(
            $"  Setup + initialization: {initializationMs:F3} ms");

        PrintStatistics("Update", updateTimes);

        PrintStatistics(
            "FixedUpdate batch per frame",
            fixedUpdateTimes);

        PrintStatistics("Frame workload", frameTimes);

        Console.WriteLine(
            $"  Allocated: {allocatedBytes:N0} bytes");

        Console.WriteLine(
            $"  Allocated per frame: " +
            $"{allocatedBytes / (double)measuredFrames:N1} bytes");

        Console.WriteLine(
    $"  Update allocated: {updateAllocatedBytes:N0} bytes " +
    $"({updateAllocatedBytes / (double)measuredFrames:N1} bytes/frame)");

        Console.WriteLine(
            $"  FixedUpdate allocated: {fixedUpdateAllocatedBytes:N0} bytes " +
            $"({fixedUpdateAllocatedBytes / (double)measuredFrames:N1} bytes/frame)");

        Console.WriteLine(
            $"  GC collections: Gen0={gen0Collections}, " +
            $"Gen1={gen1Collections}, Gen2={gen2Collections}");

        Console.WriteLine(
            $"  Managed delta while active: " +
            $"{managedAfterFrames - managedBefore:N0} bytes");

        Console.WriteLine(
            $"  Managed delta after shutdown: " +
            $"{managedAfterShutdown - managedBefore:N0} bytes");

        Console.WriteLine(
            $"  Shutdown: {shutdownMs:F3} ms");

        Console.WriteLine();
    }

    private static void PrintStatistics(
        string name,
        double[] samples)
    {
        Console.WriteLine(name);

        Console.WriteLine(
            $"  Median: {Median(samples):F3} ms");

        Console.WriteLine(
            $"  P95: {Percentile(samples, 0.95):F3} ms");

        Console.WriteLine(
            $"  Max: {samples.Max():F3} ms");
    }

    private static double Median(double[] values)
    {
        var sorted = (double[])values.Clone();
        Array.Sort(sorted);

        var middle = sorted.Length / 2;

        if (sorted.Length % 2 != 0)
        {
            return sorted[middle];
        }

        return (sorted[middle - 1] + sorted[middle]) / 2.0;
    }

    private static double Percentile(
        double[] values,
        double percentile)
    {
        var sorted = (double[])values.Clone();
        Array.Sort(sorted);

        var index = (int)Math.Ceiling(
            percentile * sorted.Length) - 1;

        return sorted[Math.Clamp(
            index,
            0,
            sorted.Length - 1)];
    }

    private static void CollectGarbage()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
