namespace Engine.Graphics.Resources;

public interface IRenderTargetManager
{
    RenderTargetHandle Create(
        RenderTargetDescription description);

    bool Exists(
        RenderTargetHandle target);

    RenderTargetDescription GetDescription(
        RenderTargetHandle target);

    TextureHandle GetColorTexture(
        RenderTargetHandle target);

    void Destroy(
        RenderTargetHandle target);

    void Resize(
    RenderTargetHandle target,
    int width,
    int height);
}