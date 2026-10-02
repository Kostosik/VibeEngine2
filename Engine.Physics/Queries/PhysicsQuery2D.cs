using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Physics.BroadPhase;
using Engine.Physics.Collision;
using Engine.Physics.Components;
using Engine.Physics.Shapes;

namespace Engine.Physics.Queries;

public sealed class PhysicsQuery2D
{
    private readonly World _world;

    public PhysicsQuery2D(
        World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;
    }

    public bool Raycast(
        FixedVector2 origin,
        FixedVector2 direction,
        Fixed32 maxDistance,
        out PhysicsRaycastHit2D hit,
        uint layerMask = uint.MaxValue,
        bool includeTriggers = false)
    {
        if (maxDistance <= Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxDistance),
                "Maximum raycast distance must be greater than zero.");
        }

        var directionLength =
            direction.Length();

        if (directionLength == Fixed32.Zero)
        {
            throw new ArgumentException(
                "Raycast direction must not be zero.",
                nameof(direction));
        }

        direction /=
            directionLength;

        var found =
            false;

        var closestDistance =
            maxDistance;

        hit = default;

        foreach (var item
                 in _world.Query<Collider2D>())
        {
            var entity =
                item.Entity;

            ref var collider =
                ref item.Component;

            if (!collider.Enabled)
            {
                continue;
            }

            if ((collider.CollisionLayer &
                 layerMask) == 0)
            {
                continue;
            }

            if (!includeTriggers &&
                collider.IsTrigger)
            {
                continue;
            }

            if (!_world.Has<WorldTransform2D>(
                    entity))
            {
                continue;
            }

            ref var transform =
                ref _world.Get<WorldTransform2D>(
                    entity);

            var position =
                collider.GetWorldPosition(
                    transform.Position,
                    transform.Rotation);

            if (!TryRaycastShape(
                    collider.Shape,
                    position,
                    transform.Rotation,
                    origin,
                    direction,
                    closestDistance,
                    out var distance,
                    out var normal))
            {
                continue;
            }

            var point =
                origin +
                direction *
                distance;

            hit =
                new PhysicsRaycastHit2D(
                    entity,
                    point,
                    normal,
                    distance);

            closestDistance =
                distance;

            found =
                true;
        }

        return found;
    }

    private static bool TryRaycastShape(
        PhysicsShape2D shape,
        FixedVector2 position,
        Fixed32 rotation,
        FixedVector2 origin,
        FixedVector2 direction,
        Fixed32 maxDistance,
        out Fixed32 distance,
        out FixedVector2 normal)
    {
        return shape.Type switch
        {
            PhysicsShapeType.Aabb =>
                TryRaycastAabb(
                    shape.Aabb,
                    position,
                    origin,
                    direction,
                    maxDistance,
                    out distance,
                    out normal),

            PhysicsShapeType.Circle =>
                TryRaycastCircle(
                    shape.Circle,
                    position,
                    origin,
                    direction,
                    maxDistance,
                    out distance,
                    out normal),

            PhysicsShapeType.Polygon =>
                TryRaycastPolygon(
                    shape.Polygon,
                    position,
                    rotation,
                    origin,
                    direction,
                    maxDistance,
                    out distance,
                    out normal),

            _ =>
                throw new InvalidOperationException(
                    $"Unsupported physics shape type '{shape.Type}'.")
        };
    }

    private static bool TryRaycastAabb(
        AabbShape2D shape,
        FixedVector2 position,
        FixedVector2 origin,
        FixedVector2 direction,
        Fixed32 maxDistance,
        out Fixed32 distance,
        out FixedVector2 normal)
    {
        var bounds =
            shape.GetBounds(
                position);

        var tMin =
            Fixed32.Zero;

        var tMax =
            maxDistance;

        var entryNormal =
            -direction;

        if (!ClipAxis(
                origin.X,
                direction.X,
                bounds.Min.X,
                bounds.Max.X,
                new FixedVector2(
                    Fixed32.FromInt(-1),
                    Fixed32.Zero),
                new FixedVector2(
                    Fixed32.One,
                    Fixed32.Zero),
                ref tMin,
                ref tMax,
                ref entryNormal))
        {
            distance = default;
            normal = default;
            return false;
        }

        if (!ClipAxis(
                origin.Y,
                direction.Y,
                bounds.Min.Y,
                bounds.Max.Y,
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromInt(-1)),
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.One),
                ref tMin,
                ref tMax,
                ref entryNormal))
        {
            distance = default;
            normal = default;
            return false;
        }

        if (tMax < Fixed32.Zero ||
            tMin > maxDistance)
        {
            distance = default;
            normal = default;
            return false;
        }

        if (tMin == Fixed32.Zero)
        {
            distance =
                Fixed32.Zero;

            normal =
                -direction;

            return true;
        }

        distance =
            tMin;

        normal =
            entryNormal;

        return true;
    }

    private static bool ClipAxis(
        Fixed32 origin,
        Fixed32 direction,
        Fixed32 minimum,
        Fixed32 maximum,
        FixedVector2 minimumNormal,
        FixedVector2 maximumNormal,
        ref Fixed32 tMin,
        ref Fixed32 tMax,
        ref FixedVector2 entryNormal)
    {
        if (direction == Fixed32.Zero)
        {
            return origin >= minimum &&
                   origin <= maximum;
        }

        var inverse =
            Fixed32.One /
            direction;

        var first =
            (minimum - origin) *
            inverse;

        var second =
            (maximum - origin) *
            inverse;

        var firstNormal =
            minimumNormal;

        var secondNormal =
            maximumNormal;

        if (first > second)
        {
            (first, second) =
                (second, first);

            (firstNormal, secondNormal) =
                (secondNormal, firstNormal);
        }

        if (first > tMin)
        {
            tMin =
                first;

            entryNormal =
                firstNormal;
        }

        tMax =
            Fixed32.Min(
                tMax,
                second);

        return tMin <= tMax;
    }

    private static bool TryRaycastCircle(
        CircleShape2D shape,
        FixedVector2 position,
        FixedVector2 origin,
        FixedVector2 direction,
        Fixed32 maxDistance,
        out Fixed32 distance,
        out FixedVector2 normal)
    {
        var offset =
            origin -
            position;

        var projection =
            offset.Dot(
                direction);

        var discriminant =
            projection * projection -
            (offset.LengthSquared() -
             shape.Radius * shape.Radius);

        if (discriminant < Fixed32.Zero)
        {
            distance = default;
            normal = default;
            return false;
        }

        var root =
            Fixed32.Sqrt(
                discriminant);

        var first =
            -projection -
            root;

        var second =
            -projection +
            root;

        Fixed32 candidate;

        if (first >= Fixed32.Zero)
        {
            candidate =
                first;
        }
        else if (second >= Fixed32.Zero)
        {
            candidate =
                second;
        }
        else
        {
            distance = default;
            normal = default;
            return false;
        }

        if (candidate > maxDistance)
        {
            distance = default;
            normal = default;
            return false;
        }

        var point =
            origin +
            direction *
            candidate;

        normal =
            (point - position).Normalize();

        if (normal == FixedVector2.Zero)
        {
            normal =
                -direction;
        }

        distance =
            candidate;

        return true;
    }

    private static bool TryRaycastPolygon(
        PolygonShape2D shape,
        FixedVector2 position,
        Fixed32 rotation,
        FixedVector2 origin,
        FixedVector2 direction,
        Fixed32 maxDistance,
        out Fixed32 distance,
        out FixedVector2 normal)
    {
        var tEnter =
            Fixed32.Zero;

        var tExit =
            maxDistance;

        var enterNormal =
            -direction;

        var inside =
            true;

        for (var i = 0;
             i < shape.VertexCount;
             i++)
        {
            var nextIndex =
                (i + 1) %
                shape.VertexCount;

            var start =
                Rotate(
                    shape.GetVertex(i),
                    rotation) +
                position;

            var end =
                Rotate(
                    shape.GetVertex(nextIndex),
                    rotation) +
                position;

            var edge =
                end -
                start;

            var outwardNormal =
                new FixedVector2(
                    edge.Y,
                    -edge.X).Normalize();

            var side =
                Cross(
                    edge,
                    origin - start);

            var sideDirection =
                Cross(
                    edge,
                    direction);

            if (side < Fixed32.Zero)
            {
                inside = false;
            }

            if (sideDirection == Fixed32.Zero)
            {
                if (side < Fixed32.Zero)
                {
                    distance = default;
                    normal = default;
                    return false;
                }

                continue;
            }

            var t =
                -side /
                sideDirection;

            if (sideDirection > Fixed32.Zero)
            {
                if (t > tEnter)
                {
                    tEnter =
                        t;

                    enterNormal =
                        outwardNormal;
                }
            }
            else
            {
                tExit =
                    Fixed32.Min(
                        tExit,
                        t);
            }

            if (tEnter > tExit)
            {
                distance = default;
                normal = default;
                return false;
            }
        }

        if (inside)
        {
            distance =
                Fixed32.Zero;

            normal =
                -direction;

            return true;
        }

        if (tEnter < Fixed32.Zero ||
            tEnter > maxDistance)
        {
            distance = default;
            normal = default;
            return false;
        }

        distance =
            tEnter;

        normal =
            enterNormal;

        return true;
    }

    public void OverlapPoint(
    FixedVector2 point,
    List<EntityId> results,
    uint layerMask = uint.MaxValue,
    bool includeTriggers = false)
    {
        ArgumentNullException.ThrowIfNull(results);

        results.Clear();

        foreach (var item in
                 _world.Query<Collider2D>())
        {
            var entity =
                item.Entity;

            ref var collider =
                ref item.Component;

            if (!collider.Enabled)
            {
                continue;
            }

            if ((collider.CollisionLayer &
                 layerMask) == 0)
            {
                continue;
            }

            if (!includeTriggers &&
                collider.IsTrigger)
            {
                continue;
            }

            if (!_world.Has<WorldTransform2D>(
                    entity))
            {
                continue;
            }

            ref var transform =
                ref _world.Get<WorldTransform2D>(
                    entity);

            var position =
                collider.GetWorldPosition(
                    transform.Position,
                    transform.Rotation);

            if (ContainsPoint(
                    collider.Shape,
                    position,
                    transform.Rotation,
                    point))
            {
                results.Add(
                    entity);
            }
        }

        SortEntities(
            results);
    }

    public void OverlapBounds(
        FixedBounds2 bounds,
        List<EntityId> results,
        uint layerMask = uint.MaxValue,
        bool includeTriggers = false)
    {
        ArgumentNullException.ThrowIfNull(results);

        results.Clear();

        var queryCollider =
            new Collider2D(
                new AabbShape2D(
                    bounds.Size));

        queryCollider.CollisionLayer =
            uint.MaxValue;

        queryCollider.CollisionMask =
            uint.MaxValue;

        var queryProxy =
            new PhysicsColliderProxy(
                default,
                bounds,
                queryCollider);

        var detector =
            new CollisionDetector2D();

        foreach (var item in
                 _world.Query<Collider2D>())
        {
            var entity =
                item.Entity;

            ref var collider =
                ref item.Component;

            if (!collider.Enabled)
            {
                continue;
            }

            if ((collider.CollisionLayer &
                 layerMask) == 0)
            {
                continue;
            }

            if (!includeTriggers &&
                collider.IsTrigger)
            {
                continue;
            }

            if (!_world.Has<WorldTransform2D>(
                    entity))
            {
                continue;
            }

            ref var transform =
                ref _world.Get<WorldTransform2D>(
                    entity);

            var position =
                transform.Position +
                collider.Offset;

            var colliderBounds =
                collider.GetWorldBounds(
                    transform.Position,
                    transform.Rotation);

            if (!colliderBounds.Intersects(
                    bounds))
            {
                continue;
            }

            var colliderProxy =
                new PhysicsColliderProxy(
                    entity,
                    colliderBounds,
                    collider)
                {
                    WorldPosition =
                        position,

                    WorldRotation =
                        transform.Rotation
                };

            if (detector.TryDetect(
                    colliderProxy,
                    queryProxy,
                    out _))
            {
                results.Add(
                    entity);
            }
        }

        SortEntities(
            results);
    }

    private static bool ContainsPoint(
    PhysicsShape2D shape,
    FixedVector2 position,
    Fixed32 rotation,
    FixedVector2 point)
    {
        return shape.Type switch
        {
            PhysicsShapeType.Aabb =>
                shape.Aabb
                    .GetBounds(position)
                    .Contains(point),

            PhysicsShapeType.Circle =>
                shape.Circle
                    .ContainsPoint(
                        position,
                        point),

            PhysicsShapeType.Polygon =>
                ContainsPointPolygon(
                    shape.Polygon,
                    position,
                    rotation,
                    point),

            _ =>
                throw new InvalidOperationException(
                    $"Unsupported physics shape type '{shape.Type}'.")
        };
    }

    private static bool ContainsPointPolygon(
    PolygonShape2D polygon,
    FixedVector2 position,
    Fixed32 rotation,
    FixedVector2 point)
    {
        for (var i = 0;
             i < polygon.VertexCount;
             i++)
        {
            var nextIndex =
                (i + 1) %
                polygon.VertexCount;

            var current =
                Rotate(
                    polygon.GetVertex(i),
                    rotation) +
                position;

            var next =
                Rotate(
                    polygon.GetVertex(nextIndex),
                    rotation) +
                position;

            var edge =
                next -
                current;

            var toPoint =
                point -
                current;

            var cross =
                edge.X * toPoint.Y -
                edge.Y * toPoint.X;

            if (cross < Fixed32.Zero)
            {
                return false;
            }
        }

        return true;
    }

    private static void SortEntities(
        List<EntityId> entities)
    {
        entities.Sort(
            static (left, right) =>
                left.Index.CompareTo(
                    right.Index));
    }

    private static FixedVector2 Rotate(
    FixedVector2 value,
    Fixed32 angle)
    {
        var cosine =
            Fixed32.Cos(angle);

        var sine =
            Fixed32.Sin(angle);

        return new FixedVector2(
            value.X * cosine -
            value.Y * sine,

            value.X * sine +
            value.Y * cosine);
    }

    private static Fixed32 Cross(
        FixedVector2 left,
        FixedVector2 right)
    {
        return
            left.X * right.Y -
            left.Y * right.X;
    }
}