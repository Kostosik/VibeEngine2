
using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Physics.BroadPhase;
using Engine.Physics.Components;

namespace Engine.Physics.Queries;

internal sealed class PhysicsQuerySpatialIndex2D
{
    private readonly World _world;
    private readonly Fixed32 _cellSize = Fixed32.FromInt(2);

    private readonly Dictionary<GridCell, List<PhysicsColliderProxy>>
        _cells = new();

    private readonly Stack<List<PhysicsColliderProxy>>
        _cellListPool = new();

    private readonly List<PhysicsColliderProxy>
        _candidates = new();

    private readonly HashSet<EntityId>
        _candidateEntities = new();

    private ulong _colliderVersion;
    private ulong _transformVersion;
    private bool _initialized;

    public PhysicsQuerySpatialIndex2D(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        _world = world;
    }

    public List<PhysicsColliderProxy> QueryBounds(
        FixedBounds2 bounds)
    {
        EnsureUpToDate();

        _candidates.Clear();
        _candidateEntities.Clear();

        var minimum = ToCell(bounds.Min);
        var maximum = ToCell(bounds.Max);

        for (var x = minimum.X; x <= maximum.X; x++)
        {
            for (var y = minimum.Y; y <= maximum.Y; y++)
            {
                var cell = new GridCell(x, y);

                if (!_cells.TryGetValue(cell, out var colliders))
                {
                    continue;
                }

                foreach (var collider in colliders)
                {
                    if (_candidateEntities.Add(collider.Entity))
                    {
                        _candidates.Add(collider);
                    }
                }
            }
        }

        return _candidates;
    }

    private void EnsureUpToDate()
    {
        var colliderVersion =
            _world.GetWriteAccessVersion<Collider2D>();

        var transformVersion =
            _world.GetWriteAccessVersion<WorldTransform2D>();

        if (_initialized &&
            colliderVersion == _colliderVersion &&
            transformVersion == _transformVersion)
        {
            return;
        }

        Rebuild();

        _colliderVersion =
            _world.GetWriteAccessVersion<Collider2D>();

        _transformVersion =
            _world.GetWriteAccessVersion<WorldTransform2D>();

        _initialized = true;
    }

    private void Rebuild()
    {
        foreach (var colliders in _cells.Values)
        {
            colliders.Clear();
            _cellListPool.Push(colliders);
        }

        _cells.Clear();

        foreach (var item in _world.QueryReadOnly<Collider2D>())
        {
            var collider = item.Component;

            if (!collider.Enabled)
            {
                continue;
            }

            var entity = item.Entity;

            if (!_world.Has<WorldTransform2D>(entity))
            {
                continue;
            }

            ref readonly var transform =
                ref _world.GetReadOnly<WorldTransform2D>(entity);

            var position = collider.GetWorldPosition(
                transform.Position,
                transform.Rotation);

            var bounds = collider.GetWorldBounds(
                transform.Position,
                transform.Rotation);

            var proxy = new PhysicsColliderProxy(
                entity,
                bounds,
                collider)
            {
                WorldPosition = position,
                WorldRotation = transform.Rotation
            };

            Insert(proxy);
        }
    }

    private void Insert(PhysicsColliderProxy collider)
    {
        var minimum = ToCell(collider.Bounds.Min);
        var maximum = ToCell(collider.Bounds.Max);

        for (var x = minimum.X; x <= maximum.X; x++)
        {
            for (var y = minimum.Y; y <= maximum.Y; y++)
            {
                var cell = new GridCell(x, y);

                if (!_cells.TryGetValue(cell, out var colliders))
                {
                    colliders = _cellListPool.Count > 0
                        ? _cellListPool.Pop()
                        : new List<PhysicsColliderProxy>();

                    colliders.Clear();
                    _cells.Add(cell, colliders);
                }

                colliders.Add(collider);
            }
        }
    }

    private GridCell ToCell(FixedVector2 position)
    {
        var halfCellSize =
            _cellSize * Fixed32.FromRatio(1, 2);

        return new GridCell(
            ((position.X + halfCellSize) / _cellSize).FloorToInt(),
            ((position.Y + halfCellSize) / _cellSize).FloorToInt());
    }

    private readonly record struct GridCell(int X, int Y);
}
