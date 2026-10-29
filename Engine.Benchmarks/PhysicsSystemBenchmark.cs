using System.Diagnostics;
using Engine.Core.Events;
using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.Physics;
using Engine.Physics.BroadPhase;
using Engine.Physics.Components;
using Engine.Physics.Shapes;

namespace Engine.Benchmarks;

internal static class PhysicsSystemBenchmark
{
    private const int WarmupSteps = 10;
    private const int SampleCount = 15;

    private static readonly int[] BodyCounts =
    [
        100,
        500,
        1_000,
        2_500,
        5_000
    ];

    private static readonly Fixed32 Delta =
        Fixed32.FromRatio(1, 60);

    public static void Run()
    {
        Console.WriteLine();
        Console.WriteLine("Full PhysicsSystem Benchmark");
        Console.WriteLine(
            "Scene: sparse grid, dynamic AABB bodies, no contacts");

        foreach (var bodyCount in BodyCounts)
        {
            Console.WriteLine();
            Console.WriteLine($"Bodies: {bodyCount}");

            // Полный перебор оставляем только для небольших сцен:
            // на больших размерах его стоимость уже измерена
            // отдельным broad-phase benchmark.
            if (bodyCount <= 1_000)
            {
                Measure(
                    bodyCount,
                    "BruteForce",
                    new BruteForceBroadPhase());
            }
            else
            {
                Console.WriteLine(
                    "  BruteForce skipped (quadratic cost).");
            }

            Measure(
                bodyCount,
                "SpatialHash",
                new SpatialHashBroadPhase(Fixed32.One));
        }

        Console.WriteLine();
        Console.WriteLine("Dense Collision Benchmark");
        Console.WriteLine("Static AABB bodies, spacing=0.75");

        foreach (var bodyCount in new[] { 100, 250, 500, 1_000 })
        {
            Measure(
                bodyCount,
                "SpatialHash",
                new SpatialHashBroadPhase(Fixed32.One),
                dense: true);
        }

        Console.WriteLine();
        Console.WriteLine("Dynamic Contact Benchmark");
        Console.WriteLine(
            "Dynamic AABB bodies resting on static supports");

        foreach (var bodyCount in new[] { 100, 250, 500, 1_000 })
        {
            MeasureGroundedBodies(bodyCount);
        }

        Console.WriteLine();
        Console.WriteLine(
            "Dynamic Solver Diagnostic (1,000 dynamic + 1,000 static)");

        MeasureGroundedBodies(
            1_000,
            substeps: 4,
            positionIterations: 2,
            label: "Default");

        MeasureGroundedBodies(
            1_000,
            substeps: 1,
            positionIterations: 2,
            label: "1 substep");

        MeasureGroundedBodies(
            1_000,
            substeps: 4,
            positionIterations: 1,
            label: "1 position iter");

        Console.WriteLine();
        Console.WriteLine("Awake Contact Benchmark");
        Console.WriteLine(
            "Dynamic bodies are forced awake before each step");

        MeasureGroundedBodies(
            100,
            label: "Awake",
            keepAwake: true);

        MeasureGroundedBodies(
            250,
            label: "Awake",
            keepAwake: true);

        MeasureGroundedBodies(
            500,
            label: "Awake",
            keepAwake: true);

        MeasureGroundedBodies(
            1_000,
            label: "Awake",
            keepAwake: true);

        Console.WriteLine();
        Console.WriteLine(
            "Awake Solver Diagnostic (1,000 dynamic + 1,000 static)");

        MeasureGroundedBodies(
            1_000,
            substeps: 1,
            positionIterations: 2,
            label: "1 substep",
            keepAwake: true);

        MeasureGroundedBodies(
            1_000,
            substeps: 4,
            positionIterations: 1,
            label: "1 position iter",
            keepAwake: true);

        Console.WriteLine();
        Console.WriteLine("Full physics benchmark complete.");
    }

    private static void MeasureGroundedBodies(
        int bodyCount,
        int substeps = 4,
        int positionIterations = 2,
        string label = "Default",
        bool keepAwake = false)
    {
        using var world = new World();

        var collider = new Collider2D(
            new AabbShape2D(
                new FixedVector2(
                    Fixed32.One,
                    Fixed32.One)));

        for (var i = 0; i < bodyCount; i++)
        {
            var x = Fixed32.FromInt(i * 4);

            var support = world.CreateEntity();

            world.Add(
                support,
                new WorldTransform2D(
                    new FixedVector2(x, Fixed32.Zero)));

            world.Add(support, PhysicsBody2D.Static());
            world.Add(support, collider);

            var dynamicBody = world.CreateEntity();

            world.Add(
                dynamicBody,
                new WorldTransform2D(
                    new FixedVector2(
                        x,
                        Fixed32.FromFloat(0.9f))));

            world.Add(
                dynamicBody,
                PhysicsBody2D.Dynamic(Fixed32.One));

            world.Add(dynamicBody, collider);
        }

        var events = new EventBus();

        var physics = new PhysicsSystem2D(
            world,
            new PhysicsSettings2D
            {
                Gravity = new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromFloat(-9.8f)),
                Substeps = substeps,
                PositionIterations = positionIterations
            },
            events,
            new SpatialHashBroadPhase(Fixed32.One));

