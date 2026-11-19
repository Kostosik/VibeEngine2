using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering;

public interface IFramePresentationExecutor
{
    RenderTargetHandle PresentationTarget { get; }

    void ExecutePresent(TextureHandle source);
}