namespace Engine.Graphics.Rendering;

/// <summary>
/// Defines a rectangular rendering restriction.
/// Coordinates use framebuffer pixels with a bottom-left origin.
/// </summary>
public readonly record struct RenderScissorState(
    bool Enabled,
    int X,
    int Y,
    int Width,
    int Height)
{
    public static RenderScissorState Disabled =>
        new(false, 0, 0, 0, 0);
}