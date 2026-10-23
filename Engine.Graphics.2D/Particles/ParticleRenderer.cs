using Engine.Camera;
using Engine.Content;
using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Graphics;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Engine.Graphics.Sprites;
using Engine.Graphics2D.Sprites;
using Engine.Particles.Particles;

namespace Engine.Graphics2D.Particles;

public sealed class ParticleRenderer : IDisposable
{
    private readonly Camera2D _camera;
    private readonly SpriteRenderer _renderer;
    private readonly TextureResourceManager _resources;

    private bool _disposed;

    public ParticleRenderer(
        IGraphicsDevice graphics,
        IContentManager content,
        Camera2D camera)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(camera);

        _camera = camera;

        _renderer = new SpriteRenderer(graphics);

        _resources = new TextureResourceManager(
            content,
            graphics.Textures);
    }

    public void Render(
        ParticleSystem particleSystem,
        string texturePath,
        int layer = RenderLayers.World)
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);

        ArgumentNullException.ThrowIfNull(
            particleSystem);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            texturePath);

        var texture = _resources.Load(
            new AssetPath(texturePath));

        foreach (var particle in particleSystem.Particles)
        {
            if (!float.IsFinite(particle.Position.X) ||
                !float.IsFinite(particle.Position.Y) ||
                !float.IsFinite(particle.Size) ||
                particle.Size <= 0.0f)
            {
                continue;
            }

            var size = new Vector2(
                particle.Size,
                particle.Size);

            var sprite = new Sprite(
                texture,
                new Rectangle(
                    0.0f,
                    0.0f,
                    1.0f,
                    1.0f),
                size,
                layer);

            // Position частицы обозначает её центр.
            var drawPosition = new Vector2(
                particle.Position.X - size.X * 0.5f,
                particle.Position.Y - size.Y * 0.5f);

            _renderer.DrawWorld(
                sprite,
                drawPosition,
                _camera);
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