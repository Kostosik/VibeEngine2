using Engine.Camera;
using Engine.Content;
using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Sprites;

namespace Engine.Graphics.Sprites;

public sealed class SpriteRenderSystem : IDisposable
{
    private readonly World _world;
    private readonly Camera2D _camera;
    private readonly SpriteRenderer _renderer;
    private readonly TextureResourceManager _resources;

    private bool _disposed;

    public SpriteRenderSystem(
        World world,
        IGraphicsDevice graphics,
        IContentManager content,
        Camera2D camera)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(camera);

        _world = world;
        _camera = camera;

        _renderer = new SpriteRenderer(graphics);

        _resources = new TextureResourceManager(
            content,
            graphics.Textures);
    }

    public void Render()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);

        foreach (var item in
                 _world.QueryReadOnly<SpriteComponent, WorldTransform2D>())
        {
            var component = item.First;
            var transform = item.Second;

            // Спрайт без пути к текстуре не отрисовывается.
            if (string.IsNullOrWhiteSpace(component.TexturePath))
            {
                continue;
            }

            if (!float.IsFinite(component.Width) ||
                !float.IsFinite(component.Height) ||
                component.Width <= 0.0f ||
                component.Height <= 0.0f)
            {
                continue;
            }

            var scaleX =
                MathF.Abs(transform.Scale.X.ToFloat());

            var scaleY =
                MathF.Abs(transform.Scale.Y.ToFloat());

            var width =
                component.Width * scaleX;

            var height =
                component.Height * scaleY;

            if (!float.IsFinite(width) ||
                !float.IsFinite(height) ||
                width <= 0.0f ||
                height <= 0.0f)
            {
                continue;
            }

            var texture =
                _resources.Load(
                    new AssetPath(component.TexturePath));

            var flipX =
    transform.Scale.X < Fixed32.Zero;

            var flipY =
                transform.Scale.Y < Fixed32.Zero;

            var uv = new Rectangle(
                flipX ? 1.0f : 0.0f,
                flipY ? 1.0f : 0.0f,
                flipX ? -1.0f : 1.0f,
                flipY ? -1.0f : 1.0f);

            var sprite =
                new Sprite(
                    texture,
                    uv,
                    new Vector2(width, height),
                    component.Layer);

            var position =
                transform.Position;

            // Позиция Transform трактуется как центр спрайта.
            // Команда отрисовки принимает верхний левый угол.
            var drawPosition =
                new Vector2(
                    position.X.ToFloat() - width * 0.5f,
                    position.Y.ToFloat() - height * 0.5f);

            var rotationDegrees =
                transform.Rotation.ToFloat() *
                (180.0f / MathF.PI);

            _renderer.DrawWorld(
                sprite,
                drawPosition,
                _camera,
                rotationDegrees);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _resources.Dispose();
        _disposed = true;
    }
}