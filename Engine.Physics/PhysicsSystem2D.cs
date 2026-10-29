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
using Engine.Physics.Shapes;

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
    private readonly List<CollisionManifold> _solverManifolds = new();
    private readonly Dictionary<
        CollisionPair,
        ContactState> _previousContacts = new();

    private readonly Dictionary<
        CollisionPair,
        ContactState> _currentContacts = new();

    private readonly List<PhysicsContactEvent> _contactEvents = new();
    private readonly List<CollisionPair> _currentContactPairs = new();
    private readonly List<CollisionPair> _exitedContactPairs = new();
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
            new SpatialHashBroadPhase(Fixed32.One);

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

        var substeps =
            _settings.Substeps;

        var substepDelta =
            context.Time.Delta /
            Fixed32.FromInt(
                substeps);

        var substepTime =
            new SimulationTime(
                substepDelta,
                context.Time.Tick);

        var contactsValid = false;

        for (var step = 0;
             step < substeps;
             step++)
        {
            SimulateSubstep(
                substepTime,
                ref contactsValid);
        }

        ApplyDamping(
            context.Time.Delta);

        SolveJointVelocities();

        PublishContactEvents();

        UpdateSleeping(
            context.Time.Delta);

        ClearForces();
    }

    private void SimulateSubstep(
        SimulationTime time,
        ref bool contactsValid)
    {
        var transformsChanged = Integrate(time);

        if (transformsChanged || !contactsValid)
        {
            DetectContacts();
            contactsValid = true;
        }

        WakeSleepingBodies();

        if (SolvePositions())
        {
            DetectContacts();
            contactsValid = true;
        }

        if (SolveJointPositions())
        {
            DetectContacts();
            contactsValid = true;
        }

        SolveVelocities();
    }

    private bool SolveJointPositions()
    {
        var jointsMayHaveChanged = false;

        for (var iteration = 0;
             iteration < _settings.PositionIterations;
             iteration++)
        {
            foreach (var item in _world.QueryReadOnly<DistanceJoint2D>())
            {
                if (item.Component.Enabled)
                {
                    jointsMayHaveChanged = true;
                }

                _jointSolver.SolvePosition(
                    _world,
                    item.Component,
                    _settings.PositionCorrectionPercent);
            }
        }

        return jointsMayHaveChanged;
    }

    private void SolveJointVelocities()
    {
        for (var iteration = 0;
             iteration < _settings.VelocityIterations;
             iteration++)
        {
            foreach (var item
                     in _world.QueryReadOnly<DistanceJoint2D>())
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
        foreach (var manifold in _solverManifolds)
        {
            WakeBodyFromContact(
                manifold.Pair.First,
                manifold.Pair.Second);

            WakeBodyFromContact(
                manifold.Pair.Second,
                manifold.Pair.First);
        }

        foreach (var item
                 in _world.QueryReadOnly<DistanceJoint2D>())
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

    private bool Integrate(
        SimulationTime time)
    {
        var delta = time.Delta;
        var transformsChanged = false;

        foreach (var item in _world.Query<PhysicsBody2D>())
        {
            ref var body = ref item.Component;

            if (body.BodyType == PhysicsBodyType.Static)
            {
                continue;
            }

            if (body.BodyType == PhysicsBodyType.Dynamic &&
                body.IsSleeping)
            {
                continue;
            }

            ref var transform =
                ref _world.Get<WorldTransform2D>(item.Entity);

            if (body.BodyType == PhysicsBodyType.Dynamic)
            {
                var gravity =
                    _settings.Gravity * body.GravityScale;

                var acceleration =
                    gravity + body.Force * body.InverseMass;

                body.Velocity += acceleration * delta;

                body.AngularVelocity +=
                    body.Torque * body.InverseInertia * delta;
            }

            var positionDelta = body.Velocity * delta;
            var rotationDelta = body.AngularVelocity * delta;

            if (positionDelta != FixedVector2.Zero ||
                rotationDelta != Fixed32.Zero)
            {
                transformsChanged = true;
            }

            transform.Position += positionDelta;
            transform.Rotation += rotationDelta;
        }

        return transformsChanged;
    }

    private void ApplyDamping(
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

            if (body.LinearDamping > Fixed32.Zero)
            {
                var factor =
                    Fixed32.One /
                    (Fixed32.One +
                     body.LinearDamping * delta);

                body.Velocity *=
                    factor;
            }

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
    }

    private void DetectContacts()
    {
        _colliders.Clear();
        _pairs.Clear();
        _manifolds.Clear();
        _currentContacts.Clear();
        _solverManifolds.Clear();
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

            if (!first.Value.Collider.CanCollideWith(
        second.Value.Collider))
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

            if (type == PhysicsContactType.Trigger)
            {
                continue;
            }

            _manifolds.Add(manifold);

            ref var firstBody =
                ref _world.Get<PhysicsBody2D>(
                    pair.First);

            ref var secondBody =
                ref _world.Get<PhysicsBody2D>(
                    pair.Second);

            if (firstBody.InverseMass > Fixed32.Zero ||
                secondBody.InverseMass > Fixed32.Zero)
            {
                _solverManifolds.Add(manifold);
            }
        }
    }

    private bool SolvePositions()
    {
        var contactsNeedRefresh = false;

        for (var iteration = 0;
             iteration < _settings.PositionIterations;
             iteration++)
        {
            var positionsChanged = false;

            foreach (var manifold in _solverManifolds)
            {
                if (_resolver.TryResolvePosition(
                        _world,
                        manifold,
                        _settings.PenetrationSlop,
                        _settings.PositionCorrectionPercent))
                {
                    positionsChanged = true;
                    contactsNeedRefresh = true;
                }
            }

            if (!positionsChanged)
            {
                break;
            }

            if (iteration + 1 <
                _settings.PositionIterations)
            {
                DetectContacts();
                contactsNeedRefresh = false;
            }
        }

        return contactsNeedRefresh;
    }

    private void SolveVelocities()
    {
        for (var iteration = 0;
             iteration < _settings.VelocityIterations;
             iteration++)
        {
            foreach (var manifold in _solverManifolds)
            {
                _resolver.ResolveVelocity(
                    _world,
                    manifold);
            }
        }
    }

    private void PublishContactEvents()
    {
        _contactEvents.Clear();

        _currentContactPairs.Clear();

        foreach (var pair in _currentContacts.Keys)
        {
            _currentContactPairs.Add(pair);
        }

        SortPairs(_currentContactPairs);

        foreach (var pair in _currentContactPairs)
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

        _exitedContactPairs.Clear();

        foreach (var pair in _previousContacts.Keys)
        {
            if (!_currentContacts.ContainsKey(pair))
            {
                _exitedContactPairs.Add(pair);
            }
        }

        SortPairs(_exitedContactPairs);

        foreach (var pair in _exitedContactPairs)
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
        _contactEvents.Add(@event);

        if (_events.HasSubscribers<PhysicsContactEvent>())
        {
            _events.Publish(@event);
        }
    }

    private void CollectColliders()
    {
        _colliders.Clear();

        foreach (var item in _world.Query<PhysicsBody2D>())
        {
            var entity = item.Entity;

            if (!_world.Has<Collider2D>(entity))
            {
                continue;
            }

            ref var sourceCollider =
                ref _world.Get<Collider2D>(entity);

            if (!sourceCollider.Enabled)
            {
                continue;
            }

            sourceCollider.Validate();

            ref var transform =
                ref _world.Get<WorldTransform2D>(entity);

            var collider = sourceCollider;

            if (!PhysicsColliderScale2D.TryApplyWorldScale(
                    ref collider,
                    transform.Scale))
            {
                continue;
            }

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
                    WorldPosition = worldPosition,
                    WorldRotation = transform.Rotation
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
        var low = 0;
        var high = _colliders.Count - 1;

        while (low <= high)
        {
            var middle =
                low + ((high - low) >> 1);

            var candidate =
                _colliders[middle];

            var comparison =
                candidate.Entity.Index.CompareTo(
                    entity.Index);

            if (comparison == 0)
            {
                return candidate.Entity == entity
                    ? candidate
                    : null;
            }

            if (comparison < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
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