
using Engine.Camera;
using Engine.Core.Math;
using Engine.Graphics;
using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.Graphics.Materials;
using Engine.Graphics.Rendering;
using Engine.Graphics.Rendering.RenderGraph;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Commands;
using Engine.Graphics2D.Sprites;

namespace Engine.Tests.Graphics2D;

public sealed class SpriteRendererTests
{

    [Fact]
    public void Draw_PreservesMaterialAndAtlasUv()
    {
        var graphics = new TestGraphicsDevice();

        var material = new SpriteMaterial(
            new TextureHandle(1),
            new TextureHandle(2),
            new TextureHandle(3));

        var uv = new Rectangle(
            0.25f, 0.5f, 0.125f, 0.25f);

        var sprite = new Sprite(
            material,
            uv,
            new Vector2(32, 32),
            10);

        var renderer = new SpriteRenderer(graphics);

        renderer.Draw(sprite, new Vector2(10, 20));

        var command = Assert.IsType<DrawTextureCommand>(
            Assert.Single(graphics.Commands));

        Assert.Equal(material, command.Material);
        Assert.Equal(uv, command.UV);
    }

    [Fact]
    public void DrawWorld_PreservesMaterialWithoutDataSubmitter()
    {
        var graphics = new TestGraphicsDevice();

        var material = new SpriteMaterial(
            new TextureHandle(1),
            new TextureHandle(2),
            new TextureHandle(3));

        var sprite = new Sprite(
            material,
            new Rectangle(0.2f, 0.3f, 0.4f, 0.5f),
            new Vector2(32, 32));

        var renderer = new SpriteRenderer(graphics);

        renderer.DrawWorld(
            sprite,
            new Vector2(10, 20),
            45.0f);

        var command = Assert.IsType<DrawWorldTextureCommand>(
            Assert.Single(graphics.Commands));

        Assert.Equal(material, command.Material);
        Assert.Equal(sprite.UV, command.UV);
        Assert.Equal(45.0f, command.RotationDegrees);
    }

    [Fact]
    public void DrawWorld_RotatedSpriteIsVisibleWhenRotatedBoundsIntersectCamera()
    {
        var graphics = new TestGraphicsDevice();

        var camera = new Camera2D(
            new Vector2(100, 100));

        var sprite = new Sprite(
            new TextureHandle(1),
            new Rectangle(0, 0, 1, 1),
            new Vector2(2, 10));

        var renderer = new SpriteRenderer(graphics);

        // Исходный прямоугольник находится за границей камеры.
        // После поворота на 90 градусов он частично попадает на экран.
        renderer.DrawWorld(
            sprite,
            new Vector2(52, -5),
            camera,
            90.0f);

        var command = Assert.IsType<DrawWorldTextureCommand>(
            Assert.Single(graphics.Commands));

        Assert.Equal(90.0f, command.RotationDegrees);
    }

    [Fact]
    public void DrawWorld_UnrotatedSpriteOutsideCameraIsCulled()
    {
        var graphics = new TestGraphicsDevice();

        var camera = new Camera2D(
            new Vector2(100, 100));

        var sprite = new Sprite(
            new TextureHandle(1),
            new Rectangle(0, 0, 1, 1),
            new Vector2(2, 10));

        var renderer = new SpriteRenderer(graphics);

        renderer.DrawWorld(
            sprite,
            new Vector2(52, -5),
            camera,
            0.0f);

        Assert.Empty(graphics.Commands);
    }

    private sealed class TestGraphicsDevice : IGraphicsDevice
    {
        public IRenderGraphExtensionRegistry RenderGraphExtensions => new RenderGraphExtensionRegistry();
        public RenderPipeline Pipeline =>
            throw new NotSupportedException();

        public IFontManager Fonts =>
            throw new NotSupportedException();

        public IRenderTargetManager RenderTargets =>
            throw new NotSupportedException();

        public IShaderManager Shaders =>
            throw new NotSupportedException();

        public IGraphicsBufferManager Buffers =>
            throw new NotSupportedException();

        public ITextureManager Textures =>
            throw new NotSupportedException();

        public List<IRenderCommand> Commands { get; } = new();

        public void BeginFrame()
        {
        }

        public void Submit(IRenderCommand command)
        {
            Commands.Add(command);
        }

        public void EndFrame()
        {
        }
        public void Resize(int x,int y,int width,int height) { }
        public void Dispose() { }
    }
}
