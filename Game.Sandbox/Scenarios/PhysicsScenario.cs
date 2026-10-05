using Engine.Core.Events;
using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Physics.Collision;
using Engine.Physics.Components;
using Engine.Physics.Joints;
using Engine.Physics.Shapes;
using Engine.Runtime;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Game.Sandbox.Scenarios;

public sealed class PhysicsScenario :
    IDisposable
{
    private readonly EngineRuntime _runtime;
    private readonly Engine.ECS.World _world;

    private readonly EventSubscription _physicsSubscription;

    private readonly List<EntityId> _entities = new();
    private readonly List<EntityId> _spawnedEntities = new();

    private EntityId _floor;
    private EntityId _box;
    private EntityId _circle;
    private EntityId _polygon;
    private EntityId _trigger;
    private EntityId _sleepBody;
    private EntityId _jointFirst;
    private EntityId _jointSecond;

    private int _collisionCount;
    private int _triggerCount;

    public PhysicsScenario(
        EngineRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        _runtime = runtime;
        _world = runtime.EcsWorld;

        _physicsSubscription =
            runtime.Events.Subscribe<PhysicsContactEvent>(
                OnPhysicsContact);

        Reset();
    }

    public void Reset()
    {
        ClearEntities();

        _collisionCount = 0;
        _triggerCount = 0;

        _runtime.PhysicsSettings.Gravity =
            new FixedVector2(
                Fixed32.Zero,
                Fixed32.FromFloat(-9.8f));

        _floor =
            CreateStaticAabb(
                new FixedVector2(
                    Fixed32.FromInt(120),
                    Fixed32.FromInt(4)),
                new FixedVector2(
                    Fixed32.FromInt(40),
                    Fixed32.One));

        _box =
            CreateDynamicAabb(
                new FixedVector2(
                    Fixed32.FromInt(114),
                    Fixed32.FromInt(18)));

        _circle =
            CreateDynamicCircle(
                new FixedVector2(
                    Fixed32.FromInt(120),
                    Fixed32.FromInt(22)));

        _polygon =
            CreateDynamicPolygon(
                new FixedVector2(
                    Fixed32.FromInt(126),
                    Fixed32.FromInt(18)));

        _trigger =
            CreateTrigger(
                new FixedVector2(
                    Fixed32.FromInt(120),
                    Fixed32.FromInt(10)),
                new FixedVector2(
                    Fixed32.FromInt(5),
                    Fixed32.One));

        _sleepBody =
            CreateDynamicAabb(
                new FixedVector2(
                    Fixed32.FromInt(110),
                    Fixed32.FromInt(8)));

        _jointFirst =
            CreateDynamicAabb(
                new FixedVector2(
                    Fixed32.FromInt(134),
                    Fixed32.FromInt(16)));

        _jointSecond =
            CreateDynamicAabb(
                new FixedVector2(
                    Fixed32.FromInt(138),
                    Fixed32.FromInt(16)));

        _world.Add(
            _jointFirst,
            new DistanceJoint2D(
                _jointFirst,
                _jointSecond,
                Fixed32.FromInt(4)));
    }

    public void SpawnStressBatch()
    {
        for (var i = 0; i < 25; i++)
        {
            var x =
                i % 5;

            var y =
                i / 5;

            _spawnedEntities.Add(
                CreateDynamicAabb(
                    new FixedVector2(
                        Fixed32.FromInt(
                            113 + x * 2),
                        Fixed32.FromInt(
                            24 + y * 2))));
        }
    }

    public void ClearSpawnedEntities()
    {
        foreach (var entity in _spawnedEntities)
        {
            if (_world.Exists(entity))
            {
                _world.DestroyEntity(entity);
            }

            _entities.Remove(entity);
        }

        _spawnedEntities.Clear();
    }

    public void ApplyForce()
    {
        if (!_world.Exists(_box))
        {
            return;
        }

        ref var body =
            ref _world.Get<PhysicsBody2D>(
                _box);

        body.AddForce(
            new FixedVector2(
                Fixed32.Zero,
                Fixed32.FromInt(12)));
    }

    public void ToggleTrigger()
    {
        if (!_world.Exists(_trigger))
        {
            return;
        }

        ref var collider =
            ref _world.Get<Collider2D>(
                _trigger);

        collider.Enabled =
            !collider.Enabled;
    }

    public void RotatePolygon()
    {
        if (!_world.Exists(_polygon))
        {
            return;
        }

        ref var transform =
            ref _world.Get<WorldTransform2D>(
                _polygon);

        transform.Rotation +=
            Fixed32.FromFloat(
                0.25f);
    }

    public string GetStatus()
    {
        var sleepPass =
            _world.Exists(_sleepBody) &&
            _world.Has<PhysicsBody2D>(_sleepBody) &&
            _world.Get<PhysicsBody2D>(
                _sleepBody).IsSleeping;

        var jointPass =
            CheckJoint();

        var worldPass =
            CheckWorldCoordinates();

        var polygonContact =
    GetPolygonContactState();

        var circleState =
    GetBodyState(
        _circle);

        var polygonState =
            GetBodyState(
                _polygon);
        DumpPolygonDiagnostic();
        DumpCircleDiagnostic();
        return
            $"Entities       {_world.EntityCount}\n" +
            $"Collisions     {_collisionCount}\n" +
            $"Triggers       {_triggerCount}\n" +
            $"Circle         {circleState}\n" +
            $"Polygon        {polygonState}\n" +
            $"Polygon        {polygonState}\n" +
            $"Poly contact   {polygonContact}\n" +
            $"Sleep          {Pass(sleepPass)}\n" +
            $"Distance joint {Pass(jointPass)}\n" +
            $"World mapping  {Pass(worldPass)}";
    }

    private bool CheckJoint()
    {
        if (!_world.Exists(_jointFirst) ||
            !_world.Exists(_jointSecond))
        {
            return false;
        }

        var first =
            _world.Get<WorldTransform2D>(
                _jointFirst).Position;

        var second =
            _world.Get<WorldTransform2D>(
                _jointSecond).Position;

        var distance =
            Fixed32.Sqrt(
                (second - first)
                    .LengthSquared());

        return Fixed32.Abs(
                   distance -
                   Fixed32.FromInt(4)) <=
               Fixed32.FromFloat(0.25f);
    }

    private bool CheckWorldCoordinates()
    {
        var position =
            new WorldPosition(
                -1,
                -1);

        var chunk =
            _runtime.World.GetChunkPosition(
                position);

        var local =
            _runtime.World.GetLocalPosition(
                position);

        return
            chunk ==
                new ChunkPosition(
                    -1,
                    -1) &&
            local ==
                new LocalPosition(
                    31,
                    31) &&
            _runtime.World.ToWorldPosition(
                chunk,
                local) == position;
    }

    private EntityId CreateStaticAabb(
        FixedVector2 position,
        FixedVector2 size)
    {
        var entity =
            _world.CreateEntity();

        _world.Add(
            entity,
            new WorldTransform2D(
                position));

        _world.Add(
            entity,
            PhysicsBody2D.Static());

        _world.Add(
            entity,
            new Collider2D(
                new AabbShape2D(
                    size)));

        _entities.Add(entity);

        return entity;
    }

    private EntityId CreateTrigger(
        FixedVector2 position,
        FixedVector2 size)
    {
        var entity =
            CreateStaticAabb(
                position,
                size);

        ref var collider =
            ref _world.Get<Collider2D>(
                entity);

        collider.IsTrigger =
            true;

        return entity;
    }

    private EntityId CreateDynamicAabb(
        FixedVector2 position)
    {
        var entity =
            _world.CreateEntity();

        _world.Add(
            entity,
            new WorldTransform2D(
                position));

        _world.Add(
            entity,
            PhysicsBody2D.Dynamic(
                Fixed32.One));

        _world.Add(
            entity,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.One,
                        Fixed32.One))));

        _entities.Add(entity);

        return entity;
    }

    private EntityId CreateDynamicCircle(
        FixedVector2 position)
    {
        var entity =
            _world.CreateEntity();

        _world.Add(
            entity,
            new WorldTransform2D(
                position));

        _world.Add(
            entity,
            PhysicsBody2D.Dynamic(
                Fixed32.One));

        _world.Add(
            entity,
            new Collider2D(
                new CircleShape2D(
                    Fixed32.One)));

        _entities.Add(entity);

        return entity;
    }

    private EntityId CreateDynamicPolygon(
        FixedVector2 position)
    {
        var entity =
            _world.CreateEntity();

        _world.Add(
            entity,
            new WorldTransform2D(
                position));

        _world.Add(
            entity,
            PhysicsBody2D.Dynamic(
                Fixed32.One));

        _world.Add(
            entity,
            new Collider2D(
                new PolygonShape2D(
                    new[]
                    {
                        new FixedVector2(
                            -Fixed32.One,
                            -Fixed32.One),

                        new FixedVector2(
                            Fixed32.One,
                            -Fixed32.One),

                        new FixedVector2(
                            Fixed32.Zero,
                            Fixed32.One)
                    })));

        _entities.Add(entity);

        return entity;
    }

    private void OnPhysicsContact(
        PhysicsContactEvent @event)
    {
        if (@event.Phase !=
            PhysicsContactPhase.Enter)
        {
            return;
        }

        if (@event.Type ==
            PhysicsContactType.Trigger)
        {
            _triggerCount++;
        }
        else
        {
            _collisionCount++;
        }
    }

    private string GetPolygonContactState()
    {
        if (!_world.Exists(_polygon))
        {
            return "N/A";
        }

        foreach (var manifold in _runtime.Physics.Contacts)
        {
            if (manifold.Pair.First != _polygon &&
                manifold.Pair.Second != _polygon)
            {
                continue;
            }

            var contact =
                manifold.Contact;

            return
                $"P=({contact.Position.X.ToFloat():F3}," +
                $"{contact.Position.Y.ToFloat():F3}) " +
                $"N=({contact.Normal.X.ToFloat():F3}," +
                $"{contact.Normal.Y.ToFloat():F3}) " +
                $"Depth={contact.Penetration.ToFloat():F4}";
        }

        return "NONE";
    }

    private string GetBodyState(
    EntityId entity)
    {
        if (!_world.Exists(entity) ||
            !_world.Has<PhysicsBody2D>(entity) ||
            !_world.Has<WorldTransform2D>(entity))
        {
            return "N/A";
        }

        var body =
            _world.Get<PhysicsBody2D>(
                entity);

        var transform =
            _world.Get<WorldTransform2D>(
                entity);

        return
            $"Y={transform.Position.Y.ToFloat():F3} " +
            $"VY={body.Velocity.Y.ToFloat():F3} " +
            $"W={body.AngularVelocity.ToFloat():F3} " +
            $"Sleep={body.IsSleeping}";
    }

    private string? _lastPolygonDiagnostic;

    private void DumpPolygonDiagnostic()
    {
        if (!_world.Exists(_polygon) ||
            !_world.Has<PhysicsBody2D>(_polygon) ||
            !_world.Has<WorldTransform2D>(_polygon))
        {
            return;
        }

        var body =
            _world.Get<PhysicsBody2D>(
                _polygon);

        var transform =
            _world.Get<WorldTransform2D>(
                _polygon);

        var lines =
            new List<string>();

        foreach (var manifold in _runtime.Physics.Contacts)
        {
            if (manifold.Pair.First != _polygon &&
                manifold.Pair.Second != _polygon)
            {
                continue;
            }

            var otherEntity =
                manifold.Pair.First == _polygon
                    ? manifold.Pair.Second
                    : manifold.Pair.First;

            var otherShape =
                _world.Has<Collider2D>(
                    otherEntity)
                    ? _world
                        .Get<Collider2D>(
                            otherEntity)
                        .Shape
                        .Type
                        .ToString()
                    : "N/A";

            var contact =
                manifold.Contact;

            var otherBodyText =
                string.Empty;

            if (_world.Has<PhysicsBody2D>(
                    otherEntity) &&
                _world.Has<WorldTransform2D>(
                    otherEntity))
            {
                var otherBody =
                    _world.Get<PhysicsBody2D>(
                        otherEntity);

                var otherTransform =
                    _world.Get<WorldTransform2D>(
                        otherEntity);

                otherBodyText =
                    $" OtherY={otherTransform.Position.Y.ToFloat():F3}" +
                    $" OtherVY={otherBody.Velocity.Y.ToFloat():F3}";
            }

            lines.Add(
                $"Pair={manifold.Pair.First.Index}->{manifold.Pair.Second.Index}" +
                $" OtherShape={otherShape}" +
                $" PolyY={transform.Position.Y.ToFloat():F3}" +
                $" VY={body.Velocity.Y.ToFloat():F3}" +
                $" W={body.AngularVelocity.ToFloat():F3}" +
                $" P=({contact.Position.X.ToFloat():F3}," +
                $"{contact.Position.Y.ToFloat():F3})" +
                $" N=({contact.Normal.X.ToFloat():F3}," +
                $"{contact.Normal.Y.ToFloat():F3})" +
                $" Depth={contact.Penetration.ToFloat():F4}" +
                otherBodyText);
        }

        if (lines.Count == 0)
        {
            _lastPolygonDiagnostic = null;
            return;
        }

        var diagnostic =
            string.Join(
                Environment.NewLine,
                lines);

        if (diagnostic != _lastPolygonDiagnostic)
        {
            System.Console.WriteLine(
                $"--- Polygon contacts ---");

            System.Console.WriteLine(
                diagnostic);

            System.Console.WriteLine(
                $"------------------------");

            _lastPolygonDiagnostic =
                diagnostic;
        }
    }

    private string? _lastCircleDiagnostic;

    private void DumpCircleDiagnostic()
    {
        if (!_world.Exists(_circle) ||
            !_world.Has<PhysicsBody2D>(_circle) ||
            !_world.Has<WorldTransform2D>(_circle))
        {
            return;
        }

        var body =
            _world.Get<PhysicsBody2D>(
                _circle);

        var transform =
            _world.Get<WorldTransform2D>(
                _circle);

        var lines =
            new List<string>();

        foreach (var manifold in _runtime.Physics.Contacts)
        {
            if (manifold.Pair.First != _circle &&
                manifold.Pair.Second != _circle)
            {
                continue;
            }

            var otherEntity =
                manifold.Pair.First == _circle
                    ? manifold.Pair.Second
                    : manifold.Pair.First;

            var otherShape =
                _world.Has<Collider2D>(
                    otherEntity)
                    ? _world
                        .Get<Collider2D>(
                            otherEntity)
                        .Shape
                        .Type
                        .ToString()
                    : "N/A";

            var contact =
                manifold.Contact;

            lines.Add(
                $"Pair={manifold.Pair.First.Index}->{manifold.Pair.Second.Index}" +
                $" OtherShape={otherShape}" +
                $" CircleY={transform.Position.Y.ToFloat():F4}" +
                $" VY={body.Velocity.Y.ToFloat():F4}" +
                $" Sleep={body.IsSleeping}" +
                $" SleepTimer={body.SleepTimer.ToFloat():F4}" +
                $" P=({contact.Position.X.ToFloat():F4}," +
                $"{contact.Position.Y.ToFloat():F4})" +
                $" N=({contact.Normal.X.ToFloat():F4}," +
                $"{contact.Normal.Y.ToFloat():F4})" +
                $" Depth={contact.Penetration.ToFloat():F4}");
        }

        if (lines.Count == 0)
        {
            var diagnostic =
                $"Circle: NO CONTACT " +
                $"Y={transform.Position.Y.ToFloat():F4} " +
                $"VY={body.Velocity.Y.ToFloat():F4} " +
                $"Sleep={body.IsSleeping}";

            if (diagnostic != _lastCircleDiagnostic)
            {
                System.Console.WriteLine(
                    diagnostic);

                _lastCircleDiagnostic =
                    diagnostic;
            }

            return;
        }

        var result =
            string.Join(
                Environment.NewLine,
                lines);

        if (result != _lastCircleDiagnostic)
        {
            System.Console.WriteLine(
                "--- Circle contacts ---");

            System.Console.WriteLine(
                result);

            System.Console.WriteLine(
                "-----------------------");

            _lastCircleDiagnostic =
                result;
        }
    }

    private void ClearEntities()
    {
        foreach (var entity in _entities)
        {
            if (_world.Exists(entity))
            {
                _world.DestroyEntity(entity);
            }
        }

        _entities.Clear();
        _spawnedEntities.Clear();
    }

    private static string Pass(
        bool value)
    {
        return value
            ? "PASS"
            : "WAIT";
    }

    public void Dispose()
    {
        _physicsSubscription.Dispose();

        ClearEntities();
    }
}