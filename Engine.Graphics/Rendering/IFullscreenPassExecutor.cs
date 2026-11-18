using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering;

/// <summary>
/// Backend-neutral executor for fullscreen rendering operations.
/// The output framebuffer must already be bound by the active render pass.
/// </summary>
public interface IFullscreenPassExecutor
{
    void ExecuteFullscreen(
        FullscreenPassDescription pass,
        TextureHandle source,
        RenderTargetDescription outputDescription);
}