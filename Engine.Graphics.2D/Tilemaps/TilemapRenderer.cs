using Engine.Camera;
using Engine.Core.Math;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Commands;

namespace Engine.Graphics.Tilemaps;

public sealed class TilemapRenderer
{
    private readonly IGraphicsDevice _graphics;
    private readonly Camera2D _camera;
    private readonly TileSet _tileSet;
    private readonly float _tileSize;

    public TilemapRenderer(
    IGraphicsDevice graphics,
    Camera2D camera,
    TileSet tileSet,
    float tileSize)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(tileSet);

        if (!float.IsFinite(tileSize) ||
            tileSize <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tileSize));
        }

        _graphics = graphics;
        _camera = camera;
        _tileSet = tileSet;
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

                if (tileValue == 0 ||
                    !_tileSet.TryGet(
                        tileValue,
                        out var visual))
                {
                    continue;
                }

                _graphics.Submit(
                    new DrawWorldTextureCommand(
                        visual.Texture,

                        new Vector2(
                            chunkWorldX + x,
                            chunkWorldY + y),

                        new Vector2(
                            _tileSize,
                            _tileSize),

                        visual.UV));
            }
        }
    }

    public void RenderMap(
    TilemapDefinition definition,
    Vector2 position,
    Vector2 scale,
    float rotationRadians,
    int layer = RenderLayers.World)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (!float.IsFinite(position.X) ||
            !float.IsFinite(position.Y) ||
            !float.IsFinite(scale.X) ||
            !float.IsFinite(scale.Y) ||
            !float.IsFinite(rotationRadians) ||
            MathF.Abs(scale.X) < 0.0001f ||
            MathF.Abs(scale.Y) < 0.0001f)
        {
            return;
        }

        var cosine = MathF.Cos(rotationRadians);
        var sine = MathF.Sin(rotationRadians);

        if (!TryGetVisibleCellBounds(
                definition,
                position,
                scale,
                cosine,
                sine,
                out var startX,
                out var endX,
                out var startY,
                out var endY))
        {
            return;
        }

        var tileWidth = _tileSize * MathF.Abs(scale.X);
        var tileHeight = _tileSize * MathF.Abs(scale.Y);

        if (!float.IsFinite(tileWidth) ||
            !float.IsFinite(tileHeight) ||
            tileWidth <= 0.0f ||
            tileHeight <= 0.0f)
        {
            return;
        }

        var absCosine = MathF.Abs(cosine);
        var absSine = MathF.Abs(sine);

        var extentX =
            (tileWidth * absCosine +
             tileHeight * absSine) * 0.5f;

        var extentY =
            (tileWidth * absSine +
             tileHeight * absCosine) * 0.5f;

        var rotationDegrees =
            rotationRadians * (180.0f / MathF.PI);

        for (var y = startY; y <= endY; y++)
        {
            for (var x = startX; x <= endX; x++)
            {
                var tileId =
                    definition.Tiles[y * definition.Width + x];

                if (tileId == 0 ||
                    !_tileSet.TryGet(tileId, out var visual))
                {
                    continue;
                }

                var localX =
                    (x + 0.5f) * _tileSize * scale.X;

                var localY =
                    (y + 0.5f) * _tileSize * scale.Y;

                var centerX =
                    position.X +
                    localX * cosine -
                    localY * sine;

                var centerY =
                    position.Y +
                    localX * sine +
                    localY * cosine;

                // Отсекаем ячейки за пределами камеры,
                // учитывая также их поворот.
                if (!_camera.IsVisible(
                        new Rectangle(
                            centerX - extentX,
                            centerY - extentY,
                            extentX * 2.0f,
                            extentY * 2.0f)))
                {
                    continue;
                }

                var uv = visual.UV;

                if (scale.X < 0.0f)
                {
                    uv = new Rectangle(
                        uv.X + uv.Width,
                        uv.Y,
                        -uv.Width,
                        uv.Height);
                }

                if (scale.Y < 0.0f)
                {
                    uv = new Rectangle(
                        uv.X,
                        uv.Y + uv.Height,
                        uv.Width,
                        -uv.Height);
                }

                _graphics.Submit(
                    new DrawWorldTextureCommand(
                        visual.Texture,
                        new Vector2(
                            centerX - tileWidth * 0.5f,
                            centerY - tileHeight * 0.5f),
                        new Vector2(tileWidth, tileHeight),
                        uv,
                        layer,
                        rotationDegrees));
            }
        }
    }

    private bool TryGetVisibleCellBounds(
        TilemapDefinition definition,
        Vector2 position,
        Vector2 scale,
        float cosine,
        float sine,
        out int startX,
        out int endX,
        out int startY,
        out int endY)
    {
        startX = 0;
        endX = -1;
        startY = 0;
        endY = -1;

        var bounds = _camera.WorldBounds;

        var corners = new[]
        {
        new Vector2(bounds.X, bounds.Y),
        new Vector2(bounds.X + bounds.Width, bounds.Y),
        new Vector2(
            bounds.X + bounds.Width,
            bounds.Y + bounds.Height),
        new Vector2(bounds.X, bounds.Y + bounds.Height)
    };

        var minX = float.PositiveInfinity;
        var minY = float.PositiveInfinity;
        var maxX = float.NegativeInfinity;
        var maxY = float.NegativeInfinity;

        foreach (var corner in corners)
        {
            var dx = corner.X - position.X;
            var dy = corner.Y - position.Y;

            // Переводим мировые координаты в координаты сетки.
            var localX =
                (dx * cosine + dy * sine) /
                (_tileSize * scale.X);

            var localY =
                (-dx * sine + dy * cosine) /
                (_tileSize * scale.Y);

            minX = MathF.Min(minX, localX);
            minY = MathF.Min(minY, localY);
            maxX = MathF.Max(maxX, localX);
            maxY = MathF.Max(maxY, localY);
        }

        if (!float.IsFinite(minX) ||
            !float.IsFinite(minY) ||
            !float.IsFinite(maxX) ||
            !float.IsFinite(maxY) ||
            maxX <= 0.0f ||
            maxY <= 0.0f ||
            minX >= definition.Width ||
            minY >= definition.Height)
        {
            return false;
        }

        // Небольшой запас учитывает ячейки, частично попавшие в экран.
        startX = Math.Max(
            0,
            (int)MathF.Floor(
                Math.Clamp(minX, -1.0f, definition.Width)) - 1);

        endX = Math.Min(
            definition.Width - 1,
            (int)MathF.Ceiling(
                Math.Clamp(maxX, 0.0f, definition.Width)));

        startY = Math.Max(
            0,
            (int)MathF.Floor(
                Math.Clamp(minY, -1.0f, definition.Height)) - 1);

        endY = Math.Min(
            definition.Height - 1,
            (int)MathF.Ceiling(
                Math.Clamp(maxY, 0.0f, definition.Height)));

        return startX <= endX && startY <= endY;
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