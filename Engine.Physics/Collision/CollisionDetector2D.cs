using Engine.Core.Math;
using Engine.Physics.BroadPhase;
using Engine.Physics.Shapes;

namespace Engine.Physics.Collision;

public sealed class CollisionDetector2D
{
    public bool TryDetect(
        PhysicsColliderProxy first,
        PhysicsColliderProxy second,
        out CollisionManifold manifold)
    {
        manifold = default;

        if (!first.Collider.Enabled ||
            !second.Collider.Enabled)
        {
            return false;
        }

        if (!first.Collider.CanCollideWith(
                second.Collider))
        {
            return false;
        }

        if (!first.Bounds.Intersects(
                second.Bounds))
        {
            return false;
        }

        var firstPosition =
            first.Bounds.Center;

        var secondPosition =
            second.Bounds.Center;

        var firstShape =
            first.Collider.Shape;

        var secondShape =
            second.Collider.Shape;

        return (firstShape.Type, secondShape.Type) switch
        {
            (PhysicsShapeType.Aabb, PhysicsShapeType.Aabb) =>
                TryDetectAabbAabb(
                    first,
                    second,
                    firstPosition,
                    secondPosition,
                    out manifold),

            (PhysicsShapeType.Circle, PhysicsShapeType.Circle) =>
                TryDetectCircleCircle(
                    first,
                    second,
                    firstPosition,
                    secondPosition,
                    out manifold),

            (PhysicsShapeType.Circle, PhysicsShapeType.Aabb) =>
                TryDetectCircleAabb(
                    first,
                    second,
                    firstPosition,
                    secondPosition,
                    out manifold),

            (PhysicsShapeType.Aabb, PhysicsShapeType.Circle) =>
                TryDetectAabbCircle(
                    first,
                    second,
                    firstPosition,
                    secondPosition,
                    out manifold),

            (PhysicsShapeType.Polygon, PhysicsShapeType.Aabb) =>
                TryDetectPolygonAabb(
                    first,
                    second,
                    out manifold),

            (PhysicsShapeType.Aabb, PhysicsShapeType.Polygon) =>
                TryDetectAabbPolygon(
                    first,
                    second,
                    out manifold),

            (PhysicsShapeType.Circle, PhysicsShapeType.Polygon) =>
            TryDetectCirclePolygon(
            first,
            second,
            out manifold),

            (PhysicsShapeType.Polygon, PhysicsShapeType.Circle) =>
             TryDetectPolygonCircle(
              first,
              second,
              out manifold),

            (PhysicsShapeType.Polygon, PhysicsShapeType.Polygon) =>
  TryDetectPolygonPolygon(
      first,
      second,
      out manifold),

            _ =>
                false
        };
    }

    private static bool TryDetectPolygonPolygon(
    PhysicsColliderProxy first,
    PhysicsColliderProxy second,
    out CollisionManifold manifold)
    {
        manifold = default;

        var firstPolygon =
            first.Collider.Shape.Polygon;

        var secondPolygon =
            second.Collider.Shape.Polygon;

        var firstLocalBounds =
            firstPolygon.GetBounds(
                FixedVector2.Zero);

        var secondLocalBounds =
            secondPolygon.GetBounds(
                FixedVector2.Zero);

        var firstTranslation =
            first.Bounds.Min -
            firstLocalBounds.Min;

        var secondTranslation =
            second.Bounds.Min -
            secondLocalBounds.Min;

        var firstCenter =
            first.Bounds.Center;

        var secondCenter =
            second.Bounds.Center;

        var centerDelta =
            secondCenter -
            firstCenter;

        var bestPenetration =
            Fixed32.Zero;

        var bestAxis =
            FixedVector2.Zero;

        var hasBestAxis =
            false;

        if (!EvaluatePolygonAxes(
                firstPolygon,
                firstTranslation,
                secondPolygon,
                secondTranslation,
                centerDelta,
                ref bestPenetration,
                ref bestAxis,
                ref hasBestAxis))
        {
            return false;
        }

        if (!EvaluatePolygonAxes(
                secondPolygon,
                secondTranslation,
                firstPolygon,
                firstTranslation,
                -centerDelta,
                ref bestPenetration,
                ref bestAxis,
                ref hasBestAxis))
        {
            return false;
        }

        OrientAxis(
            ref bestAxis,
            centerDelta);

        var firstSupport =
            GetPolygonSupport(
                firstPolygon,
                firstTranslation,
                bestAxis);

        var secondSupport =
            GetPolygonSupport(
                secondPolygon,
                secondTranslation,
                -bestAxis);

        var contactPoint =
            (firstSupport + secondSupport) *
            Fixed32.FromRatio(
                1,
                2);

        manifold =
            new CollisionManifold(
                new CollisionPair(
                    first.Entity,
                    second.Entity),

                new ContactPoint(
                    contactPoint,
                    bestAxis,
                    bestPenetration));

        return true;
    }

