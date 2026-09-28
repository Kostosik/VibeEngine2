using Engine.Core.Math;

namespace Engine.Editor.Viewport.Gizmos;

public sealed class EditorTranslationGizmo
{
    public const float DefaultArmLength = 48.0f;
    public const float DefaultHitRadius = 8.0f;

    public EditorTranslationGizmo(
        float armLength = DefaultArmLength,
        float hitRadius = DefaultHitRadius)
    {
        if (armLength <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(armLength));
        }

        if (hitRadius <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hitRadius));
        }

        ArmLength = armLength;
        HitRadius = hitRadius;
    }

    public float ArmLength { get; }

    public float HitRadius { get; }

    public EditorGizmoAxis HitTest(
        Vector2 screenPosition,
        Vector2 origin)
    {
        var xAxisStart = origin;
        var xAxisEnd =
            new Vector2(
                origin.X + ArmLength,
                origin.Y);

        if (DistanceToSegmentSquared(
                screenPosition,
                xAxisStart,
                xAxisEnd) <=
            HitRadius * HitRadius)
        {
            return EditorGizmoAxis.X;
        }

        var yAxisStart = origin;
        var yAxisEnd =
            new Vector2(
                origin.X,
                origin.Y - ArmLength);

        if (DistanceToSegmentSquared(
                screenPosition,
                yAxisStart,
                yAxisEnd) <=
            HitRadius * HitRadius)
        {
            return EditorGizmoAxis.Y;
        }

        return EditorGizmoAxis.None;
    }

    private static float DistanceToSegmentSquared(
        Vector2 point,
        Vector2 start,
        Vector2 end)
    {
        var segment =
            end - start;

        var lengthSquared =
            segment.X * segment.X +
            segment.Y * segment.Y;

        if (lengthSquared <= 0.000001f)
        {
            var dx =
                point.X - start.X;

            var dy =
                point.Y - start.Y;

            return dx * dx + dy * dy;
        }

        var pointRelative =
            point - start;

        var projection =
            (pointRelative.X * segment.X +
             pointRelative.Y * segment.Y) /
            lengthSquared;

        projection =
            Math.Clamp(
                projection,
                0.0f,
                1.0f);

        var closest =
            start +
            segment * projection;

        var delta =
            point - closest;

        return delta.X * delta.X +
               delta.Y * delta.Y;
    }
}