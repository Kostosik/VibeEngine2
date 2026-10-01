using Engine.Core.Math;
using Engine.Physics.Collision;

namespace Engine.Physics.BroadPhase;

public sealed class SpatialHashBroadPhase :
    IPhysicsBroadPhase
{
    private readonly Fixed32 _cellSize;

    private readonly Dictionary<
        GridCell,
        List<int>> _cells = new();

    private readonly HashSet<CollisionPair>
        _candidatePairs = new();

    public SpatialHashBroadPhase(
        Fixed32 cellSize)
    {
        if (cellSize <= Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cellSize),
                "Cell size must be greater than zero.");
        }

        _cellSize =
            cellSize;
    }

    public Fixed32 CellSize =>
        _cellSize;

    public void FindPairs(
        IReadOnlyList<PhysicsColliderProxy> colliders,
        List<CollisionPair> pairs)
    {
        ArgumentNullException.ThrowIfNull(colliders);
        ArgumentNullException.ThrowIfNull(pairs);

        pairs.Clear();
        _cells.Clear();
        _candidatePairs.Clear();

        for (var index = 0;
             index < colliders.Count;
             index++)
        {
            if (!colliders[index]
                    .Collider
                    .Enabled)
            {
                continue;
            }

            Insert(
                index,
                colliders[index].Bounds);
        }

        foreach (var cell in _cells.Values)
        {
            for (var i = 0;
                 i < cell.Count - 1;
                 i++)
            {
                var firstIndex =
                    cell[i];

                var first =
                    colliders[firstIndex];

                for (var j = i + 1;
                     j < cell.Count;
                     j++)
                {
                    var secondIndex =
                        cell[j];

                    var second =
                        colliders[secondIndex];

                    if (!first.Collider.CanCollideWith(
                            second.Collider))
                    {
                        continue;
                    }

                    if (!first.Bounds.Intersects(
                            second.Bounds))
                    {
                        continue;
                    }

                    _candidatePairs.Add(
                        CreatePair(
                            first.Entity,
                            second.Entity));
                }
            }
        }

        pairs.AddRange(
            _candidatePairs);

        pairs.Sort(
            static (left, right) =>
            {
                var result =
                    left.First.Index.CompareTo(
                        right.First.Index);

                if (result != 0)
                {
                    return result;
                }

                result =
                    left.First.Generation.CompareTo(
                        right.First.Generation);

                if (result != 0)
                {
                    return result;
                }

                result =
                    left.Second.Index.CompareTo(
                        right.Second.Index);

                if (result != 0)
                {
                    return result;
                }

                return left.Second.Generation.CompareTo(
                    right.Second.Generation);
            });
    }

    private void Insert(
        int colliderIndex,
        FixedBounds2 bounds)
    {
        var minimum =
            ToCell(
                bounds.Min);

        var maximum =
            ToCell(
                bounds.Max);

        for (var x = minimum.X;
             x <= maximum.X;
             x++)
        {
            for (var y = minimum.Y;
                 y <= maximum.Y;
                 y++)
            {
                var cell =
                    new GridCell(
                        x,
                        y);

                if (!_cells.TryGetValue(
                        cell,
                        out var indices))
                {
                    indices =
                        new List<int>();

                    _cells.Add(
                        cell,
                        indices);
                }

                indices.Add(
                    colliderIndex);
            }
        }
    }

    private GridCell ToCell(
        FixedVector2 position)
    {
        return new GridCell(
            (position.X / _cellSize)
                .FloorToInt(),
            (position.Y / _cellSize)
                .FloorToInt());
    }

    private static CollisionPair CreatePair(
        Engine.ECS.Entities.EntityId first,
        Engine.ECS.Entities.EntityId second)
    {
        if (Compare(
                first,
                second) <= 0)
        {
            return new CollisionPair(
                first,
                second);
        }

        return new CollisionPair(
            second,
            first);
    }

    private static int Compare(
        Engine.ECS.Entities.EntityId first,
        Engine.ECS.Entities.EntityId second)
    {
        var result =
            first.Index.CompareTo(
                second.Index);

        if (result != 0)
        {
            return result;
        }

        return first.Generation.CompareTo(
            second.Generation);
    }

    private readonly record struct GridCell(
        int X,
        int Y);
}