    private static bool EvaluatePolygonAxes(
        PolygonShape2D first,
        FixedVector2 firstTranslation,
        PolygonShape2D second,
        FixedVector2 secondTranslation,
        FixedVector2 centerDelta,
        ref Fixed32 bestPenetration,
        ref FixedVector2 bestAxis,
        ref bool hasBestAxis)
    {
        for (var i = 0;
             i < first.VertexCount;
             i++)
        {
            var nextIndex =
                (i + 1) %
                first.VertexCount;

            var current =
                first.GetVertex(i) +
                firstTranslation;

            var next =
                first.GetVertex(nextIndex) +
                firstTranslation;

            var edge =
                next -
                current;

            var axis =
                new FixedVector2(
                    -edge.Y,
                    edge.X);

            var length =
                axis.Length();

            if (length == Fixed32.Zero)
            {
                continue;
            }

            axis /=
                length;

            var firstProjection =
                ProjectPolygon(
                    first,
                    firstTranslation,
                    axis);

            var secondProjection =
                ProjectPolygon(
                    second,
                    secondTranslation,
                    axis);

            var overlap =
                Fixed32.Min(
                    firstProjection.Max,
                    secondProjection.Max) -
                Fixed32.Max(
                    firstProjection.Min,
                    secondProjection.Min);

            if (overlap < Fixed32.Zero)
            {
                return false;
            }

            if (!hasBestAxis ||
                overlap < bestPenetration)
            {
                bestPenetration =
                    overlap;

                bestAxis =
                    axis;

                hasBestAxis =
                    true;
            }
        }

        return true;
    }

    private static bool TryDetectCirclePolygon(
    PhysicsColliderProxy first,
    PhysicsColliderProxy second,
    out CollisionManifold manifold)
    {
        manifold = default;

        var circle =
            first.Collider.Shape.Circle;

        var polygon =
            second.Collider.Shape.Polygon;

        var circleCenter =
            first.Bounds.Center;

        var polygonBounds =
            polygon.GetBounds(
                FixedVector2.Zero);

        var polygonTranslation =
            second.Bounds.Min -
            polygonBounds.Min;

        var bestPenetration =
            Fixed32.Zero;

        var bestAxis =
            FixedVector2.Zero;

        var hasBestAxis =
            false;

        for (var i = 0;
             i < polygon.VertexCount;
             i++)
        {
            var nextIndex =
                (i + 1) %
                polygon.VertexCount;

            var current =
                polygon.GetVertex(i) +
                polygonTranslation;

            var next =
                polygon.GetVertex(nextIndex) +
                polygonTranslation;

            var edge =
                next -
                current;

            var axis =
                new FixedVector2(
                    -edge.Y,
                    edge.X);

            var length =
                axis.Length();

            if (length == Fixed32.Zero)
            {
                continue;
            }

            axis /=
                length;

            var polygonProjection =
                ProjectPolygon(
                    polygon,
                    polygonTranslation,
                    axis);

            var circleProjection =
                ProjectCircle(
                    circleCenter,
                    circle.Radius,
                    axis);

            var overlap =
                Fixed32.Min(
                    polygonProjection.Max,
                    circleProjection.Max) -
                Fixed32.Max(
                    polygonProjection.Min,
                    circleProjection.Min);

            if (overlap < Fixed32.Zero)
            {
                return false;
            }

            if (!hasBestAxis ||
                overlap < bestPenetration)
            {
                bestPenetration =
                    overlap;

                bestAxis =
                    axis;

                hasBestAxis =
                    true;
            }
        }

        var closestPoint =
            FindClosestPointOnPolygon(
                polygon,
                polygonTranslation,
                circleCenter);

        var circleAxis =
            closestPoint -
            circleCenter;

        var circleAxisLength =
            circleAxis.Length();

        if (circleAxisLength != Fixed32.Zero)
        {
            circleAxis /=
                circleAxisLength;

            var polygonProjection =
                ProjectPolygon(
                    polygon,
                    polygonTranslation,
                    circleAxis);

            var circleProjection =
                ProjectCircle(
                    circleCenter,
                    circle.Radius,
                    circleAxis);

            var overlap =
                Fixed32.Min(
                    polygonProjection.Max,
                    circleProjection.Max) -
                Fixed32.Max(
                    polygonProjection.Min,
                    circleProjection.Min);

            if (overlap < Fixed32.Zero)
            {
                return false;
            }

            if (!hasBestAxis ||
                overlap < bestPenetration)
            {
                bestPenetration =
                    overlap;

                bestAxis =
                    circleAxis;

                hasBestAxis =
                    true;
            }
        }

        var centerDelta =
            new FixedVector2(
                second.Bounds.Center.X -
                circleCenter.X,
                second.Bounds.Center.Y -
                circleCenter.Y);

        OrientAxis(
            ref bestAxis,
            centerDelta);

        var circleContact =
            circleCenter +
            bestAxis *
            circle.Radius;

        var polygonContact =
            GetPolygonSupport(
                polygon,
                polygonTranslation,
                -bestAxis);

        var contactPoint =
            (circleContact + polygonContact) *
            Fixed32.FromRatio(
                1,
                2);

        manifold =
            new CollisionManifold(
                new CollisionPair(
                    first.Entity,
                    second.Entity),

                new ContactPoint(
                    contactPoint,
                    bestAxis,
                    bestPenetration));

        return true;
    }

