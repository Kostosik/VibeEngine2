using System.Diagnostics;
using Engine.Core.Math;
using Engine.ECS.Entities;
using Engine.Physics.BroadPhase;
using Engine.Physics.Collision;
using Engine.Physics.Components;
using Engine.Physics.Shapes;

namespace Engine.Benchmarks;

internal static class PhysicsBroadPhaseBenchmark
{
    private const int SampleCount = 5;
    private const int ColliderSpacing = 4;

    private static readonly int[] ColliderCounts =
    [
        100,
        500,
        1_000,
        2_500,
        5_000
    ];

    public static void Run()
    {
        Console.WriteLine("Physics Broad Phase Benchmark");
        Console.WriteLine($"Runtime: {Environment.Version}");
        Console.WriteLine($"Processors: {Environment.ProcessorCount}");
        Console.WriteLine(
            "Scene: sparse grid, 1x1 AABB colliders, spacing 4");
        Console.WriteLine(
            $"Samples per implementation: {SampleCount}");

        RunScene("Sparse", 4.0f);
        RunScene("Overlapping", 0.75f);

        Console.WriteLine();
        Console.WriteLine("Physics benchmark complete.");
    }

    private static void RunScene(
    string sceneName,
    float spacing)
    {
        Console.WriteLine();
        Console.WriteLine(
            $"Scene: {sceneName}, spacing={spacing}");

        foreach (var colliderCount in ColliderCounts)
        {
            var colliders = CreateColliders(
                colliderCount,
                spacing);

            Console.WriteLine();
            Console.WriteLine($"Colliders: {colliderCount}");

            Measure(
                "BruteForce",
                new BruteForceBroadPhase(),
                colliders);

            Measure(
                "SpatialHash",
                new SpatialHashBroadPhase(Fixed32.One),
                colliders);
        }
    }

    private static PhysicsColliderProxy[] CreateColliders(
        int count,
        float spacing)
    {
        var collider = new Collider2D(
            new AabbShape2D(
                new FixedVector2(
                    Fixed32.One,
                    Fixed32.One)));

        var colliders = new PhysicsColliderProxy[count];
        var columns = (int)Math.Ceiling(Math.Sqrt(count));

        for (var i = 0; i < count; i++)
        {
            var position = new FixedVector2(
                Fixed32.FromFloat((i % columns) * spacing),
                Fixed32.FromFloat((i / columns) * spacing));

            colliders[i] = new PhysicsColliderProxy(
                new EntityId((uint)(i + 1), 1),
                collider.GetWorldBounds(position),
                collider);
        }

        return colliders;
    }

    private static void Measure(
        string name,
        IPhysicsBroadPhase broadPhase,
        PhysicsColliderProxy[] colliders)
    {
        var pairs = new List<CollisionPair>();

        // Прогрев: первичные накладные расходы не попадут в замеры.
        broadPhase.FindPairs(colliders, pairs);

        var timesMs = new double[SampleCount];
        var allocatedBytes = new long[SampleCount];

        var pairCount = 0;

        for (var sample = 0; sample < SampleCount; sample++)
        {
            var allocatedBefore =
                GC.GetAllocatedBytesForCurrentThread();

            var started = Stopwatch.GetTimestamp();

            broadPhase.FindPairs(colliders, pairs);

            timesMs[sample] =
                Stopwatch.GetElapsedTime(started)
                    .TotalMilliseconds;

            allocatedBytes[sample] =
                GC.GetAllocatedBytesForCurrentThread()
                - allocatedBefore;

            pairCount = pairs.Count;
        }

        Array.Sort(timesMs);
        Array.Sort(allocatedBytes);

        Console.WriteLine(
            $"  {name,-12} " +
            $"median={timesMs[SampleCount / 2],8:F3} ms, " +
            $"alloc={allocatedBytes[SampleCount / 2],8:N0} B/call, " +
            $"pairs={pairCount}");
    }
}