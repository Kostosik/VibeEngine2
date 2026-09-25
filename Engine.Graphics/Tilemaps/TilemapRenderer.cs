using Engine.Core.Math;
using Engine.Graphics.Cameras;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;

namespace Engine.Graphics.Tilemaps;

public sealed class TilemapRenderer
{
    private readonly IGraphicsDevice _graphics;
    private readonly Camera _camera;
    private readonly TextureAtlas _atlas;
    private readonly float _tileSize;

    public TilemapRenderer(
        IGraphicsDevice graphics,
        Camera camera,
        TextureAtlas atlas,
        float tileSize)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(atlas);

        if (tileSize <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tileSize));
        }

        _graphics = graphics;
        _camera = camera;
        _atlas = atlas;
        _tileSize = tileSize;
    }

    public void RenderChunk(
        int chunkX,
        int chunkY,
        int width,
        int height,
        ReadOnlySpan<uint> tiles)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height));
        }

        var expectedCount =
            checked(width * height);

        if (tiles.Length != expectedCount)
        {
            throw new ArgumentException(
                $"Expected {expectedCount} tiles, " +
                $"but received {tiles.Length}.",
                nameof(tiles));
        }

        var bounds =
            _camera.WorldBounds;

        var chunkWorldX =
            chunkX * width;

        var chunkWorldY =
            chunkY * height;

        if (!Intersects(
                chunkWorldX,
                chunkWorldY,
                width,
                height,
                bounds))
        {
            return;
        }

        var startX =
            Math.Max(
                0,
                (int)MathF.Floor(
                    bounds.X -
                    chunkWorldX));

        var endX =
            Math.Min(
                width - 1,
                (int)MathF.Ceiling(
                    bounds.X +
                    bounds.Width -
                    chunkWorldX) - 1);

        var startY =
            Math.Max(
                0,
                (int)MathF.Floor(
                    bounds.Y -
                    chunkWorldY));

        var endY =
            Math.Min(
                height - 1,
                (int)MathF.Ceiling(
                    bounds.Y +
                    bounds.Height -
                    chunkWorldY) - 1);

        if (startX > endX ||
            startY > endY)
        {
            return;
        }

        for (var y = startY;
             y <= endY;
             y++)
        {
            for (var x = startX;
                 x <= endX;
                 x++)
            {
                var index =
                    y * width +
                    x;

                var tileValue =
                    tiles[index];

                if (tileValue == 0)
                {
                    continue;
                }

                var atlasIndex =
                    checked(
                        (int)tileValue - 1);

                var region =
                    _atlas.GetRegion(
                        atlasIndex);

                _graphics.Submit(
                    new DrawWorldTextureCommand(
                        _atlas.Texture,

                        new Vector2(
                            chunkWorldX + x,
                            chunkWorldY + y),

                        new Vector2(
                            _tileSize,
                            _tileSize),

                        region.UV));
            }
        }
    }

    private static bool Intersects(
        int x,
        int y,
        int width,
        int height,
        Rectangle bounds)
    {
        return
            x < bounds.X + bounds.Width &&
            x + width > bounds.X &&
            y < bounds.Y + bounds.Height &&
            y + height > bounds.Y;
    }
}