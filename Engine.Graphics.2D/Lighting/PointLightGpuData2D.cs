using Engine.Core.Math;
using Engine.Graphics2D.Rendering;

namespace Engine.Graphics2D.Lighting;

/// <summary>
/// Point-light parameters transformed into lighting-buffer coordinates.
/// </summary>
public readonly record struct PointLightGpuData2D(
    Vector2 ScreenPosition,
    Vector2 ScreenRadius,
    Vector3 Color,
    float Intensity)
{
    public static PointLightGpuData2D From(
    in Light2D light,
    in LightingContext2D context)
    {
        if (!light.IsEnabled)
        {
            throw new ArgumentException(
                "The point light must be enabled.",
                nameof(light));
        }

        var transform =
            new CameraRenderTargetTransform2D(
                context.Camera,
                context.LogicalViewportWidth,
                context.LogicalViewportHeight);

        var position =
            transform.WorldToTargetBottomLeft(
                light.Position);

        var radius =
            transform.WorldSizeToTarget(
                new Vector2(light.Radius, light.Radius));

        if (!float.IsFinite(position.X) ||
            !float.IsFinite(position.Y) ||
            !float.IsFinite(radius.X) ||
            !float.IsFinite(radius.Y) ||
            radius.X <= 0.0f ||
            radius.Y <= 0.0f)
        {
            throw new InvalidOperationException(
                "Point-light screen parameters are invalid.");
        }

        return new PointLightGpuData2D(
            position,
            radius,
            light.Color,
            light.Intensity);
    }
}