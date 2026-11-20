using Engine.Graphics.Rendering.RenderGraph;

namespace Engine.Graphics2D.Lighting;

/// <summary>
/// Backend-neutral contract for rendering one 2D lighting channel.
/// </summary>
public interface ILightingPassRenderer2D
{
    void Render(
        in LightingPassContext2D context,
        RenderGraphContext renderGraphContext);
}