    private readonly record struct Projection(
    Fixed32 Min,
    Fixed32 Max);

    private static Projection ProjectPolygon(
        PolygonShape2D polygon,
        FixedVector2 translation,
        FixedVector2 axis)
    {
        var firstVertex =
            polygon.GetVertex(0) +
            translation;

        var firstProjection =
            firstVertex.Dot(axis);

        var minimum =
            firstProjection;

        var maximum =
            firstProjection;

        for (var i = 1;
             i < polygon.VertexCount;
             i++)
        {
            var vertex =
                polygon.GetVertex(i) +
                translation;

            var projection =
                vertex.Dot(axis);

            minimum =
                Fixed32.Min(
                    minimum,
                    projection);

            maximum =
                Fixed32.Max(
                    maximum,
                    projection);
        }

        return new Projection(
            minimum,
            maximum);
    }

    private static Projection ProjectCircle(
        FixedVector2 center,
        Fixed32 radius,
        FixedVector2 axis)
    {
        var centerProjection =
            center.Dot(axis);

        return new Projection(
            centerProjection - radius,
            centerProjection + radius);
    }

    private static FixedVector2 FindClosestPointOnPolygon(
        PolygonShape2D polygon,
        FixedVector2 translation,
        FixedVector2 point)
    {
        var bestPoint =
            polygon.GetVertex(0) +
            translation;

        var bestDistanceSquared =
            FixedVector2.DistanceSquared(
                bestPoint,
                point);

        for (var i = 0;
             i < polygon.VertexCount;
             i++)
        {
            var nextIndex =
                (i + 1) %
                polygon.VertexCount;

            var start =
                polygon.GetVertex(i) +
                translation;

            var end =
                polygon.GetVertex(nextIndex) +
                translation;

            var edge =
                end -
                start;

            var edgeLengthSquared =
                edge.LengthSquared();

            if (edgeLengthSquared == Fixed32.Zero)
            {
                continue;
            }

            var amount =
                (point - start).Dot(edge) /
                edgeLengthSquared;

            amount =
                Fixed32.Clamp(
                    amount,
                    Fixed32.Zero,
                    Fixed32.One);

            var candidate =
                start +
                edge *
                amount;

            var distanceSquared =
                FixedVector2.DistanceSquared(
                    candidate,
                    point);

            if (distanceSquared < bestDistanceSquared)
            {
                bestDistanceSquared =
                    distanceSquared;

                bestPoint =
                    candidate;
            }
        }

        return bestPoint;
    }

    private static bool TryDetectPolygonCircle(
        PhysicsColliderProxy first,
        PhysicsColliderProxy second,
        out CollisionManifold manifold)
    {
        if (!TryDetectCirclePolygon(
                second,
                first,
                out var circleManifold))
        {
            manifold = default;
            return false;
        }

        manifold =
            new CollisionManifold(
                new CollisionPair(
                    first.Entity,
                    second.Entity),

            new ContactPoint(
                circleManifold.Contact.Position,
                -circleManifold.Contact.Normal,
                circleManifold.Contact.Penetration));

        return true;
    }

