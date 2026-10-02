namespace Engine.Graphics.Rendering;

public readonly record struct RenderState(
    RenderBlendMode BlendMode,
    bool DepthTestEnabled,
    RenderCullMode CullMode)
{
    public static RenderState Default =>
        new(
            RenderBlendMode.Alpha,
            DepthTestEnabled: false,
            RenderCullMode.Disabled);

    public static RenderState Default2D =>
    Default;
}