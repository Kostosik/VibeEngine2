
using System.Diagnostics;
using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Physics.Components;
using Engine.Physics.Queries;
using Engine.Physics.Shapes;

namespace Engine.Benchmarks;

internal static class PhysicsQuery2DBenchmark
{
    private const int SampleCount = 5;
    private const int CallsPerSample = 20;
    private const int WarmupCalls = 10;
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
        Console.WriteLine("Physics Query 2D Benchmark");
        Console.WriteLine(
            "Scene: sparse grid, 1x1 AABB colliders");
        Console.WriteLine(
            $"Samples: {SampleCount}, calls per sample: {CallsPerSample}");
        Console.WriteLine(
            "Colliders have Collider2D and WorldTransform2D, without PhysicsBody2D.");

        foreach (var colliderCount in ColliderCounts)
        {
            using var world = CreateWorld(
                colliderCount,
                out var movingEntity);

            var query = new PhysicsQuery2D(world);

            var pointResults = new List<EntityId>();
            var boundsResults = new List<EntityId>();

            var queryBounds = new FixedBounds2(
                new FixedVector2(
                    Fixed32.FromInt(-1),
                    Fixed32.FromInt(-1)),
                new FixedVector2(
                    Fixed32.One,
                    Fixed32.One));

            var columns = (int)Math.Ceiling(
                Math.Sqrt(colliderCount));

            var rayOrigin = new FixedVector2(
                Fixed32.FromInt(-10),
                Fixed32.Zero);

            var rayDirection = new FixedVector2(
                Fixed32.One,
                Fixed32.Zero);

            var rayDistance = Fixed32.FromInt(
                columns * ColliderSpacing + 20);

            // Прогрев и измерение начинаются после создания мира.
            Console.WriteLine();
            Console.WriteLine($"Colliders: {colliderCount}");

            Measure(
                "Raycast",
                () =>
                {
                    query.Raycast(
                        rayOrigin,
                        rayDirection,
                        rayDistance,
                        out _);
                });

            Measure(
                "OverlapPoint",
                () =>
                {
                    query.OverlapPoint(
                        FixedVector2.Zero,
                        pointResults);
                });

            var moveAway = false;

            void MoveIndexedCollider()
            {
                ref var transform =
                    ref world.Get<WorldTransform2D>(movingEntity);

                transform.Position = moveAway
                    ? FixedVector2.Zero
                    : new FixedVector2(
                        Fixed32.FromInt(ColliderSpacing),
                        Fixed32.Zero);

                moveAway = !moveAway;
            }

            Measure(
                "OverlapPoint (rebuild)",
                () =>
                {
                    query.OverlapPoint(
                        FixedVector2.Zero,
                        pointResults);
                },
                beforeSample: MoveIndexedCollider);

            Measure(
                "OverlapBounds",
                () =>
                {
                    query.OverlapBounds(
                        queryBounds,
                        boundsResults);
                });
        }

        Console.WriteLine();
        Console.WriteLine("Physics query benchmark complete.");
    }

    private static World CreateWorld(
        int colliderCount,
        out EntityId movingEntity)
    {
        var world = new World();
        movingEntity = default;

        try
        {
            var columns = (int)Math.Ceiling(
                Math.Sqrt(colliderCount));

            for (var i = 0; i < colliderCount; i++)
            {
                var entity = world.CreateEntity();

                if (i == 0)
                {
                    movingEntity = entity;
                }

                var position = new FixedVector2(
                    Fixed32.FromInt(
                        (i % columns) * ColliderSpacing),
                    Fixed32.FromInt(
                        (i / columns) * ColliderSpacing));

                world.Add(
                    entity,
                    new WorldTransform2D(position));

                world.Add(
                    entity,
                    new Collider2D(
                        new AabbShape2D(
                            new FixedVector2(
                                Fixed32.One,
                                Fixed32.One))));
            }

            return world;
        }
        catch
        {
            world.Dispose();
            throw;
        }
    }

    private static void Measure(
        string name,
        Action action,
        Action? beforeSample = null)
    {
        for (var i = 0; i < WarmupCalls; i++)
        {
            action();
        }

        var timesMs = new double[SampleCount];
        var allocations = new long[SampleCount];

        for (var sample = 0; sample < SampleCount; sample++)
        {
            var allocatedBefore =
                GC.GetAllocatedBytesForCurrentThread();

            var started = Stopwatch.GetTimestamp();

            beforeSample?.Invoke();

            for (var call = 0; call < CallsPerSample; call++)
            {
                action();
            }

            var elapsed = Stopwatch.GetElapsedTime(started);

            var allocated =
                GC.GetAllocatedBytesForCurrentThread()
                - allocatedBefore;

            timesMs[sample] =
                elapsed.TotalMilliseconds / CallsPerSample;

            allocations[sample] =
                allocated / CallsPerSample;
        }

        Array.Sort(timesMs);
        Array.Sort(allocations);

        Console.WriteLine(
            $"  {name,-16} " +
            $"median={timesMs[SampleCount / 2],8:F3} ms/call, " +
            $"alloc={allocations[SampleCount / 2],8:N0} B/call");
    }
}
