using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Resource information retained by a compiled graph.
/// FirstUseIndex and LastUseIndex refer to positions in the compiled pass list.
/// Imported resources are never owned or destroyed by the graph.
/// </summary>
public sealed record RenderGraphCompiledResource(
    RenderGraphResourceHandle Handle,
    string Name,
    RenderGraphResourceKind Kind,
    RenderGraphResourceDescriptor Descriptor,
    RenderTargetHandle ImportedRenderTarget,
    int FirstUseIndex,
    int LastUseIndex,
    bool IsOutput);
