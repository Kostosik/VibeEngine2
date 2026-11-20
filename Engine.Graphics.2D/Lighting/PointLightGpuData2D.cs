using Engine.Core.Math;

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

        var camera = context.Camera;
        var viewport = camera.ViewportSize;

        var scaleX =
            context.LogicalViewportWidth / viewport.X;

        var scaleY =
            context.LogicalViewportHeight / viewport.Y;

        var screenPosition = camera.WorldToScreen(
            light.Position);

        // Camera coordinates use a top-left origin.
        // Fullscreen texture coordinates use a bottom-left origin.
        var position = new Vector2(
            screenPosition.X * scaleX,
            context.LogicalViewportHeight -
            screenPosition.Y * scaleY);

        var radius = new Vector2(
            light.Radius * camera.Zoom * scaleX,
            light.Radius * camera.Zoom * scaleY);

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