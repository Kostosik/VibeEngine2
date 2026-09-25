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
    //[Fact]
    //public void RenderSubmitsCommandsForNonEmptyTiles()
    //{
    //    var ecsWorld =
    //        new Engine.ECS.World();

    //    var world =
    //        new Engine.Worlds.World(
    //            new ChunkSize(2, 2),
    //            ecsWorld);

    //    var chunk =
    //        world.CreateChunk(
    //            new ChunkPosition(0, 0));

    //    chunk.Tiles.Set(
    //        new LocalPosition(0, 0),
    //        new Tile(1));

    //    chunk.Tiles.Set(
    //        new LocalPosition(1, 1),
    //        new Tile(2));

    //    var graphics =
    //        new TestGraphicsDevice();

    //    var renderer =
    //        new Game.Sandbox.Rendering.SandboxTilemapRenderer(
    //            world,
    //            graphics,
    //            new (1),
    //            1.0f);

    //    renderer.Render();

    //    Assert.Equal(
    //        2,
    //        graphics.Commands.Count);

    //    Assert.All(
    //        graphics.Commands,
    //        command =>
    //            Assert.IsType<
    //                DrawWorldTextureCommand>(
    //                command));
    //}

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
}