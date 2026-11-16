namespace Engine.Graphics.Rendering;

public readonly record struct RenderDepthState(
    bool TestEnabled,
    bool WriteEnabled,
    RenderDepthFunction Function)
{
    public static RenderDepthState Default =>
        new(
            TestEnabled: false,
            WriteEnabled: true,
            Function: RenderDepthFunction.Less);
}