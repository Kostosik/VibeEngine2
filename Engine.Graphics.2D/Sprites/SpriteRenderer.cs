
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

    public SpriteRenderer(IGraphicsDevice graphics)
    {
        ArgumentNullException.ThrowIfNull(graphics);

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
            return;

        if (!float.IsFinite(position.X) ||
            !float.IsFinite(position.Y) ||
            !float.IsFinite(sprite.Size.X) ||
            !float.IsFinite(sprite.Size.Y) ||
            !float.IsFinite(rotationDegrees))
        {
            return;
        }

        var bounds = GetRotatedBounds(
            position,
            sprite.Size,
            rotationDegrees);

        if (!float.IsFinite(bounds.X) ||
            !float.IsFinite(bounds.Y) ||
            !float.IsFinite(bounds.Width) ||
            !float.IsFinite(bounds.Height))
        {
            return;
        }

        if (!camera.IsVisible(bounds))
            return;

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
            return;

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
            return;

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

        _graphics.Submit(
            new DrawWorldTextureCommand(
                sprite.Texture,
                position,
                sprite.Size,
                sprite.UV,
                sprite.Layer,
                rotationDegrees));
    }

    private static Rectangle GetRotatedBounds(
        Vector2 position,
        Vector2 size,
        float rotationDegrees)
    {
        var width = MathF.Abs(size.X);
        var height = MathF.Abs(size.Y);

        var radians =
            (rotationDegrees % 360.0f) *
            (MathF.PI / 180.0f);

        var cos = MathF.Abs(MathF.Cos(radians));
        var sin = MathF.Abs(MathF.Sin(radians));

        var halfWidth =
            (cos * width + sin * height) * 0.5f;

        var halfHeight =
            (sin * width + cos * height) * 0.5f;

        // Вращение выполняется вокруг центра спрайта.
        var centerX = position.X + size.X * 0.5f;
        var centerY = position.Y + size.Y * 0.5f;

        return new Rectangle(
            centerX - halfWidth,
            centerY - halfHeight,
            halfWidth * 2.0f,
            halfHeight * 2.0f);
    }
}
