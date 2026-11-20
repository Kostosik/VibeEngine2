using Engine.Graphics.Rendering.RenderGraph;

namespace Engine.Graphics2D.Lighting;

public interface ILightingCompositeRenderer2D
{
    void Composite(
        in LightingCompositeContext2D context,
        RenderGraphContext renderGraphContext);
}