using Engine.Graphics.Resources;
using Engine.Core.Math;

namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Describes how a pass accesses a logical render-graph resource.
/// </summary>
public enum RenderGraphResourceAccessKind
{
    Read,
    WriteColor
}

/// <summary>
/// One declared resource access made by a render-graph pass.
/// </summary>
public readonly record struct RenderGraphResourceAccess(
    RenderGraphResourceHandle Resource,
    RenderGraphResourceAccessKind Kind,
    RenderGraphLoadAction LoadAction,
    RenderGraphStoreAction StoreAction);

public readonly record struct RenderGraphDepthAttachment(
    RenderGraphLoadAction LoadAction,
    RenderGraphStoreAction StoreAction);

internal sealed record RenderGraphResourceDefinition(
    RenderGraphResourceHandle Handle,
    string Name,
    RenderGraphResourceKind Kind,
    RenderGraphResourceDescriptor Descriptor,
    RenderTargetHandle ImportedRenderTarget);

internal sealed record RenderGraphPassDefinition(
    RenderGraphPassHandle Handle,
    string Name,
    RenderState State,
    RenderPassLayerRange Layers,
    IReadOnlyList<RenderGraphResourceAccess> ResourceAccesses,
    IReadOnlyList<RenderGraphPassHandle> ExplicitDependencies,
    Action<RenderGraphContext>? Execute,
    bool ExecuteQueuedCommands,
    bool IsPostProcessSource,
    IRenderPassContextData? ContextData,
    bool HasSideEffects, RenderGraphDepthAttachment? DepthAttachment, Vector4 ClearColorValue);
