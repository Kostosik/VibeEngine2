using Engine.Camera;
using Engine.Core.Commands;
using Engine.Core.Events;
using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Graphics;
using Engine.Graphics.Resources;
using Engine.Graphics.Sprites;
using Engine.Graphics.Tilemaps;
using Engine.Input;
using Engine.Physics.Collision;
using Engine.Physics.Components;
using Engine.Physics.Shapes;
using Engine.Runtime;
using Engine.Worlds;
using Engine.Worlds.Spatial;
using Engine.Worlds.Tiles;

namespace Game.Sandbox.Scenarios;

public sealed class TopDownScenario :
    IDisposable
{
    private readonly EngineRuntime _runtime;
    private readonly Engine.ECS.World _ecsWorld;
    private readonly World _world;
    private readonly Camera2D _camera;
    private readonly IInput _input;

    private readonly InputAction _moveUp;
    private readonly InputAction _moveDown;
    private readonly InputAction _moveLeft;
    private readonly InputAction _moveRight;

    private readonly TilemapRenderer _tilemapRenderer;
    private readonly SpriteRenderer _spriteRenderer;
    private readonly Sprite _playerSprite;

    private readonly CommandHandlerSubscription _movementSubscription;
    private readonly EventSubscription _physicsSubscription;

    private readonly List<EntityId> _entities = new();
    private readonly List<EntityId> _spawnedEntities = new();

    private EntityId _player;
    private EntityId _trigger;
    private EntityId _rotatableObstacle;

    private int _collisionCount;
    private int _triggerCount;

    public TopDownScenario(
        EngineRuntime runtime,
        Camera2D camera,
        IInput input,
        TextureAtlas tileAtlas,
        InputAction moveUp,
        InputAction moveDown,
        InputAction moveLeft,
        InputAction moveRight)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(tileAtlas);

        _runtime = runtime;
        _ecsWorld = runtime.EcsWorld;
        _world = runtime.World;
        _camera = camera;
        _input = input;

        _moveUp = moveUp;
        _moveDown = moveDown;
        _moveLeft = moveLeft;
        _moveRight = moveRight;

        _tilemapRenderer =
            new TilemapRenderer(
                runtime.Services.Graphics,
                camera,
                tileAtlas,
                1.0f);

        _spriteRenderer =
            new SpriteRenderer(
                runtime.Services.Graphics);

        var region =
            tileAtlas.GetRegion(0);

        _playerSprite =
            new Sprite(
                tileAtlas.Texture,
                region.UV,
                new Engine.Core.Math.Vector2(
                    1.0f,
                    1.0f),
                20);

        _movementSubscription =
            runtime.Simulation.CommandDispatcher.Register(
                new TopDownMoveCommandHandler(
                    _ecsWorld,
                    Fixed32.FromInt(5)));

        _physicsSubscription =
            runtime.Events.Subscribe<PhysicsContactEvent>(
                OnPhysicsContact);

        Reset();
    }

    public EntityId Player =>
        _player;

    public void Update()
    {
        var direction =
            FixedVector2.Zero;

        if (_input.IsDown(_moveUp))
        {
            direction +=
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.One);
        }

        if (_input.IsDown(_moveDown))
        {
            direction +=
                new FixedVector2(
                    Fixed32.Zero,
                    -Fixed32.One);
        }

        if (_input.IsDown(_moveLeft))
        {
            direction +=
                new FixedVector2(
                    -Fixed32.One,
                    Fixed32.Zero);
        }

        if (_input.IsDown(_moveRight))
        {
            direction +=
                new FixedVector2(
                    Fixed32.One,
                    Fixed32.Zero);
        }

        _runtime.Simulation.Submit(
            new TopDownMoveCommand(
                _player,
                direction));
    }

    public void StopMovement()
    {
        if (!_ecsWorld.Exists(_player))
        {
            return;
        }

        _runtime.Simulation.Submit(
            new TopDownMoveCommand(
                _player,
                FixedVector2.Zero));
    }

    public void Render()
    {
        if (!_ecsWorld.Exists(_player) ||
            !_ecsWorld.Has<WorldTransform2D>(_player))
        {
            return;
        }

        var playerPosition =
            _ecsWorld.Get<WorldTransform2D>(
                _player).Position;

        _camera.Position =
            new Engine.Core.Math.Vector2(
                playerPosition.X.ToFloat(),
                playerPosition.Y.ToFloat());

        foreach (var chunk in _world.GetChunks())
        {
            _tilemapRenderer.RenderChunk(
                chunk.Position.X,
                chunk.Position.Y,
                chunk.Tiles.Width,
                chunk.Tiles.Height,
                chunk.Tiles.AsValueReadOnlySpan());
        }

        _spriteRenderer.DrawWorld(
            _playerSprite,
            new Engine.Core.Math.Vector2(
                playerPosition.X.ToFloat() - 0.5f,
                playerPosition.Y.ToFloat() - 0.5f));
    }

    public void Reset()
    {
        ClearEntities();

        _collisionCount = 0;
        _triggerCount = 0;

        BuildMap();

        CreatePlayer();

        CreateBoundaries();

        CreateObstacle(
            new FixedVector2(
                Fixed32.FromInt(8),
                Fixed32.FromInt(10)),
            new FixedVector2(
                Fixed32.FromInt(12),
                Fixed32.One));

        CreateObstacle(
            new FixedVector2(
                Fixed32.FromInt(-12),
                Fixed32.FromInt(-8)),
            new FixedVector2(
                Fixed32.One,
                Fixed32.FromInt(14)));

        _rotatableObstacle =
            CreatePolygonObstacle(
                new FixedVector2(
                    Fixed32.FromInt(20),
                    Fixed32.FromInt(8)));

        _trigger =
            CreateTrigger(
                new FixedVector2(
                    Fixed32.FromInt(20),
                    Fixed32.FromInt(-2)),
                new FixedVector2(
                    Fixed32.FromInt(6),
                    Fixed32.FromInt(4)));
    }

    public void SpawnStressBatch()
    {
        for (var i = 0; i < 40; i++)
        {
            var x =
                i % 8;

            var y =
                i / 8;

            var entity =
                CreateDynamicCrate(
                    new FixedVector2(
                        Fixed32.FromInt(
                            14 + x * 2),
                        Fixed32.FromInt(
                            20 + y * 2)));

            _spawnedEntities.Add(
                entity);
        }
    }

    public void ClearSpawnedEntities()
    {
        foreach (var entity in _spawnedEntities)
        {
            if (_ecsWorld.Exists(entity))
            {
                _ecsWorld.DestroyEntity(entity);
            }

            _entities.Remove(entity);
        }

        _spawnedEntities.Clear();
    }

    public void ToggleTrigger()
    {
        if (!_ecsWorld.Exists(_trigger))
        {
            return;
        }

        ref var collider =
            ref _ecsWorld.Get<Collider2D>(
                _trigger);

        collider.Enabled =
            !collider.Enabled;
    }

    public void RotateObstacle()
    {
        if (!_ecsWorld.Exists(_rotatableObstacle))
        {
            return;
        }

        ref var transform =
            ref _ecsWorld.Get<WorldTransform2D>(
                _rotatableObstacle);

        transform.Rotation +=
            Fixed32.FromFloat(
                0.25f);
    }

    public string GetStatus()
    {
        if (!_ecsWorld.Exists(_player) ||
            !_ecsWorld.Has<WorldTransform2D>(_player))
        {
            return
                "Player          MISSING";
        }

        var position =
            _ecsWorld.Get<WorldTransform2D>(
                _player).Position;

        var worldPosition =
            new WorldPosition(
                position.X.FloorToInt(),
                position.Y.FloorToInt());

        var chunk =
            _world.GetChunkPosition(
                worldPosition);

        var local =
            _world.GetLocalPosition(
                worldPosition);

        var roundTrip =
            _world.ToWorldPosition(
                chunk,
                local);

        var worldPass =
            roundTrip == worldPosition;

        return
            $"Entities       {_ecsWorld.EntityCount}\n" +
            $"Player         {position.X.ToFloat():0.0}, {position.Y.ToFloat():0.0}\n" +
            $"Chunk          {chunk.X}, {chunk.Y}\n" +
            $"Local          {local.X}, {local.Y}\n" +
            $"World mapping  {Pass(worldPass)}\n" +
            $"Collisions     {_collisionCount}\n" +
            $"Triggers       {_triggerCount}\n" +
            $"Chunks         {_world.ChunkCount}";
    }

    private void BuildMap()
    {
        for (var chunkY = -2;
             chunkY <= 2;
             chunkY++)
        {
            for (var chunkX = -2;
                 chunkX <= 2;
                 chunkX++)
            {
                var chunk =
                    _world.GetOrCreateChunk(
                        new ChunkPosition(
                            chunkX,
                            chunkY));

                var tileValue =
                    ((chunkX + chunkY) & 1) == 0
                        ? 1u
                        : 2u;

                chunk.Tiles.Fill(
                    new Tile(tileValue));
            }
        }
    }

    private void CreatePlayer()
    {
        _player =
            CreateDynamicCrate(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.Zero));

        ref var body =
            ref _ecsWorld.Get<PhysicsBody2D>(
                _player);

        body.GravityScale =
            Fixed32.Zero;
    }

    private void CreateBoundaries()
    {
        CreateStaticAabb(
            new FixedVector2(
                Fixed32.FromFloat(-64.5f),
                Fixed32.FromInt(16)),
            new FixedVector2(
                Fixed32.One,
                Fixed32.FromInt(160)));

        CreateStaticAabb(
            new FixedVector2(
                Fixed32.FromFloat(96.5f),
                Fixed32.FromInt(16)),
            new FixedVector2(
                Fixed32.One,
                Fixed32.FromInt(160)));

        CreateStaticAabb(
            new FixedVector2(
                Fixed32.FromInt(16),
                Fixed32.FromFloat(-64.5f)),
            new FixedVector2(
                Fixed32.FromInt(160),
                Fixed32.One));

        CreateStaticAabb(
            new FixedVector2(
                Fixed32.FromInt(16),
                Fixed32.FromFloat(96.5f)),
            new FixedVector2(
                Fixed32.FromInt(160),
                Fixed32.One));
    }

    private EntityId CreateObstacle(
        FixedVector2 position,
        FixedVector2 size)
    {
        return CreateStaticAabb(
            position,
            size);
    }

    private EntityId CreateStaticAabb(
        FixedVector2 position,
        FixedVector2 size)
    {
        var entity =
            _ecsWorld.CreateEntity();

        _ecsWorld.Add(
            entity,
            new WorldTransform2D(
                position));

        _ecsWorld.Add(
            entity,
            PhysicsBody2D.Static());

        _ecsWorld.Add(
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
            ref _ecsWorld.Get<Collider2D>(
                entity);

        collider.IsTrigger =
            true;

        return entity;
    }

    private EntityId CreatePolygonObstacle(
        FixedVector2 position)
    {
        var entity =
            _ecsWorld.CreateEntity();

        _ecsWorld.Add(
            entity,
            new WorldTransform2D(
                position));

        _ecsWorld.Add(
            entity,
            PhysicsBody2D.Static());

        _ecsWorld.Add(
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
                            Fixed32.FromFloat(0.5f),
                            Fixed32.One),

                        new FixedVector2(
                            -Fixed32.FromFloat(0.5f),
                            Fixed32.One)
                    })));

        _entities.Add(entity);

        return entity;
    }

    private EntityId CreateDynamicCrate(
        FixedVector2 position)
    {
        var entity =
            _ecsWorld.CreateEntity();

        _ecsWorld.Add(
            entity,
            new WorldTransform2D(
                position));

        var body =
            PhysicsBody2D.Dynamic(
                Fixed32.One);

        body.GravityScale =
            Fixed32.Zero;

        _ecsWorld.Add(
            entity,
            body);

        _ecsWorld.Add(
            entity,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.One,
                        Fixed32.One))));

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

    private void ClearEntities()
    {
        foreach (var entity in _entities)
        {
            if (_ecsWorld.Exists(entity))
            {
                _ecsWorld.DestroyEntity(entity);
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
        _movementSubscription.Dispose();
        _physicsSubscription.Dispose();

        ClearEntities();
    }
}