        ulong tick = 0;

        void Step()
        {
            tick++;

            if (keepAwake)
            {
                foreach (var item in world.Query<PhysicsBody2D>())
                {
                    ref var body = ref item.Component;

                    if (body.BodyType == PhysicsBodyType.Dynamic)
                    {
                        body.WakeUp();
                    }
                }
            }

            physics.FixedUpdate(
                new FixedSystemContext(
                    new SimulationTime(
                        Delta,
                        new Tick(tick))));

            events.DispatchPending();
        }

        const int groundedWarmupSteps = 45;

        for (var i = 0; i < groundedWarmupSteps; i++)
        {
            Step();
        }

        var sleepingBodies = 0;

        foreach (var item in world.Query<PhysicsBody2D>())
        {
            if (item.Component.IsSleeping)
            {
                sleepingBodies++;
            }
        }

        Console.WriteLine(
            $"  Before measurement: sleeping={sleepingBodies}/{bodyCount}");

        var times = new double[SampleCount];
        var allocations = new long[SampleCount];

        for (var i = 0; i < SampleCount; i++)
        {
            var allocatedBefore =
                GC.GetAllocatedBytesForCurrentThread();

            var started = Stopwatch.GetTimestamp();

            Step();

            times[i] =
                Stopwatch.GetElapsedTime(started)
                    .TotalMilliseconds;

            allocations[i] =
                GC.GetAllocatedBytesForCurrentThread()
                - allocatedBefore;
        }

        Array.Sort(times);
        Array.Sort(allocations);

        Console.WriteLine(
            $"  {label,-15} " +
            $"substeps={substeps}, posIter={positionIterations}, " +
            $"median={times[SampleCount / 2],8:F3} ms/step, " +
            $"alloc={allocations[SampleCount / 2],8:N0} B/step, " +
            $"contacts={physics.Contacts.Count,6:N0}, " +
            $"events={physics.ContactEvents.Count,6:N0}");
    }

    private static void Measure(
        int bodyCount,
        string name,
        IPhysicsBroadPhase broadPhase, bool dense = false)
    {

        using var world = new World();

        var collider = new Collider2D(
            new AabbShape2D(
                new FixedVector2(
                    Fixed32.One,
                    Fixed32.One)));

        var columns = (int)Math.Ceiling(
            Math.Sqrt(bodyCount));

        for (var i = 0; i < bodyCount; i++)
        {
            var position = dense
                ? new FixedVector2(
                    Fixed32.FromRatio(
                        3 * (i % columns),
                        4),
                    Fixed32.FromRatio(
                        3 * (i / columns),
                        4))
                : new FixedVector2(
                    Fixed32.FromInt((i % columns) * 4),
                    Fixed32.FromInt((i / columns) * 4));

            var entity = world.CreateEntity();

            world.Add(
                entity,
                new WorldTransform2D(position));

            if (dense)
            {
                world.Add(
                    entity,
                    PhysicsBody2D.Static());
            }
            else
            {
                var body = PhysicsBody2D.Dynamic(
                    Fixed32.One);

                // Не даём телам уснуть в разреженном сценарии.
                body.Velocity = new FixedVector2(
                    Fixed32.FromFloat(0.1f),
                    Fixed32.Zero);

                world.Add(entity, body);
            }

            world.Add(entity, collider);
        }

        var events = new EventBus();

        var physics = new PhysicsSystem2D(
            world,
            new PhysicsSettings2D
            {
                Gravity = FixedVector2.Zero
            },
            events,
            broadPhase);

        ulong tick = 0;

        void Step()
        {
            tick++;

            physics.FixedUpdate(
                new FixedSystemContext(
                    new SimulationTime(
                        Delta,
                        new Tick(tick))));

            events.DispatchPending();
        }

        for (var i = 0; i < WarmupSteps; i++)
        {
            Step();
        }

        var times = new double[SampleCount];
        var allocations = new long[SampleCount];

        for (var i = 0; i < SampleCount; i++)
        {
            var allocatedBefore =
                GC.GetAllocatedBytesForCurrentThread();

            var started = Stopwatch.GetTimestamp();

            Step();

            times[i] =
                Stopwatch.GetElapsedTime(started)
                    .TotalMilliseconds;

            allocations[i] =
                GC.GetAllocatedBytesForCurrentThread()
                - allocatedBefore;
        }

        Array.Sort(times);
        Array.Sort(allocations);

        Console.WriteLine(
            $"  {name,-12} " +
            $"median={times[SampleCount / 2],8:F3} ms/step, " +
            $"alloc={allocations[SampleCount / 2],8:N0} B/step, " +
            $"contacts={physics.Contacts.Count,6:N0}, " +
            $"events={physics.ContactEvents.Count,6:N0}");
    }
}