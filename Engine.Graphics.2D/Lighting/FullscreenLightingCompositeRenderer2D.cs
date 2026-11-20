using Engine.Graphics.Rendering;
using Engine.Graphics.Rendering.RenderGraph;
using Engine.Graphics.Resources;

namespace Engine.Graphics2D.Lighting;

public sealed class FullscreenLightingCompositeRenderer2D :
    ILightingCompositeRenderer2D
{
    private readonly ShaderHandle _shader;

    private readonly Action<IShaderParameterWriter>? _applyParameters;

    public FullscreenLightingCompositeRenderer2D(
        ShaderHandle shader,
        Action<IShaderParameterWriter>? applyParameters = null)
    {
        if (!shader.IsValid)
        {
            throw new ArgumentException(
                "A valid composite shader is required.",
                nameof(shader));
        }

        _shader = shader;
        _applyParameters = applyParameters;
    }

    public void Composite(
        in LightingCompositeContext2D context,
        RenderGraphContext renderGraphContext)
    {
        ArgumentNullException.ThrowIfNull(renderGraphContext);

        var pass = new FullscreenPassDescription(
            _shader,
            _applyParameters,
            [
                new FullscreenPassTextureInput(
                    LightingCompositeShaderSource2D.SceneColor,
                    context.SceneColor),

                new FullscreenPassTextureInput(
                    LightingCompositeShaderSource2D.DirectLighting,
                    context.DirectLighting),

                new FullscreenPassTextureInput(
                    LightingCompositeShaderSource2D.AmbientLighting,
                    context.AmbientLighting),

                new FullscreenPassTextureInput(
                    LightingCompositeShaderSource2D.IndirectLighting,
                    context.IndirectLighting),

                new FullscreenPassTextureInput(
                    LightingCompositeShaderSource2D.Emission,
                    context.Emission)
            ]);

        renderGraphContext.ExecuteFullscreen(pass);
    }
}