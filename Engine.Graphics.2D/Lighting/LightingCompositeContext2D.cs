using Engine.Graphics.Rendering.RenderGraph;

namespace Engine.Graphics2D.Lighting;

public readonly record struct LightingCompositeContext2D(
    RenderGraphResourceHandle SceneColor,
    RenderGraphResourceHandle DirectLighting,
    RenderGraphResourceHandle AmbientLighting,
    RenderGraphResourceHandle IndirectLighting,
    RenderGraphResourceHandle Emission,
    RenderGraphResourceHandle Output);