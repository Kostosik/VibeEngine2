
using System.Diagnostics;
using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Physics.Collision;
using Engine.Physics.Components;
using Engine.Physics.Shapes;

namespace Engine.Benchmarks;

internal static class PhysicsSolverBenchmark
{
    private const int WarmupCount = 3;
    private const int SampleCount = 9;

    private static readonly int[] ContactCounts =
    [
        100,
        500,
        1_000,
        2_500
    ];

    private static readonly Fixed32 Penetration =
        Fixed32.FromFloat(0.25f);

    private static readonly Fixed32 PenetrationSlop =
        Fixed32.FromFloat(0.01f);

    private static readonly Fixed32 CorrectionPercent =
        Fixed32.FromFloat(0.8f);

    private static readonly FixedVector2 Normal =
        new(Fixed32.One, Fixed32.Zero);

    public static void Run()
    {
        Console.WriteLine();
        Console.WriteLine("Physics Solver Benchmark");
        Console.WriteLine($"Runtime: {Environment.Version}");
        Console.WriteLine($"Processors: {Environment.ProcessorCount}");
        Console.WriteLine($"Samples: {SampleCount}");

        foreach (var count in ContactCounts)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"Contacts: {count:N0} | Bodies: {count * 2:N0}");

            var scenario = CreateScenario(count);

            using var world = scenario.World;

