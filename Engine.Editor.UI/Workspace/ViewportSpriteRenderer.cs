using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.Editor.Documents;
using Engine.Graphics.Commands;
using Engine.Graphics2D.Sprites;
using Engine.UI.Core;
using Engine.UI.Layout;
using Engine.Worlds.Spatial;

namespace Engine.Editor.UI.Workspace;

internal sealed class ViewportSpriteRenderer
{
    private readonly ViewportProjectTextureCache _textureCache;

    public ViewportSpriteRenderer(
        ViewportProjectTextureCache textureCache)
    {
        ArgumentNullException.ThrowIfNull(textureCache);

        _textureCache = textureCache;
    }

    public void Draw(
        UiRenderContext context,
        EditorDocument document,
        string? assetsRoot,
        UiRect viewportBounds)
    {
        if (string.IsNullOrWhiteSpace(assetsRoot) ||
            !_textureCache.IsAvailable)
        {
            if (_textureCache.HasAssetsRoot)
            {
                _textureCache.ReleaseResources();
            }

            return;
        }

        _textureCache.SetAssetsRoot(assetsRoot);

        var zoom = document.Viewport.State.Zoom;

        var sprites = document.World.EcsWorld
            .Query<SpriteComponent, WorldTransform2D>();

        foreach (var item in sprites)
        {
            var sprite = item.First;
            var transform = item.Second;

            if (string.IsNullOrWhiteSpace(sprite.TexturePath))
            {
                continue;
            }

            if (!float.IsFinite(sprite.Width) ||
                !float.IsFinite(sprite.Height) ||
                sprite.Width <= 0.0f ||
                sprite.Height <= 0.0f)
            {
                continue;
            }

            var scaleX = MathF.Abs(transform.Scale.X.ToFloat());
            var scaleY = MathF.Abs(transform.Scale.Y.ToFloat());

            var width = sprite.Width * scaleX;
            var height = sprite.Height * scaleY;

            if (!float.IsFinite(width) ||
                !float.IsFinite(height) ||
                width <= 0.0f ||
                height <= 0.0f)
            {
                continue;
            }

            var screenWidth = width * zoom;
            var screenHeight = height * zoom;

            var localScreen = document.Viewport.Transform.WorldToScreen(
                new Vector2(
                    transform.Position.X.ToFloat(),
                    transform.Position.Y.ToFloat()));

            var center = new Vector2(
                viewportBounds.X + localScreen.X,
                viewportBounds.Y + localScreen.Y);

            var destination = new UiRect(
                center.X - screenWidth * 0.5f,
                center.Y - screenHeight * 0.5f,
                screenWidth,
                screenHeight);

            if (!_textureCache.TryLoad(
                    sprite.TexturePath,
                    out var texture))
            {
                DrawTexturePlaceholder(
                    context,
                    destination,
                    sprite.Layer);

                continue;
            }

            var rotationDegrees =
    -transform.Rotation.ToFloat() *
    (180.0f / MathF.PI);

            var flipX =
                transform.Scale.X < Fixed32.Zero;

            var flipY =
                transform.Scale.Y < Fixed32.Zero;

            var uv = new Rectangle(
                flipX ? 1.0f : 0.0f,
                flipY ? 1.0f : 0.0f,
                flipX ? -1.0f : 1.0f,
                flipY ? -1.0f : 1.0f);

            context.PushRotation(
                center,
                rotationDegrees);

            try
            {
                context.DrawTexture(
                    texture,
                    destination,
                    uv,
                    layer: sprite.Layer);
            }
            finally
            {
                context.PopRotation();
            }
        }
    }

    private static void DrawTexturePlaceholder(
        UiRenderContext context,
        UiRect rect,
        int layer)
    {
        if (rect.Width < 2.0f ||
            rect.Height < 2.0f)
        {
            return;
        }

        context.DrawRectangle(
            rect,
            new UiColor(190, 65, 65, 220),
            filled: false,
            layer: layer);

        if (rect.Width >= 80.0f &&
            rect.Height >= 20.0f)
        {
            context.DrawText(
                "Texture unavailable",
                new Vector2(
                    rect.X + 4.0f,
                    rect.Y + 4.0f),
                10.0f,
                new UiColor(235, 150, 150, 255),
                layer: layer);
        }
    }
}