    private static bool TryDetectPolygonAabb(
    PhysicsColliderProxy first,
    PhysicsColliderProxy second,
    out CollisionManifold manifold)
    {
        manifold = default;

        var polygon =
            first.Collider.Shape.Polygon;

        var polygonLocalBounds =
            polygon.GetBounds(
                FixedVector2.Zero);

        var polygonTranslation =
            first.Bounds.Min -
            polygonLocalBounds.Min;

        var boxBounds =
            second.Bounds;

        var bestPenetration =
            Fixed32.Zero;

        var bestAxis =
            FixedVector2.Zero;

        var hasBestAxis =
            false;

        var delta =
            boxBounds.Center -
            first.Bounds.Center;

        if (!TryFindMinimumSeparatingAxis(
                polygon,
                polygonTranslation,
                boxBounds,
                delta,
                ref bestPenetration,
                ref bestAxis,
                ref hasBestAxis))
        {
            return false;
        }

        OrientAxis(
            ref bestAxis,
            delta);

        var polygonSupport =
            GetPolygonSupport(
                polygon,
                polygonTranslation,
                bestAxis);

        var boxSupport =
            GetAabbSupport(
                boxBounds,
                -bestAxis);

        var contactPoint =
            (polygonSupport + boxSupport) *
            Fixed32.FromRatio(
                1,
                2);

        manifold =
            new CollisionManifold(
                new CollisionPair(
                    first.Entity,
                    second.Entity),

                new ContactPoint(
                    contactPoint,
                    bestAxis,
                    bestPenetration));

        return true;
    }

    private static bool TryDetectAabbPolygon(
        PhysicsColliderProxy first,
        PhysicsColliderProxy second,
        out CollisionManifold manifold)
    {
        if (!TryDetectPolygonAabb(
                second,
                first,
                out var polygonManifold))
        {
            manifold = default;
            return false;
        }

        manifold =
            new CollisionManifold(
                new CollisionPair(
                    first.Entity,
                    second.Entity),

                new ContactPoint(
                    polygonManifold.Contact.Position,
                    -polygonManifold.Contact.Normal,
                    polygonManifold.Contact.Penetration));

        return true;
    }

    private static bool TryFindMinimumSeparatingAxis(
        PolygonShape2D polygon,
        FixedVector2 polygonTranslation,
        FixedBounds2 boxBounds,
        FixedVector2 centerDelta,
        ref Fixed32 bestPenetration,
        ref FixedVector2 bestAxis,
        ref bool hasBestAxis)
    {
        var axes =
            new List<FixedVector2>(
                polygon.VertexCount + 2)
            {
            new FixedVector2(
                Fixed32.One,
                Fixed32.Zero),

            new FixedVector2(
                Fixed32.Zero,
                Fixed32.One)
            };

        for (var i = 0;
             i < polygon.VertexCount;
             i++)
        {
            var nextIndex =
                (i + 1) %
                polygon.VertexCount;

            var current =
                polygon.GetVertex(i) +
                polygonTranslation;

            var next =
                polygon.GetVertex(nextIndex) +
                polygonTranslation;

            var edge =
                next -
                current;

            var axis =
                new FixedVector2(
                    -edge.Y,
                    edge.X);

            var length =
                axis.Length();

            if (length == Fixed32.Zero)
            {
                continue;
            }

            axes.Add(
                axis / length);
        }

        foreach (var axis in axes)
        {
            var firstVertex =
                polygon.GetVertex(0) +
                polygonTranslation;

            var firstProjection =
                firstVertex.Dot(axis);

            var polygonMin =
                firstProjection;

            var polygonMax =
                firstProjection;

            for (var i = 0;
                 i < polygon.VertexCount;
                 i++)
            {
                var vertex =
                    polygon.GetVertex(i) +
                    polygonTranslation;

                var projection =
                    vertex.Dot(axis);

                polygonMin =
                    Fixed32.Min(
                        polygonMin,
                        projection);

                polygonMax =
                    Fixed32.Max(
                        polygonMax,
                        projection);
            }

            var boxCenter =
                boxBounds.Center;

            var halfSize =
                boxBounds.Size *
                Fixed32.FromRatio(
                    1,
                    2);

            var boxRadius =
                Abs(axis.X) *
                halfSize.X +
                Abs(axis.Y) *
                halfSize.Y;

            var boxProjection =
                boxCenter.Dot(axis);

            var boxMin =
                boxProjection -
                boxRadius;

            var boxMax =
                boxProjection +
                boxRadius;

            var overlap =
                Fixed32.Min(
                    polygonMax,
                    boxMax) -
                Fixed32.Max(
                    polygonMin,
                    boxMin);

            if (overlap < Fixed32.Zero)
            {
                return false;
            }

            if (!hasBestAxis ||
                overlap < bestPenetration)
            {
                bestPenetration =
                    overlap;

                bestAxis =
                    axis;

                hasBestAxis =
                    true;
            }
        }

        return true;
    }

