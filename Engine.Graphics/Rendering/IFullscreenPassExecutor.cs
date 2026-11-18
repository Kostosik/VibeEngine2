using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering;

/// <summary>
/// Backend-neutral executor for fullscreen rendering operations.
/// The output framebuffer must already be bound by the active render pass.
/// </summary>
public interface IFullscreenPassExecutor
{
    /// <summary>
    /// Legacy single-texture execution path.
    /// </summary>
    void ExecuteFullscreen(
        FullscreenPassDescription pass,
        TextureHandle source,
        RenderTargetDescription outputDescription);

    /// <summary>
    /// Executes a fullscreen pass using named texture bindings.
    /// Backends may override this method to support multiple inputs.
    /// </summary>
    void ExecuteFullscreen(
        FullscreenPassDescription pass,
        IReadOnlyList<FullscreenPassTextureBinding> inputs,
        RenderTargetDescription outputDescription)
    {
        ArgumentNullException.ThrowIfNull(pass);
        ArgumentNullException.ThrowIfNull(inputs);

        if (inputs.Count != 1)
        {
            throw new NotSupportedException(
                "The fullscreen backend does not support multiple texture inputs yet.");
        }

        var input = inputs[0];

        if (!input.Texture.IsValid)
        {
            throw new ArgumentException(
                "Fullscreen input texture is invalid.",
                nameof(inputs));
        }

        if (!string.Equals(
                input.Name,
                FullscreenShaderSource.SourceTextureUniform,
                StringComparison.Ordinal))
        {
            throw new NotSupportedException(
                $"The legacy fullscreen backend only supports " +
                $"'{FullscreenShaderSource.SourceTextureUniform}'.");
        }

        if (input.Sampler != TextureSamplerDescription.Default)
        {
            throw new NotSupportedException(
                "The legacy fullscreen backend does not support custom sampler settings.");
        }

        ExecuteFullscreen(
            pass,
            input.Texture,
            outputDescription);
    }
}