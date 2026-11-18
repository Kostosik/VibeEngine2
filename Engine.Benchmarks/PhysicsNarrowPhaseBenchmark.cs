
using Engine.Core.Math;
using Engine.ECS.Entities;
using Engine.Physics.BroadPhase;
using Engine.Physics.Collision;
using Engine.Physics.Components;
using Engine.Physics.Shapes;
using System.Diagnostics;
using System.Drawing;

namespace Engine.Benchmarks;

internal static class PhysicsNarrowPhaseBenchmark
{
    private const int WarmupCount = 3;
    private const int SampleCount = 9;

    private static readonly int[] PairCounts =
    [
        100,
        1_000,
        5_000
    ];

    private static readonly FixedVector2 SquareSize =
        new(Fixed32.FromInt(2), Fixed32.FromInt(2));

    private static readonly FixedVector2[] SquareVertices =
    [
        new(-Fixed32.One, -Fixed32.One),
        new(Fixed32.One, -Fixed32.One),
        new(Fixed32.One, Fixed32.One),
        new(-Fixed32.One, Fixed32.One)
    ];

    public static void Run()
    {
        Console.WriteLine();
        Console.WriteLine("Physics Narrow Phase Benchmark");
        Console.WriteLine($"Runtime: {Environment.Version}");
        Console.WriteLine($"Processors: {Environment.ProcessorCount}");
        Console.WriteLine($"Samples: {SampleCount}");

        RunShapeCase(
            "AABB / AABB",
            CreateAabb(),
            CreateAabb());

        RunShapeCase(
            "Circle / Circle",
            CreateCircle(),
            CreateCircle());

        RunShapeCase(
            "Circle / AABB",
            CreateCircle(),
            CreateAabb());

        RunShapeCase(
            "Polygon / AABB",
            CreatePolygon(),
            CreateAabb());

        RunShapeCase(
            "Circle / Polygon",
            CreateCircle(),
            CreatePolygon());

        RunShapeCase(
            "Polygon / Polygon",
            CreatePolygon(),
            CreatePolygon());

        Console.WriteLine();
        Console.WriteLine("Narrow phase benchmark complete.");
    }

    private static void RunShapeCase(
        string name,
        Collider2D firstCollider,
        Collider2D secondCollider)
    {
        Console.WriteLine();
        Console.WriteLine(name);

        foreach (var count in PairCounts)
        {
            var overlapping = CreatePairs(
                count,
                firstCollider,
                secondCollider,
                Fixed32.FromFloat(1.25f));

            var separated = CreatePairs(
                count,
                firstCollider,
                secondCollider,
                Fixed32.FromFloat(3.5f));

            var rotated = CreatePairs(
                count,
                firstCollider,
                secondCollider,
                Fixed32.FromFloat(1.25f),
                Fixed32.Pi / Fixed32.FromInt(6));

            Measure(
                "Overlap 30deg",
                rotated.First,
                rotated.Second);

            Measure("Overlap", overlapping.First,
                overlapping.Second);

            Measure("Separated", separated.First,
                separated.Second);
        }
    }

    private static void Measure(
        string scenario,
        PhysicsColliderProxy[] first,
        PhysicsColliderProxy[] second)
    {
        var detector = new CollisionDetector2D();

        Execute(detector, first, second, out _);

        for (var i = 0; i < WarmupCount; i++)
        {
            Execute(detector, first, second, out _);
        }

        var times = new double[SampleCount];
        var allocations = new long[SampleCount];

        var hits = 0;
        long checksum = 0;

        for (var sample = 0; sample < SampleCount; sample++)
        {
            var allocatedBefore =
                GC.GetAllocatedBytesForCurrentThread();

            var started = Stopwatch.GetTimestamp();

            hits = Execute(
                detector,
                first,
                second,
                out checksum);

            times[sample] =
                Stopwatch.GetElapsedTime(started).TotalMilliseconds;

            allocations[sample] =
                GC.GetAllocatedBytesForCurrentThread()
                - allocatedBefore;
        }

        GC.KeepAlive(checksum);

        Array.Sort(times);
        Array.Sort(allocations);

        var median = times[SampleCount / 2];

        var nanosecondsPerPair =
            median * 1_000_000.0 / first.Length;

        Console.WriteLine(
            $"  {scenario,-12} " +
            $"pairs={first.Length,6:N0}, " +
            $"median={median,8:F4} ms, " +
            $"ns/pair={nanosecondsPerPair,8:F1}, " +
            $"alloc={allocations[SampleCount / 2],8:N0} B, " +
            $"hits={hits,6:N0}");
    }

    private static int Execute(
        CollisionDetector2D detector,
        PhysicsColliderProxy[] first,
        PhysicsColliderProxy[] second,
        out long checksum)
    {
        var hits = 0;
        checksum = 0;

        for (var i = 0; i < first.Length; i++)
        {
            if (!detector.TryDetect(
                    first[i],
                    second[i],
                    out var manifold))
            {
                continue;
            }

            hits++;

            checksum +=
                manifold.Contact.Penetration.RawValue;
        }

        return hits;
    }

    private static (
        PhysicsColliderProxy[] First,
        PhysicsColliderProxy[] Second) CreatePairs(
        int count,
        Collider2D firstCollider,
        Collider2D secondCollider,
        Fixed32 separation, Fixed32 rotation = default)
    {
        var first = new PhysicsColliderProxy[count];
        var second = new PhysicsColliderProxy[count];

        var columns = (int)Math.Ceiling(Math.Sqrt(count));

        for (var i = 0; i < count; i++)
        {
            var x = Fixed32.FromInt(
                (i % columns) * 8);

            var y = Fixed32.FromInt(
                (i / columns) * 8);

            var firstPosition =
                new FixedVector2(x, y);

            var secondPosition =
                firstPosition +
                new FixedVector2(separation, Fixed32.Zero);

            var entityIndex =
                checked((uint)(i * 2 + 1));

            first[i] = CreateProxy(
                new EntityId(entityIndex, 1),
                firstCollider,
                firstPosition);

            second[i] = CreateProxy(
                new EntityId(entityIndex + 1, 1),
                secondCollider,
                secondPosition);
        }

        return (first, second);
    }

    private static PhysicsColliderProxy CreateProxy(
        EntityId entity,
        Collider2D collider,
        FixedVector2 position)
    {
        var rotation = Fixed32.Zero;

        var hasPolygon =
    collider.Shape.Type == PhysicsShapeType.Polygon;

        var sine = hasPolygon
            ? Fixed32.Sin(rotation)
            : Fixed32.Zero;

        var cosine = hasPolygon
            ? Fixed32.Cos(rotation)
            : Fixed32.Zero;

        return new PhysicsColliderProxy(
            entity,
            collider.GetWorldBounds(position, rotation),
            collider)
        {
            WorldPosition = collider.GetWorldPosition(
                position,
                rotation),

            WorldRotation = rotation,
            WorldSine = sine,
            WorldCosine = cosine,
            HasCachedRotation = hasPolygon
        };
    }

    private static Collider2D CreateAabb()
    {
        return new Collider2D(
            new AabbShape2D(SquareSize));
    }

    private static Collider2D CreateCircle()
    {
        return new Collider2D(
            new CircleShape2D(Fixed32.One));
    }

    private static Collider2D CreatePolygon()
    {
        return new Collider2D(
            new PolygonShape2D(SquareVertices));
    }
}
