using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering;

/// <summary>
/// Backend-neutral description of a fullscreen operation.
/// Input and output resources are managed by the render graph.
/// </summary>
public sealed class FullscreenPassDescription
{
    public FullscreenPassDescription(
        ShaderHandle shader,
        Action<IShaderParameterWriter>? applyParameters = null)
    {
        if (!shader.IsValid)
        {
            throw new ArgumentException(
                "A valid shader handle is required.",
                nameof(shader));
        }

        Shader = shader;
        ApplyParameters = applyParameters;
    }

    public ShaderHandle Shader { get; }

    public Action<IShaderParameterWriter>? ApplyParameters { get; }
}