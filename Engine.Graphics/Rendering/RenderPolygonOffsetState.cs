namespace Engine.Graphics.Rendering;

public readonly record struct RenderPolygonOffsetState(
    bool Enabled,
    float Factor,
    float Units)
{
    public static RenderPolygonOffsetState Disabled =>
        new(false, 0f, 0f);
}