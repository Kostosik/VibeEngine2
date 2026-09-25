using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Abstraction;

public sealed class DrawTextureCommandTests
{
    [Fact]
    public void Constructor_StoresValues()
    {
        var texture =
            new TextureHandle(10);

        var position =
            new Vector2(100, 200);

        var size =
            new Vector2(64, 32);

        var uv =
            new Rectangle(
                0.25f,
                0.25f,
                0.5f,
                0.5f);

        var command =
            new DrawTextureCommand(
                texture,
                position,
                size,
                uv);

        Assert.Equal(
            texture,
            command.Texture);

        Assert.Equal(
            position,
            command.Position);

        Assert.Equal(
            size,
            command.Size);

        Assert.Equal(
            uv,
            command.UV);
    }

    [Fact]
    public void Command_ImplementsRenderCommand()
    {
        var command =
            new DrawTextureCommand(
                new TextureHandle(1),
                new Vector2(0, 0),
                new Vector2(1, 1),
                new Rectangle(0, 0, 1, 1));

        Assert.IsAssignableFrom<IRenderCommand>(
            command);
    }
}