using Engine.Camera;
using Engine.Core.Math;
using Engine.Graphics;
using Engine.Graphics.Commands;
using Engine.Graphics2D.Commands;

namespace Engine.Graphics2D.Sprites;

public sealed class SpriteRenderer
{
    private readonly IGraphicsDevice _graphics;
    private readonly IWorldTextureDataSubmitter?
    _worldTextureDataSubmitter;
    public SpriteRenderer(
        IGraphicsDevice graphics)
    {
        ArgumentNullException.ThrowIfNull(
            graphics);

        _graphics = graphics;
        _worldTextureDataSubmitter =
    graphics as IWorldTextureDataSubmitter;
    }

    public void DrawWorld(
        Sprite sprite,
        Vector2 position,
        Camera2D camera,
        float rotationDegrees = 0.0f)
    {
        ArgumentNullException.ThrowIfNull(camera);

        if (!sprite.Texture.IsValid)
        {
            return;
        }

        var bounds =
            new Rectangle(
                position.X,
                position.Y,
                sprite.Size.X,
                sprite.Size.Y);

        if (!camera.IsVisible(bounds))
        {
            return;
        }

        SubmitWorld(
            sprite,
            position,
            rotationDegrees);
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
        Vector2 position,
        float rotationDegrees = 0.0f)
    {
        if (!sprite.Texture.IsValid)
        {
            return;
        }

        if (_worldTextureDataSubmitter is not null)
        {
            _worldTextureDataSubmitter.Submit(
                new DrawWorldTextureData(
                    sprite.Texture,
                    position,
                    sprite.Size,
                    sprite.UV,
                    sprite.Layer,
                    rotationDegrees));

            return;
        }

        _graphics.Submit(
            new DrawWorldTextureCommand(
                sprite.Texture,
                position,
                sprite.Size,
                sprite.UV,
                sprite.Layer,
                rotationDegrees));
    }

    private void SubmitWorld(
    Sprite sprite,
    Vector2 position,
    float rotationDegrees)
    {
        if (_worldTextureDataSubmitter is not null)
        {
            _worldTextureDataSubmitter.Submit(
                new DrawWorldTextureData(
                    sprite.Texture,
                    position,
                    sprite.Size,
                    sprite.UV,
                    sprite.Layer,
                    rotationDegrees));

            return;
        }

        // Совместимость с графическими устройствами,
        // которые пока не поддерживают прямую отправку.
        _graphics.Submit(
            new DrawWorldTextureCommand(
                sprite.Texture,
                position,
                sprite.Size,
                sprite.UV,
                sprite.Layer,
                rotationDegrees));
    }
}