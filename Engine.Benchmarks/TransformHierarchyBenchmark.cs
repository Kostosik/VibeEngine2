
using System.Diagnostics;
using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.ECS.Systems;
using Engine.Transform;

namespace Engine.Benchmarks;

internal static class TransformHierarchyBenchmark
{
    private const int WarmupIterations = 100;
    private const int SampleCount = 9;
    private const int OperationsPerSample = 25;
    private static readonly int[] RootCounts =
    [
        5_000,
    1_000,
    500,
    100
    ];

    private static readonly int[] ChainCounts =
    [
        500,
    250,
    100
    ];

    public static void Run()
    {
        Console.WriteLine();
        Console.WriteLine("Transform Hierarchy Benchmark");
        Console.WriteLine(
            "Measures FixedUpdate time and managed allocations.");

        Console.WriteLine();
        Console.WriteLine("Independent root transforms");

        foreach (var count in RootCounts)
        {
            Measure(count, deepChain: false);
        }

        Console.WriteLine();
        Console.WriteLine("Deep transform hierarchy");

        foreach (var count in ChainCounts)
        {
            Measure(count, deepChain: true);
        }

        Console.WriteLine();
        Console.WriteLine("Transform hierarchy benchmark complete.");
    }

    private static void Measure(
        int count,
        bool deepChain)
    {
        using var world = new World();

        var entities = new EntityId[count];

        // Подготовка сущностей не входит в измерение.
        for (var i = 0; i < count; i++)
        {
            entities[i] = world.CreateEntity();

            world.Add(
                entities[i],
                new WorldTransform2D(FixedVector2.Zero));
        }

        // Добавляем LocalTransform в обратном порядке.
        // Для цепочки это заставляет первый Resolve пройти
        // по родителям, а не использовать уже вычисленные значения.
        for (var i = count - 1; i >= 0; i--)
        {
            world.Add(
                entities[i],
                new LocalTransform2D(
                    new FixedTransform2D(
                        new FixedVector2(
                            Fixed32.One,
                            Fixed32.Zero))));

            if (deepChain && i > 0)
            {
                world.Add(
                    entities[i],
                    new TransformParent2D(entities[i - 1]));
            }
        }

        var actualCount = 0;

        foreach (var item in world.QueryReadOnly<LocalTransform2D>())
        {
            actualCount++;
        }

        if (actualCount != count)
        {
            throw new InvalidOperationException(
                $"Expected {count} local transforms, found {actualCount}.");
        }

        var system = new TransformHierarchySystem(world);

        void RunStep()
        {
            system.FixedUpdate(default);
        }

        for (var i = 0; i < WarmupIterations; i++)
        {
            RunStep();
        }

        var times = new double[SampleCount];
        var allocations = new long[SampleCount];

        for (var i = 0; i < SampleCount; i++)
        {
            var allocatedBefore =
                GC.GetAllocatedBytesForCurrentThread();

            var started = Stopwatch.GetTimestamp();

            for (var operation = 0;
                 operation < OperationsPerSample;
                 operation++)
            {
                RunStep();
            }

            times[i] =
                Stopwatch.GetElapsedTime(started).TotalMilliseconds
                / OperationsPerSample;

            allocations[i] =
                (GC.GetAllocatedBytesForCurrentThread()
                 - allocatedBefore)
                / OperationsPerSample;
        }

        Array.Sort(times);
        Array.Sort(allocations);

        var scenario = deepChain ? "Chain" : "Roots";

        for (var i = 0; i < count; i++)
        {
            var expectedX = Fixed32.FromInt(
                deepChain ? i + 1 : 1);

            var actualX = world
                .GetReadOnly<WorldTransform2D>(entities[i])
                .Position.X;

            if (actualX != expectedX)
            {
                throw new InvalidOperationException(
                    $"Transform mismatch: scenario={scenario}, " +
                    $"entity={i}, expected X={expectedX}, " +
                    $"actual X={actualX}.");
            }
        }

        Console.WriteLine(
            $"  {scenario,-6} " +
            $"entities={count,5:N0} " +
            $"median={times[SampleCount / 2],8:F3} ms/tick " +
            $"alloc={allocations[SampleCount / 2],8:N0} B/tick");
    }
}
