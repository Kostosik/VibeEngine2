using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;

namespace Engine.Graphics.Sprites;

public sealed class SpriteRenderer
{
    private readonly IGraphicsDevice _graphics;

    public SpriteRenderer(
        IGraphicsDevice graphics)
    {
        ArgumentNullException.ThrowIfNull(
            graphics);

        _graphics = graphics;
    }

    public void Draw(
        Sprite sprite,
        Vector2 position)
    {
        if (!sprite.Texture.IsValid)
        {
            return;
        }

        _graphics.Submit(
            new DrawTextureCommand(
                sprite.Texture,
                position,
                sprite.Size,
                sprite.UV,
                sprite.Layer));
    }

    public void DrawWorld(
        Sprite sprite,
        Vector2 position)
    {
        if (!sprite.Texture.IsValid)
        {
            return;
        }

        _graphics.Submit(
            new DrawWorldTextureCommand(
                sprite.Texture,
                position,
                sprite.Size,
                sprite.UV,
                sprite.Layer));
    }
}