namespace Engine.Graphics.Rendering;

public readonly record struct RenderRasterizerState(
    RenderCullMode CullMode,
    RenderFrontFace FrontFace,
    RenderPolygonMode PolygonMode,
    RenderPolygonOffsetState PolygonOffset)
{
    // Совместимость с прежним конструктором.
    public RenderRasterizerState(
        RenderCullMode cullMode,
        RenderFrontFace frontFace)
        : this(
            cullMode,
            frontFace,
            RenderPolygonMode.Fill,
            RenderPolygonOffsetState.Disabled)
    {
    }

    public static RenderRasterizerState Default =>
        new(
            RenderCullMode.Disabled,
            RenderFrontFace.CounterClockwise,
            RenderPolygonMode.Fill,
            RenderPolygonOffsetState.Disabled);
}