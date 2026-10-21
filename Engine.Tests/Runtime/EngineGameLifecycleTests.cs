
using Engine.Core.Events;
using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS.Entities;
using Engine.Runtime;

namespace Engine.Tests.Runtime;

public sealed class EngineGameLifecycleTests
{
    [Fact]
    public void Shutdown_IsIdempotent_AndRejectsFurtherGameLoopCalls()
    {
        using var runtime = CreateRuntime();
        var game = new LifecycleProbeGame(runtime);

        game.Initialize();

        game.FixedUpdate(CreateSimulationTime(1));

        Assert.Equal(
            1,
            runtime.EcsWorld
                .Get<ProbeComponent>(game.Entity)
                .ExecutionCount);

        game.Shutdown();

        Assert.True(runtime.IsShutdown);
        Assert.Equal(1, game.ShutdownCount);

        Assert.Throws<InvalidOperationException>(
            () => game.Update(default));

        Assert.Throws<InvalidOperationException>(
            () => game.FixedUpdate(CreateSimulationTime(2)));

        Assert.Throws<InvalidOperationException>(
            () => game.Render(0.0));

        // Повторная остановка не должна повторять очистку игры.
        game.Shutdown();

        Assert.Equal(1, game.ShutdownCount);
    }


    [Fact]
    public void Shutdown_DisposesGameOwnedEventSubscription()
    {
        using var runtime = CreateRuntime();
        var game = new SubscriptionProbeGame(runtime);

        game.Initialize();

        runtime.Events.Publish(new LifecycleProbeEvent());
        runtime.Events.DispatchPending();

        Assert.Equal(1, game.ReceivedCount);

        game.Shutdown();

        // После остановки подписчик больше не должен получать события.
        runtime.Events.Publish(new LifecycleProbeEvent());
        runtime.Events.DispatchPending();

        Assert.Equal(1, game.ReceivedCount);
    }

    private sealed record LifecycleProbeEvent : IEvent;

    private sealed class SubscriptionProbeGame : EngineGame
    {
        private EventSubscription? _subscription;

        public int ReceivedCount { get; private set; }

        public SubscriptionProbeGame(EngineRuntime runtime)
            : base(runtime)
        {
        }

        protected override void ConfigureRuntime()
        {
            _subscription = Runtime.Events.Subscribe<LifecycleProbeEvent>(
                _ => ReceivedCount++);
        }

        protected override void OnShutdown()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }


    [Fact]
    public void NewRuntime_DoesNotInheritEntitiesOrUserSystemRegistrations()
    {
        using var firstRuntime = CreateRuntime();

        var initialSystemCount =
            firstRuntime.Scheduler.SystemCount;

        var firstGame =
            new LifecycleProbeGame(firstRuntime);

        firstGame.Initialize();

        var previousEntity = firstGame.Entity;

        Assert.Equal(
            initialSystemCount + 1,
            firstRuntime.Scheduler.SystemCount);

        firstGame.FixedUpdate(CreateSimulationTime(1));

        Assert.Equal(
            1,
            firstRuntime.EcsWorld
                .Get<ProbeComponent>(previousEntity)
                .ExecutionCount);

        firstGame.Shutdown();
        firstRuntime.Dispose();

        // Новый экземпляр должен иметь собственное состояние.
        using var secondRuntime = CreateRuntime();

        Assert.Equal(
            0,
            secondRuntime.EcsWorld.EntityCount);

        Assert.False(
            secondRuntime.EcsWorld.Exists(previousEntity));

        Assert.Equal(
            initialSystemCount,
            secondRuntime.Scheduler.SystemCount);

        var secondGame =
            new LifecycleProbeGame(secondRuntime);

        secondGame.Initialize();

        Assert.Equal(
            initialSystemCount + 1,
            secondRuntime.Scheduler.SystemCount);

        secondGame.FixedUpdate(CreateSimulationTime(1));

        Assert.Equal(
            1,
            secondRuntime.EcsWorld
                .Get<ProbeComponent>(secondGame.Entity)
                .ExecutionCount);

        secondGame.Shutdown();
    }

    private static EngineRuntime CreateRuntime()
    {
        return new EngineRuntime(
            new EngineRuntimeOptions
            {
                ToolingEnabled = false
            });
    }

    private static SimulationTime CreateSimulationTime(int tick)
    {
        return new SimulationTime(
            Fixed32.FromRatio(1, 20),
            new Tick((ulong)tick));
    }

    private struct ProbeComponent
    {
        public int ExecutionCount { get; set; }
    }

    private sealed class ProbeFixedSystem : IFixedUpdateSystem
    {
        private readonly Engine.ECS.World _world;
        private readonly EntityId _entity;

        public ProbeFixedSystem(
            Engine.ECS.World world,
            EntityId entity)
        {
            _world = world;
            _entity = entity;
        }

        public void FixedUpdate(FixedSystemContext context)
        {
            ref var component =
                ref _world.Get<ProbeComponent>(_entity);

            component.ExecutionCount++;
        }
    }

    private sealed class LifecycleProbeGame : EngineGame
    {
        public EntityId Entity { get; private set; }

        public int ShutdownCount { get; private set; }

        public LifecycleProbeGame(EngineRuntime runtime)
            : base(runtime)
        {
        }

        protected override void ConfigureRuntime()
        {
            Entity = Runtime.EcsWorld.CreateEntity();

            Runtime.EcsWorld.Add(
                Entity,
                new ProbeComponent());

            Runtime.Scheduler.Add(
                new ProbeFixedSystem(
                    Runtime.EcsWorld,
                    Entity),
                SystemPhase.FixedUpdate);
        }

        protected override void OnShutdown()
        {
            ShutdownCount++;
        }
    }
}
