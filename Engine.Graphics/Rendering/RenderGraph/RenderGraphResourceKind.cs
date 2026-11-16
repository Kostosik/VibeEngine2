namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Defines how a logical resource is backed at execution time.
/// </summary>
public enum RenderGraphResourceKind
{
    Transient,
    ImportedRenderTarget,
    Backbuffer
}
