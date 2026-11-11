using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;

public interface IGraphicsDevice : IDisposable
{
    ITextureManager Textures { get; }

    IFontManager Fonts { get; }
    IGraphicsBufferManager Buffers { get; }
    IShaderManager Shaders { get; }
    IRenderTargetManager RenderTargets { get; }
    RenderPipeline Pipeline { get; }

    void BeginFrame();

    void Resize(int width, int height)
    {
        Resize(width, height, width, height);
    }

    void Resize(
        int logicalWidth,
        int logicalHeight,
        int framebufferWidth,
        int framebufferHeight);

    void Submit(IRenderCommand command);

    void Submit(DrawWorldTextureData command)
    {
        throw new NotSupportedException(
            "This graphics device does not support direct world texture submission.");
    }

    void EndFrame();
}