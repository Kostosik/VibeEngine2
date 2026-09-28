using Engine.Graphics;
using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.Graphics.Resources;
using Engine.Worlds;
using Engine.Worlds.Spatial;
using Engine.Worlds.Tiles;

using World = Engine.Worlds.World;

namespace Engine.Tests.WorldTests;

public sealed class SandboxTilemapRendererTests
{
    private sealed class TestGraphicsDevice :
        IGraphicsDevice
    {
        public IGraphicsBufferManager Buffers => throw new NotSupportedException();
        public IRenderTargetManager RenderTargets => throw new NotSupportedException();
        public IShaderManager Shaders => throw new NotSupportedException();
        public IFontManager Fonts =>
    throw new NotSupportedException();
        public ITextureManager Textures =>
            throw new NotSupportedException();

        public List<IRenderCommand> Commands { get; } = new();

        public void BeginFrame()
        {
        }

        public void Submit(
            IRenderCommand command)
        {
            Commands.Add(command);
        }

        public void EndFrame()
        {
        }
    }
}