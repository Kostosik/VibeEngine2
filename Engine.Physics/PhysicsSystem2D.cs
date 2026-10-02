using Engine.Core.Events;
using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Physics.BroadPhase;
using Engine.Physics.Collision;
using Engine.Physics.Components;
using Engine.Physics.Joints;

namespace Engine.Physics;

public sealed class PhysicsSystem2D :
    IFixedUpdateSystem
{
    private readonly World _world;
    private readonly PhysicsSettings2D _settings;
    private readonly EventBus _events;

    private readonly IPhysicsBroadPhase _broadPhase;
    private readonly CollisionDetector2D _detector;
    private readonly CollisionResolver2D _resolver;
    private readonly DistanceJointSolver2D _jointSolver;
    private readonly List<PhysicsColliderProxy> _colliders = new();
    private readonly List<CollisionPair> _pairs = new();
    private readonly List<CollisionManifold> _manifolds = new();

    private readonly Dictionary<
        CollisionPair,
        ContactState> _previousContacts = new();

    private readonly Dictionary<
        CollisionPair,
        ContactState> _currentContacts = new();

    private readonly List<PhysicsContactEvent> _contactEvents = new();

    public PhysicsSystem2D(
        World world,
        PhysicsSettings2D settings,
        EventBus events,
        IPhysicsBroadPhase? broadPhase = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(events);

        _world = world;
        _settings = settings;
        _events = events;

        _broadPhase =
            broadPhase ??
            new BruteForceBroadPhase();

        _detector =
            new CollisionDetector2D();

        _resolver =
            new CollisionResolver2D();
        _jointSolver =
    new DistanceJointSolver2D();
    }

    public IReadOnlyList<CollisionManifold> Contacts =>
        _manifolds;

    public IReadOnlyList<PhysicsContactEvent> ContactEvents =>
        _contactEvents;

    public void FixedUpdate(
        FixedSystemContext context)
    {
        ClearTransientState();

        Integrate(
            context.Time);

        DetectContacts();

        WakeSleepingBodies();

        SolvePositions();

        SolveJointPositions();

        DetectContacts();

        SolveVelocities();

        SolveJointVelocities();

        BuildCurrentContactState();

        PublishContactEvents();

        UpdateSleeping(
    context.Time.Delta);

        ClearForces();
    }

    private void SolveJointPositions()
    {
        for (var iteration = 0;
             iteration < _settings.PositionIterations;
             iteration++)
        {
            foreach (var item
                     in _world.Query<DistanceJoint2D>())
            {
                _jointSolver.SolvePosition(
                    _world,
                    item.Component,
                    _settings.PositionCorrectionPercent);
            }
        }
    }

    private void SolveJointVelocities()
    {
        for (var iteration = 0;
             iteration < _settings.VelocityIterations;
             iteration++)
        {
            foreach (var item
                     in _world.Query<DistanceJoint2D>())
            {
                _jointSolver.SolveVelocity(
                    _world,
                    item.Component);
            }
        }
    }

    private void UpdateSleeping(
    Fixed32 delta)
    {
        foreach (var item
                 in _world.Query<PhysicsBody2D>())
        {
            ref var body =
                ref item.Component;

            if (body.BodyType !=
                PhysicsBodyType.Dynamic)
            {
                continue;
            }

            if (body.IsSleeping)
            {
                continue;
            }

            if (body.Force != FixedVector2.Zero ||
                body.Torque != Fixed32.Zero)
            {
                body.WakeUp();
                continue;
            }

            var linearSpeedSquared =
                body.Velocity.LengthSquared();

            var linearThresholdSquared =
                _settings.SleepLinearVelocityThreshold *
                _settings.SleepLinearVelocityThreshold;

            var angularSpeed =
                Fixed32.Abs(
                    body.AngularVelocity);

            if (linearSpeedSquared <=
                    linearThresholdSquared &&
                angularSpeed <=
                    _settings.SleepAngularVelocityThreshold)
            {
                body.SleepTimer += delta;

                if (body.SleepTimer >=
                    _settings.SleepTime)
                {
                    body.Sleep();
                }
            }
            else
            {
                body.WakeUp();
            }
        }
    }

    private void WakeSleepingBodies()
    {
        foreach (var manifold in _manifolds)
        {
            WakeBodyFromContact(
                manifold.Pair.First,
                manifold.Pair.Second);

            WakeBodyFromContact(
                manifold.Pair.Second,
                manifold.Pair.First);
        }

        foreach (var item
                 in _world.Query<DistanceJoint2D>())
        {
            var joint =
                item.Component;

            if (!joint.Enabled)
            {
                continue;
            }

            WakeBodyFromContact(
                joint.First,
                joint.Second);

            WakeBodyFromContact(
                joint.Second,
                joint.First);
        }
    }

    private void WakeBodyFromContact(
    EntityId entity,
    EntityId otherEntity)
    {
        if (!_world.Exists(entity) ||
            !_world.Exists(otherEntity) ||
            !_world.Has<PhysicsBody2D>(entity) ||
            !_world.Has<PhysicsBody2D>(otherEntity))
        {
            return;
        }

        ref var body =
            ref _world.Get<PhysicsBody2D>(
                entity);

        if (body.BodyType !=
            PhysicsBodyType.Dynamic ||
            !body.IsSleeping)
        {
            return;
        }

        ref var other =
            ref _world.Get<PhysicsBody2D>(
                otherEntity);

        if (other.BodyType ==
            PhysicsBodyType.Kinematic ||
            (other.BodyType ==
             PhysicsBodyType.Dynamic &&
             !other.IsSleeping))
        {
            body.WakeUp();
        }
    }

    public void ResetContactState()
    {
        _previousContacts.Clear();
        _currentContacts.Clear();
        _contactEvents.Clear();
    }

    private void Integrate(
        SimulationTime time)
    {
        var delta =
            time.Delta;

        foreach (var item
                 in _world.Query<PhysicsBody2D>())
        {
            ref var body =
                ref item.Component;

            if (body.BodyType ==
                PhysicsBodyType.Static)
            {
                continue;
            }

            if (body.BodyType == PhysicsBodyType.Dynamic && body.IsSleeping)
            {
                continue;
            }

            ref var transform =
                ref _world.Get<WorldTransform2D>(
                    item.Entity);

            if (body.BodyType ==
                PhysicsBodyType.Dynamic)
            {
                var gravity =
                    _settings.Gravity *
                    body.GravityScale;

                var acceleration =
                    gravity +
                    body.Force *
                    body.InverseMass;

                body.Velocity +=
                    acceleration *
                    delta;

                if (body.LinearDamping > Fixed32.Zero)
                {
                    var factor =
                        Fixed32.One /
                        (Fixed32.One +
                         body.LinearDamping * delta);

                    body.Velocity *=
                        factor;
                }

                body.AngularVelocity +=
                    body.Torque *
                    body.InverseInertia *
                    delta;

                if (body.AngularDamping > Fixed32.Zero)
                {
                    var factor =
                        Fixed32.One /
                        (Fixed32.One +
                         body.AngularDamping * delta);

                    body.AngularVelocity *=
                        factor;
                }
            }

            transform.Position +=
                body.Velocity *
                delta;

            transform.Rotation +=
                body.AngularVelocity *
                delta;
        }
    }

    private void DetectContacts()
    {
        _colliders.Clear();
        _pairs.Clear();
        _manifolds.Clear();

        CollectColliders();

        _broadPhase.FindPairs(
            _colliders,
            _pairs);

        foreach (var pair in _pairs)
        {
            var first =
                FindCollider(
                    pair.First);

            var second =
                FindCollider(
                    pair.Second);

            if (first is null ||
                second is null)
            {
                continue;
            }

            if (!_detector.TryDetect(
                    first.Value,
                    second.Value,
                    out var manifold))
            {
                continue;
            }

            if (first.Value.Collider.IsTrigger ||
                second.Value.Collider.IsTrigger)
            {
                continue;
            }

            _manifolds.Add(
                manifold);
        }
    }

    private void SolvePositions()
    {
        for (var iteration = 0;
             iteration < _settings.PositionIterations;
             iteration++)
        {
            foreach (var manifold in _manifolds)
            {
                _resolver.ResolvePosition(
                    _world,
                    manifold,
                    _settings.PenetrationSlop,
                    _settings.PositionCorrectionPercent);
            }

            if (iteration + 1 <
                _settings.PositionIterations)
            {
                DetectContacts();
            }
        }
    }

    private void SolveVelocities()
    {
        for (var iteration = 0;
             iteration < _settings.VelocityIterations;
             iteration++)
        {
            foreach (var manifold in _manifolds)
            {
                _resolver.ResolveVelocity(
                    _world,
                    manifold);
            }
        }
    }

    private void BuildCurrentContactState()
    {
        _currentContacts.Clear();

        foreach (var pair in _pairs)
        {
            var first =
                FindCollider(
                    pair.First);

            var second =
                FindCollider(
                    pair.Second);

            if (first is null ||
                second is null)
            {
                continue;
            }

            if (!_detector.TryDetect(
                    first.Value,
                    second.Value,
                    out var manifold))
            {
                continue;
            }

            var type =
                first.Value.Collider.IsTrigger ||
                second.Value.Collider.IsTrigger
                    ? PhysicsContactType.Trigger
                    : PhysicsContactType.Collision;

            _currentContacts[pair] =
                new ContactState(
                    type,
                    manifold);
        }
    }

    private void PublishContactEvents()
    {
        _contactEvents.Clear();

        var currentPairs =
            _currentContacts.Keys.ToList();

        SortPairs(
            currentPairs);

        foreach (var pair in currentPairs)
        {
            var current =
                _currentContacts[pair];

            if (!_previousContacts.TryGetValue(
                    pair,
                    out var previous))
            {
                Publish(
                    new PhysicsContactEvent(
                        current.Type,
                        PhysicsContactPhase.Enter,
                        current.Manifold));

                continue;
            }

            if (previous.Type != current.Type)
            {
                Publish(
                    new PhysicsContactEvent(
                        previous.Type,
                        PhysicsContactPhase.Exit,
                        previous.Manifold));

                Publish(
                    new PhysicsContactEvent(
                        current.Type,
                        PhysicsContactPhase.Enter,
                        current.Manifold));

                continue;
            }

            Publish(
                new PhysicsContactEvent(
                    current.Type,
                    PhysicsContactPhase.Stay,
                    current.Manifold));
        }

        var exitedPairs =
            _previousContacts.Keys
                .Where(
                    pair =>
                        !_currentContacts.ContainsKey(
                            pair))
                .ToList();

        SortPairs(
            exitedPairs);

        foreach (var pair in exitedPairs)
        {
            var previous =
                _previousContacts[pair];

            Publish(
                new PhysicsContactEvent(
                    previous.Type,
                    PhysicsContactPhase.Exit,
                    previous.Manifold));
        }

        _previousContacts.Clear();

        foreach (var pair in _currentContacts)
        {
            _previousContacts.Add(
                pair.Key,
                pair.Value);
        }
    }

    private void Publish(
        PhysicsContactEvent @event)
    {
        _contactEvents.Add(
            @event);

        _events.Publish(
            @event);
    }

    private void CollectColliders()
    {
        _colliders.Clear();

        foreach (var item
                 in _world.Query<PhysicsBody2D>())
        {
            var entity =
                item.Entity;

            if (!_world.Has<Collider2D>(
                    entity))
            {
                continue;
            }

            ref var body =
                ref item.Component;

            ref var collider =
                ref _world.Get<Collider2D>(
                    entity);

            if (!collider.Enabled)
                continue;

            ref var transform =
                ref _world.Get<WorldTransform2D>(
                    entity);

            var worldPosition =
                collider.GetWorldPosition(
                    transform.Position,
                    transform.Rotation);

            var bounds =
                collider.GetWorldBounds(
                    transform.Position,
                    transform.Rotation);

            _colliders.Add(
                new PhysicsColliderProxy(
                    entity,
                    bounds,
                    collider)
                {
                    WorldPosition =
                        worldPosition,

                    WorldRotation =
                        transform.Rotation
                });
        }

        _colliders.Sort(
            static (left, right) =>
                left.Entity.Index.CompareTo(
                    right.Entity.Index));
    }

    private PhysicsColliderProxy? FindCollider(
        EntityId entity)
    {
        foreach (var collider in _colliders)
        {
            if (collider.Entity == entity)
                return collider;
        }

        return null;
    }

    private void ClearTransientState()
    {
        _colliders.Clear();
        _pairs.Clear();
        _manifolds.Clear();
        _currentContacts.Clear();
        _contactEvents.Clear();
    }

    private void ClearForces()
    {
        foreach (var item
                 in _world.Query<PhysicsBody2D>())
        {
            item.Component.ClearForces();
        }
    }

    private static void SortPairs(
        List<CollisionPair> pairs)
    {
        pairs.Sort(
            static (left, right) =>
            {
                var result =
                    left.First.Index.CompareTo(
                        right.First.Index);

                if (result != 0)
                    return result;

                return left.Second.Index.CompareTo(
                    right.Second.Index);
            });
    }

    private readonly record struct ContactState(
        PhysicsContactType Type,
        CollisionManifold Manifold);
}