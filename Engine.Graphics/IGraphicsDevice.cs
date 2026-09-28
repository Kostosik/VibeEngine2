using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;

namespace Engine.Graphics;

public interface IGraphicsDevice
{
    ITextureManager Textures { get; }

    IFontManager Fonts { get; }
    IGraphicsBufferManager Buffers { get; }
    IShaderManager Shaders { get; }
    IRenderTargetManager RenderTargets { get; }
    RenderPipeline Pipeline { get; }
    void BeginFrame();

    void Submit(IRenderCommand command);

    void EndFrame();
}