    private static FixedVector2 GetPolygonSupport(
        PolygonShape2D polygon,
        FixedVector2 translation,
        FixedVector2 direction)
    {
        var best =
            polygon.GetVertex(0) +
            translation;

        var bestProjection =
            best.Dot(direction);

        for (var i = 1;
             i < polygon.VertexCount;
             i++)
        {
            var vertex =
                polygon.GetVertex(i) +
                translation;

            var projection =
                vertex.Dot(direction);

            if (projection > bestProjection)
            {
                bestProjection =
                    projection;

                best =
                    vertex;
            }
        }

        return best;
    }

    private static FixedVector2 GetAabbSupport(
        FixedBounds2 bounds,
        FixedVector2 direction)
    {
        return new FixedVector2(
            direction.X >= Fixed32.Zero
                ? bounds.Max.X
                : bounds.Min.X,

            direction.Y >= Fixed32.Zero
                ? bounds.Max.Y
                : bounds.Min.Y);
    }

    private static void OrientAxis(
        ref FixedVector2 axis,
        FixedVector2 centerDelta)
    {
        if (axis.Dot(centerDelta) < Fixed32.Zero)
        {
            axis =
                -axis;
        }
    }

    private static Fixed32 Abs(
        Fixed32 value)
    {
        return value < Fixed32.Zero
            ? -value
            : value;
    }

    private static bool TryDetectAabbAabb(
        PhysicsColliderProxy first,
        PhysicsColliderProxy second,
        FixedVector2 firstPosition,
        FixedVector2 secondPosition,
        out CollisionManifold manifold)
    {
        manifold = default;

        var firstBounds =
            first.Bounds;

        var secondBounds =
            second.Bounds;

        var overlapX =
            Fixed32.Min(
                firstBounds.Max.X,
                secondBounds.Max.X) -
            Fixed32.Max(
                firstBounds.Min.X,
                secondBounds.Min.X);

        var overlapY =
            Fixed32.Min(
                firstBounds.Max.Y,
                secondBounds.Max.Y) -
            Fixed32.Max(
                firstBounds.Min.Y,
                secondBounds.Min.Y);

        if (overlapX < Fixed32.Zero ||
            overlapY < Fixed32.Zero)
        {
            return false;
        }

        var delta =
            secondPosition -
            firstPosition;

        FixedVector2 normal;
        Fixed32 penetration;

        if (overlapX < overlapY)
        {
            normal =
                delta.X < Fixed32.Zero
                    ? new FixedVector2(
                        Fixed32.FromInt(-1),
                        Fixed32.Zero)
                    : new FixedVector2(
                        Fixed32.One,
                        Fixed32.Zero);

            penetration =
                overlapX;
        }
        else
        {
            normal =
                delta.Y < Fixed32.Zero
                    ? new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.FromInt(-1))
                    : new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.One);