            MeasurePositionSolver(scenario);
            MeasureVelocitySolver(scenario);
        }

        Console.WriteLine();
        Console.WriteLine("Physics solver benchmark complete.");
    }

    private static void MeasurePositionSolver(
        SolverScenario scenario)
    {
        var resolver = new CollisionResolver2D();

        var times = new double[SampleCount];
        var allocations = new long[SampleCount];

        for (var warmup = 0; warmup < WarmupCount; warmup++)
        {
            ResetPositions(scenario);

            foreach (var manifold in scenario.Manifolds)
            {
                resolver.TryResolvePosition(
                    scenario.World,
                    manifold,
                    PenetrationSlop,
                    CorrectionPercent);
            }
        }

        for (var sample = 0; sample < SampleCount; sample++)
        {
            ResetPositions(scenario);

            var allocatedBefore =
                GC.GetAllocatedBytesForCurrentThread();

            var started = Stopwatch.GetTimestamp();

            foreach (var manifold in scenario.Manifolds)
            {
                resolver.TryResolvePosition(
                    scenario.World,
                    manifold,
                    PenetrationSlop,
                    CorrectionPercent);
            }

            times[sample] =
                Stopwatch.GetElapsedTime(started).TotalMilliseconds;

            allocations[sample] =
                GC.GetAllocatedBytesForCurrentThread()
                - allocatedBefore;
        }

        PrintResult(
            "Position Solver",
            scenario.Manifolds.Length,
            times,
            allocations);
    }

    private static void MeasureVelocitySolver(
        SolverScenario scenario)
    {
        var resolver = new CollisionResolver2D();

        var times = new double[SampleCount];
        var allocations = new long[SampleCount];

        for (var warmup = 0; warmup < WarmupCount; warmup++)
        {
            ResetVelocities(scenario);

            foreach (var manifold in scenario.Manifolds)
            {
                resolver.ResolveVelocity(
                    scenario.World,
                    manifold);
            }
        }

        for (var sample = 0; sample < SampleCount; sample++)
        {
            ResetVelocities(scenario);

            var allocatedBefore =
                GC.GetAllocatedBytesForCurrentThread();

            var started = Stopwatch.GetTimestamp();

            foreach (var manifold in scenario.Manifolds)
            {
                resolver.ResolveVelocity(
                    scenario.World,
                    manifold);
            }

            times[sample] =
                Stopwatch.GetElapsedTime(started).TotalMilliseconds;

            allocations[sample] =
                GC.GetAllocatedBytesForCurrentThread()
                - allocatedBefore;
        }

        PrintResult(
            "Velocity Solver",
            scenario.Manifolds.Length,
            times,
            allocations);
    }

    private static SolverScenario CreateScenario(int count)
    {
        var world = new World();

        try
        {
            var collider = new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.FromInt(2),
                        Fixed32.FromInt(2))));

            var firstEntities = new EntityId[count];
            var secondEntities = new EntityId[count];

            var firstPositions = new FixedVector2[count];
            var secondPositions = new FixedVector2[count];

            var manifolds = new CollisionManifold[count];

            for (var i = 0; i < count; i++)
            {
                var x = Fixed32.FromInt(i * 4);

                var firstPosition =
                    new FixedVector2(x, Fixed32.Zero);

                var secondPosition =
                    firstPosition +
                    new FixedVector2(
                        Fixed32.FromFloat(1.5f),
                        Fixed32.Zero);

                var first = world.CreateEntity();
                var second = world.CreateEntity();

                world.Add(first, PhysicsBody2D.Dynamic(Fixed32.One));
                world.Add(second, PhysicsBody2D.Dynamic(Fixed32.One));

                world.Add(first, new WorldTransform2D(firstPosition));
                world.Add(second, new WorldTransform2D(secondPosition));

                world.Add(first, collider);
                world.Add(second, collider);

                firstEntities[i] = first;
                secondEntities[i] = second;

                firstPositions[i] = firstPosition;
                secondPositions[i] = secondPosition;

                var contactPosition =
                    firstPosition +
                    new FixedVector2(
                        Fixed32.FromFloat(0.75f),
                        Fixed32.Zero);

                manifolds[i] = new CollisionManifold(
                    new CollisionPair(first, second),
                    new ContactPoint(
                        contactPosition,
                        Normal,
                        Penetration));
            }

            return new SolverScenario(
                world,
                firstEntities,
                secondEntities,
                firstPositions,
                secondPositions,
                manifolds);
        }
        catch
        {
            world.Dispose();
            throw;
        }
    }

    private static void ResetPositions(
        SolverScenario scenario)
    {
        for (var i = 0; i < scenario.Manifolds.Length; i++)
        {
            ref var first =
                ref scenario.World.Get<WorldTransform2D>(
                    scenario.FirstEntities[i]);

            ref var second =
                ref scenario.World.Get<WorldTransform2D>(
                    scenario.SecondEntities[i]);

            first.Position = scenario.FirstPositions[i];
            second.Position = scenario.SecondPositions[i];
        }
    }

    private static void ResetVelocities(
        SolverScenario scenario)
    {
        var firstVelocity = Normal;
        var secondVelocity = -Normal;

        for (var i = 0; i < scenario.Manifolds.Length; i++)
        {
            ref var first =
                ref scenario.World.Get<PhysicsBody2D>(
                    scenario.FirstEntities[i]);

            ref var second =
                ref scenario.World.Get<PhysicsBody2D>(
                    scenario.SecondEntities[i]);

            first.Velocity = firstVelocity;
            second.Velocity = secondVelocity;
        }
    }

    private static void PrintResult(
        string name,
        int contactCount,
        double[] times,
        long[] allocations)
    {
        Array.Sort(times);
        Array.Sort(allocations);

        var median = times[SampleCount / 2];

        var nanosecondsPerContact =
            median * 1_000_000.0 / contactCount;

        Console.WriteLine(
            $"  {name,-20} " +
            $"median={median,8:F4} ms, " +
            $"ns/contact={nanosecondsPerContact,8:F1}, " +
            $"alloc={allocations[SampleCount / 2],8:N0} B");
    }

    private sealed record SolverScenario(
        World World,
        EntityId[] FirstEntities,
        EntityId[] SecondEntities,
        FixedVector2[] FirstPositions,
        FixedVector2[] SecondPositions,
        CollisionManifold[] Manifolds);
}
