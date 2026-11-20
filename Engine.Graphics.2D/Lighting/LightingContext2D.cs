using Engine.Camera;

namespace Engine.Graphics2D.Lighting;

public readonly record struct LightingContext2D
{
    public Camera2D Camera { get; }

    public int LogicalViewportWidth { get; }

    public int LogicalViewportHeight { get; }

    public LightingContext2D(
        Camera2D camera,
        int logicalViewportWidth,
        int logicalViewportHeight)
    {
        ArgumentNullException.ThrowIfNull(camera);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            logicalViewportWidth);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            logicalViewportHeight);

        Camera = camera;
        LogicalViewportWidth = logicalViewportWidth;
        LogicalViewportHeight = logicalViewportHeight;
    }
}