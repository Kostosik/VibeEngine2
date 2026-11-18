using Engine.Graphics.Rendering;

namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Provides information about the pipeline stage
/// where an extension is being inserted.
/// </summary>
public readonly record struct RenderGraphExtensionContext(
    int Width,
    int Height,
    int PipelinePassIndex,
    RenderPass PipelinePass,
    RenderGraphResourceHandle PipelineOutput,
    RenderGraphResourceHandle SceneColor,
    RenderGraphResourceHandle Backbuffer,
    RenderGraphPassHandle PreviousPass);