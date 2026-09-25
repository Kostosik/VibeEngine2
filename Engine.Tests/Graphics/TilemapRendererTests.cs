using Engine.Graphics;
using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.Graphics.Resources;
using Engine.Graphics.Tilemaps;

namespace Engine.Tests.Graphics;

public sealed class TilemapRendererTests
{
    [Fact]
    public void RenderChunkSubmitsCommandsForNonEmptyTiles()
    {
        var graphics =
            new TestGraphicsDevice();

        var atlas =
            new TextureAtlas(
                new TextureHandle(1),
                new TextureDescription(
                    64,
                    32,
                    TextureFormat.Rgba8),
                32,
                32);

        var camera =
            new Engine.Graphics.Cameras.Camera(
                new Engine.Core.Math.Vector2(
                    64,
                    64));

        var renderer =
            new TilemapRenderer(
                graphics,
                camera,
                atlas,
                1.0f);

        var tiles =
            new uint[]
            {
                1, 0,
                0, 2
            };

        renderer.RenderChunk(
            0,
            0,
            2,
            2,
            tiles);

        Assert.Equal(
            2,
            graphics.Commands.Count);

        Assert.All(
            graphics.Commands,
            command =>
                Assert.IsType<
                    DrawWorldTextureCommand>(
                    command));
    }

    private sealed class TestGraphicsDevice :
        IGraphicsDevice
    {
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

    [Fact]
    public void RenderChunkIgnoresChunkOutsideCamera()
    {
        var graphics =
            new TestGraphicsDevice();

        var camera =
            new Engine.Graphics.Cameras.Camera(
                new Engine.Core.Math.Vector2(
                    64,
                    64));

        camera.Position =
            new Engine.Core.Math.Vector2(
                100,
                100);

        var atlas =
            new TextureAtlas(
                new TextureHandle(1),
                new TextureDescription(
                    32,
                    32,
                    TextureFormat.Rgba8),
                32,
                32);

        var renderer =
            new TilemapRenderer(
                graphics,
                camera,
                atlas,
                1.0f);

        var tiles =
            new uint[]
            {
            1, 1,
            1, 1
            };

        renderer.RenderChunk(
            0,
            0,
            2,
            2,
            tiles);

        Assert.Empty(
            graphics.Commands);
    }
}