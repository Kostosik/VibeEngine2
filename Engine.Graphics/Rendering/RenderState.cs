namespace Engine.Graphics.Rendering;

public readonly record struct RenderState(
    RenderBlendState Blend,
    RenderDepthState Depth,
    RenderRasterizerState Rasterizer,
    RenderScissorState Scissor)
{
    // Совместимость со старым API.
    public RenderState(
        RenderBlendMode BlendMode,
        bool DepthTestEnabled,
        RenderCullMode CullMode)
        : this(
            new RenderBlendState(BlendMode),
            RenderDepthState.Default with
            {
                TestEnabled = DepthTestEnabled
            },
            RenderRasterizerState.Default with
            {
                CullMode = CullMode
            },
            RenderScissorState.Disabled)
    {
    }

    // Совместимость со старым API.
    public RenderBlendMode BlendMode
    {
        get => Blend.Mode;
        init => Blend = Blend with { Mode = value };
    }

    public bool DepthTestEnabled
    {
        get => Depth.TestEnabled;
        init => Depth = Depth with { TestEnabled = value };
    }

    public RenderCullMode CullMode
    {
        get => Rasterizer.CullMode;
        init => Rasterizer = Rasterizer with { CullMode = value };
    }

    public static RenderState Default =>
        new(
            RenderBlendState.Default,
            RenderDepthState.Default,
            RenderRasterizerState.Default, 
            RenderScissorState.Disabled);

    public static RenderState Default2D =>
        Default;
}