            penetration =
                overlapY;
        }

        var contactMin =
            new FixedVector2(
                Fixed32.Max(
                    firstBounds.Min.X,
                    secondBounds.Min.X),
                Fixed32.Max(
                    firstBounds.Min.Y,
                    secondBounds.Min.Y));

        var contactMax =
            new FixedVector2(
                Fixed32.Min(
                    firstBounds.Max.X,
                    secondBounds.Max.X),
                Fixed32.Min(
                    firstBounds.Max.Y,
                    secondBounds.Max.Y));

        var contactPoint =
            (contactMin + contactMax) *
            Fixed32.FromRatio(
                1,
                2);

        manifold =
            new CollisionManifold(
                new CollisionPair(
                    first.Entity,
                    second.Entity),
                new ContactPoint(
                    contactPoint,
                    normal,
                    penetration));

        return true;
    }

    private static bool TryDetectCircleCircle(
        PhysicsColliderProxy first,
        PhysicsColliderProxy second,
        FixedVector2 firstPosition,
        FixedVector2 secondPosition,
        out CollisionManifold manifold)
    {
        manifold = default;

        var firstCircle =
            first.Collider.Shape.Circle;

        var secondCircle =
            second.Collider.Shape.Circle;

        var delta =
            secondPosition -
            firstPosition;

        var distanceSquared =
            delta.LengthSquared();

        var radiusSum =
            firstCircle.Radius +
            secondCircle.Radius;

        var radiusSumSquared =
            radiusSum *
            radiusSum;

        if (distanceSquared > radiusSumSquared)
        {
            return false;
        }

        FixedVector2 normal;
        Fixed32 distance;

        if (distanceSquared == Fixed32.Zero)
        {
            normal =
                new FixedVector2(
                    Fixed32.One,
                    Fixed32.Zero);

            distance =
                Fixed32.Zero;
        }
        else
        {
            distance =
                Fixed32.Sqrt(
                    distanceSquared);

            normal =
                delta /
                distance;
        }

        var penetration =
            radiusSum -
            distance;

        var firstContact =
            firstPosition +
            normal *
            firstCircle.Radius;

        var secondContact =
            secondPosition -
            normal *
            secondCircle.Radius;

        var contactPoint =
            (firstContact + secondContact) *
            Fixed32.FromRatio(
                1,
                2);

        manifold =
            new CollisionManifold(
                new CollisionPair(
                    first.Entity,
                    second.Entity),
                new ContactPoint(
                    contactPoint,
                    normal,
                    penetration));

        return true;
    }

    private static bool TryDetectCircleAabb(
        PhysicsColliderProxy first,
        PhysicsColliderProxy second,
        FixedVector2 circlePosition,
        FixedVector2 aabbPosition,
        out CollisionManifold manifold)
    {
        manifold = default;

        var circle =
            first.Collider.Shape.Circle;

        var bounds =
            second.Bounds;

        var closest =
            FixedVector2.Clamp(
                circlePosition,
                bounds.Min,
                bounds.Max);

        var delta =
            closest -
            circlePosition;

        var distanceSquared =
            delta.LengthSquared();

        if (distanceSquared > circle.Radius * circle.Radius)
        {
            return false;
        }

        FixedVector2 normal;
        Fixed32 penetration;
        FixedVector2 contactPoint;

        if (distanceSquared != Fixed32.Zero)
        {
            var distance =
                Fixed32.Sqrt(
                    distanceSquared);

            normal =
                delta /
                distance;

            penetration =
                circle.Radius -
                distance;

            contactPoint =
                closest;
        }
        else
        {
            var left =
                circlePosition.X -
                bounds.Min.X;

            var right =
                bounds.Max.X -
                circlePosition.X;

            var bottom =
                circlePosition.Y -
                bounds.Min.Y;

            var top =
                bounds.Max.Y -
                circlePosition.Y;

            var minimum =
                Fixed32.Min(
                    Fixed32.Min(left, right),
                    Fixed32.Min(bottom, top));

            if (minimum == left)
            {
                normal =
                    new FixedVector2(
                        Fixed32.FromInt(-1),
                        Fixed32.Zero);

                contactPoint =
                    circlePosition.WithX(
                        bounds.Min.X);
            }
            else if (minimum == right)
            {
                normal =
                    new FixedVector2(
                        Fixed32.One,
                        Fixed32.Zero);

                contactPoint =
                    circlePosition.WithX(
                        bounds.Max.X);
            }
            else if (minimum == bottom)
            {
                normal =
                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.FromInt(-1));

                contactPoint =
                    circlePosition.WithY(
                        bounds.Min.Y);
            }
            else
            {
                normal =
                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.One);

                contactPoint =
                    circlePosition.WithY(
                        bounds.Max.Y);
            }

            penetration =
                circle.Radius +
                minimum;
        }

        manifold =
            new CollisionManifold(
                new CollisionPair(
                    first.Entity,
                    second.Entity),
                new ContactPoint(
                    contactPoint,
                    normal,
                    penetration));

        return true;
    }

    private static bool TryDetectAabbCircle(
        PhysicsColliderProxy first,
        PhysicsColliderProxy second,
        FixedVector2 aabbPosition,
        FixedVector2 circlePosition,
        out CollisionManifold manifold)
    {
        if (!TryDetectCircleAabb(
                second,
                first,
                circlePosition,
                aabbPosition,
                out var circleManifold))
        {
            manifold = default;
            return false;
        }

        manifold =
            new CollisionManifold(
                new CollisionPair(
                    first.Entity,
                    second.Entity),
                new ContactPoint(
                    circleManifold.Contact.Position,
                    -circleManifold.Contact.Normal,
                    circleManifold.Contact.Penetration));

        return true;
    }
}