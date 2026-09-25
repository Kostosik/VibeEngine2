using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.Graphics.Resources;

namespace Engine.Graphics;

public interface IGraphicsDevice
{
    ITextureManager Textures { get; }

    IFontManager Fonts { get; }

    void BeginFrame();

    void Submit(IRenderCommand command);

    void EndFrame();
}