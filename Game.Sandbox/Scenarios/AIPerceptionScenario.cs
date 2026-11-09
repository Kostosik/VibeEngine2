
using Engine.AI.Behaviors;
using Engine.AI.Components;
using Engine.AI.Core;
using Engine.AI.Perception.Core;
using Engine.AI.Perception.Sensors;
using Engine.AI.Systems;
using Engine.Core.Math;
using Engine.Core.Time;
using Engine.ECS;
using Engine.ECS.Entities;

namespace Game.Sandbox.Scenarios;

internal sealed class AIPerceptionScenario :
    IAIBehavior,
    IDisposable
{
    private struct ProbePosition
    {
        public Vector2 Position;

        public ProbePosition(Vector2 position)
        {
            Position = position;
        }
    }

    private readonly World _world;
    private readonly AISystem _aiSystem;

    private readonly EntityId _observer;
    private readonly EntityId _stationaryTarget;
    private readonly EntityId _movingTarget;

    private bool _disposed;

    private string _status = "INITIALIZING";

    public string Status => _status;

    public AIPerceptionScenario(
        World world,
        AISystem aiSystem)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(aiSystem);

        _world = world;
        _aiSystem = aiSystem;

        _observer = _world.CreateEntity();

        _world.Add(
            _observer,
            new AIComponent());

        _world.Add(
            _observer,
            new ProbePosition(Vector2.Zero));

        _stationaryTarget = CreateTarget(
            new Vector2(5f, 0f));

        _movingTarget = CreateTarget(
            new Vector2(10f, 0f));

        var sensor = new RadiusSensor<ProbePosition>(
            "SandboxRadar",
            12f,
            static (in PerceptionContext context) =>
                context.World
                    .GetReadOnly<ProbePosition>(
                        context.Observer)
                    .Position,
            static (entity, component) =>
                component.Position);

        _aiSystem.Perception.RegisterSensor(
            _observer,
            sensor);

        _aiSystem.SetBehavior(
            _observer,
            this);
    }

    public void Update(TimeSnapshot time)
    {
        if (_disposed ||
            !_world.Exists(_movingTarget))
        {
            return;
        }

        float elapsedSeconds =
            (float)time.Elapsed.Value.TotalSeconds;

        float x =
            10f +
            MathF.Sin(elapsedSeconds * 0.55f) * 14f;

        ref var movingPosition =
            ref _world.Get<ProbePosition>(
                _movingTarget);

        movingPosition.Position =
            new Vector2(x, 0f);
    }

    public void Update(in AIContext context)
    {
        if (context.Perception is not { } perception)
        {
            _status = "NO PERCEPTION STATE";
            return;
        }

        int contactCount =
            perception.Observations.Count;

        bool mobileDetected =
            perception.TryGetBestObservation(
                _movingTarget,
                out var observation);

        if (mobileDetected)
        {
            float distance =
                MathF.Sqrt(observation.DistanceSquared);

            _status =
                $"CONTACTS {contactCount} | " +
                $"MOBILE VISIBLE | DIST {distance:0.0}";
        }
        else
        {
            _status =
                $"CONTACTS {contactCount} | MOBILE LOST";
        }
    }

    private EntityId CreateTarget(Vector2 position)
    {
        var entity = _world.CreateEntity();

        _world.Add(
            entity,
            new ProbePosition(position));

        return entity;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _aiSystem.RemoveBehavior(_observer);

        _aiSystem.Perception.RemoveAgent(_observer);

        DestroyIfExists(_observer);
        DestroyIfExists(_stationaryTarget);
        DestroyIfExists(_movingTarget);
    }

    private void DestroyIfExists(EntityId entity)
    {
        if (_world.Exists(entity))
        {
            _world.DestroyEntity(entity);
        }
    }
}
