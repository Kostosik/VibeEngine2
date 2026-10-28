using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.Editor.Documents;
using Engine.Editor.UI.Assets;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.Graphics.Tilemaps;
using Engine.Serialization.Content;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

internal sealed class ViewportTilemapRenderer
{
    private static readonly StringComparer PathComparer =
        OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    private static readonly IReadOnlyDictionary<uint, TileVisual>
        EmptyVisuals = new Dictionary<uint, TileVisual>();

    private readonly ViewportProjectTextureCache _textureCache;
    private readonly ITextureManager? _textureManager;

    private readonly Dictionary<string, CachedTilemap> _tilemaps =
        new(PathComparer);

    private readonly Dictionary<string, CachedTileSet> _tileSets =
        new(PathComparer);

    private string? _assetsRoot;

    public ViewportTilemapRenderer(
        ViewportProjectTextureCache textureCache,
        ITextureManager? textureManager)
    {
        ArgumentNullException.ThrowIfNull(textureCache);

        _textureCache = textureCache;
        _textureManager = textureManager;
    }

    public void Draw(
        UiRenderContext context,
        EditorDocument document,
        string? assetsRoot,
        UiRect viewportBounds)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(document);

        if (string.IsNullOrWhiteSpace(assetsRoot) ||
            !_textureCache.IsAvailable ||
            _textureManager is null)
        {
            ReleaseResources();
            return;
        }

        var fullAssetsRoot = Path.GetFullPath(assetsRoot);

        if (_assetsRoot is null ||
            !PathComparer.Equals(_assetsRoot, fullAssetsRoot))
        {
            _tilemaps.Clear();
            _tileSets.Clear();
            _assetsRoot = fullAssetsRoot;
        }

        _textureCache.SetAssetsRoot(fullAssetsRoot);

        var zoom = document.Viewport.State.Zoom;

        foreach (var item in document.World.EcsWorld
                     .Query<TilemapComponent, WorldTransform2D>())
        {
            var component = item.First;
            var transform = item.Second;

            if (!TryLoadTilemap(
                    fullAssetsRoot,
                    component.TilemapPath,
                    out var definition))
            {
                continue;
            }

            var scaleX = transform.Scale.X.ToFloat();
            var scaleY = transform.Scale.Y.ToFloat();
            var rotation = transform.Rotation.ToFloat();

            if (!float.IsFinite(scaleX) ||
                !float.IsFinite(scaleY) ||
                !float.IsFinite(rotation) ||
                MathF.Abs(scaleX) < 0.0001f ||
                MathF.Abs(scaleY) < 0.0001f)
            {
                continue;
            }

            if (!TryGetVisibleCellBounds(
                    document,
                    viewportBounds,
                    definition,
                    transform,
                    out var startX,
                    out var endX,
                    out var startY,
                    out var endY))
            {
                continue;
            }

            var visuals = GetTileVisuals(
                fullAssetsRoot,
                definition.TileSetPath);

            var positionX = transform.Position.X.ToFloat();
            var positionY = transform.Position.Y.ToFloat();

            var cosine = MathF.Cos(rotation);
            var sine = MathF.Sin(rotation);

            var cellWidth = MathF.Abs(scaleX) * zoom;
            var cellHeight = MathF.Abs(scaleY) * zoom;

            var rotationDegrees =
                -rotation * (180.0f / MathF.PI);

            for (var y = startY; y <= endY; y++)
            {
                for (var x = startX; x <= endX; x++)
                {
                    var tileId = definition.Tiles[
                        y * definition.Width + x];

                    // ID 0 — пустая ячейка.
                    if (tileId == 0)
                    {
                        continue;
                    }

                    // Локальная ось Y направлена вниз,
                    // а мировая ось Y — вверх.
                    var localX = (x + 0.5f) * scaleX;
                    var localY = -(y + 0.5f) * scaleY;

                    var worldX =
                        positionX +
                        localX * cosine -
                        localY * sine;

                    var worldY =
                        positionY +
                        localX * sine +
                        localY * cosine;

                    var localScreen =
                        document.Viewport.Transform.WorldToScreen(
                            new Vector2(worldX, worldY));

                    var center = new Vector2(
                        viewportBounds.X + localScreen.X,
                        viewportBounds.Y + localScreen.Y);

                    var destination = new UiRect(
                        center.X - cellWidth * 0.5f,
                        center.Y - cellHeight * 0.5f,
                        cellWidth,
                        cellHeight);

                    context.PushRotation(
                        center,
                        rotationDegrees);

                    try
                    {
                        if (visuals.TryGetValue(
                                tileId,
                                out var visual))
                        {
                            context.DrawTexture(
                                visual.Texture,
                                destination,
                                FlipUv(
                                    visual.UV,
                                    scaleX < 0.0f,
                                    scaleY < 0.0f),
                                layer: component.Layer);
                        }
                        else
                        {
                            DrawMissingTile(
                                context,
                                destination,
                                tileId,
                                zoom,
                                component.Layer);
                        }
                    }
                    finally
                    {
                        context.PopRotation();
                    }
                }
            }
        }
    }

    public void ReleaseResources()
    {
        _assetsRoot = null;
        _tilemaps.Clear();
        _tileSets.Clear();
    }

    private bool TryLoadTilemap(
        string assetsRoot,
        string tilemapPath,
        out TilemapDefinition definition)
    {
        definition = null!;

        if (string.IsNullOrWhiteSpace(tilemapPath))
        {
            return false;
        }

        string normalizedPath;

        try
        {
            normalizedPath = new AssetPath(tilemapPath).Value;

            if (!normalizedPath.EndsWith(
                    ".tilemap.json",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        catch (ArgumentException)
        {
            return false;
        }

        string fullPath;

        try
        {
            fullPath = EditorProjectAssetPathResolver.Resolve(
                assetsRoot,
                normalizedPath,
                "Tilemap");
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            InvalidDataException or
            IOException or
            UnauthorizedAccessException)
        {
            return false;
        }

        if (!File.Exists(fullPath))
        {
            _tilemaps.Remove(normalizedPath);
            return false;
        }

        var lastWrite = File.GetLastWriteTimeUtc(fullPath);

        if (_tilemaps.TryGetValue(
                normalizedPath,
                out var cached) &&
            cached.LastWriteTimeUtc == lastWrite)
        {
            if (cached.Definition is null)
            {
                return false;
            }

            definition = cached.Definition;
            return true;
        }

        try
        {
            definition =
                JsonTilemapDefinitionSerializer.Deserialize(
                    new AssetPath(normalizedPath),
                    File.ReadAllBytes(fullPath));

            _tilemaps[normalizedPath] = new CachedTilemap(
                lastWrite,
                definition);

            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            InvalidDataException or
            IOException or
            UnauthorizedAccessException)
        {
            _tilemaps[normalizedPath] = new CachedTilemap(
                lastWrite,
                null);

            return false;
        }
    }

    private IReadOnlyDictionary<uint, TileVisual> GetTileVisuals(
        string assetsRoot,
        string tileSetPath)
    {
        string normalizedPath;

        try
        {
            normalizedPath = new AssetPath(tileSetPath).Value;

            if (!normalizedPath.EndsWith(
                    ".tileset.json",
                    StringComparison.OrdinalIgnoreCase))
            {
                return EmptyVisuals;
            }
        }
        catch (ArgumentException)
        {
            return EmptyVisuals;
        }

        string fullPath;

        try
        {
            fullPath = EditorProjectAssetPathResolver.Resolve(
                assetsRoot,
                normalizedPath,
                "TileSet");
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            InvalidDataException or
            IOException or
            UnauthorizedAccessException)
        {
            return EmptyVisuals;
        }

        if (!File.Exists(fullPath))
        {
            _tileSets.Remove(normalizedPath);
            return EmptyVisuals;
        }

        var lastWrite = File.GetLastWriteTimeUtc(fullPath);

        if (_tileSets.TryGetValue(
                normalizedPath,
                out var cached) &&
            cached.LastWriteTimeUtc == lastWrite)
        {
            return cached.Visuals;
        }

        IReadOnlyDictionary<uint, TileVisual> visuals;

        try
        {
            var definition =
                JsonTileSetDefinitionSerializer.Deserialize(
                    new AssetPath(normalizedPath),
                    File.ReadAllBytes(fullPath));

            visuals = BuildTileVisuals(
                definition,
                assetsRoot);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            InvalidDataException or
            IOException or
            UnauthorizedAccessException)
        {
            visuals = EmptyVisuals;
        }

        _tileSets[normalizedPath] = new CachedTileSet(
            lastWrite,
            visuals);

        return visuals;
    }

    private IReadOnlyDictionary<uint, TileVisual> BuildTileVisuals(
        TileSetDefinition definition,
        string assetsRoot)
    {
        var atlases = new Dictionary<string, TextureAtlas>(
            StringComparer.Ordinal);

        foreach (var atlasDefinition in definition.Atlases)
        {
            if (!_textureCache.TryLoad(
                    atlasDefinition.TexturePath,
                    out var texture))
            {
                continue;
            }

            try
            {
                var description =
                    _textureManager!.GetDescription(texture);

                atlases[atlasDefinition.Id] = new TextureAtlas(
                    texture,
                    description,
                    atlasDefinition.TileWidth,
                    atlasDefinition.TileHeight);
            }
            catch (Exception exception) when (
                exception is ArgumentException or
                InvalidOperationException or
                KeyNotFoundException)
            {
                // Один неправильный атлас не блокирует остальные.
            }
        }

        var visuals = new Dictionary<uint, TileVisual>();

        foreach (var tile in definition.Tiles)
        {
            if (!atlases.TryGetValue(
                    tile.AtlasId,
                    out var atlas) ||
                (uint)tile.AtlasIndex >= (uint)atlas.Count)
            {
                continue;
            }

            visuals[tile.Id] = new TileVisual(
                atlas.Texture,
                atlas.GetRegion(tile.AtlasIndex).UV);
        }

        return visuals;
    }

    private static bool TryGetVisibleCellBounds(
        EditorDocument document,
        UiRect viewportBounds,
        TilemapDefinition definition,
        WorldTransform2D transform,
        out int startX,
        out int endX,
        out int startY,
        out int endY)
    {
        startX = 0;
        endX = -1;
        startY = 0;
        endY = -1;

        var scaleX = transform.Scale.X.ToFloat();
        var scaleY = transform.Scale.Y.ToFloat();

        if (MathF.Abs(scaleX) < 0.0001f ||
            MathF.Abs(scaleY) < 0.0001f)
        {
            return false;
        }

        var rotation = transform.Rotation.ToFloat();
        var cosine = MathF.Cos(rotation);
        var sine = MathF.Sin(rotation);

        var positionX = transform.Position.X.ToFloat();
        var positionY = transform.Position.Y.ToFloat();

        var corners = new[]
        {
            new Vector2(0.0f, 0.0f),
            new Vector2(viewportBounds.Width, 0.0f),
            new Vector2(viewportBounds.Width, viewportBounds.Height),
            new Vector2(0.0f, viewportBounds.Height)
        };

        var minX = float.PositiveInfinity;
        var minY = float.PositiveInfinity;
        var maxX = float.NegativeInfinity;
        var maxY = float.NegativeInfinity;

        foreach (var corner in corners)
        {
            var world =
                document.Viewport.Transform.ScreenToWorld(corner);

            var dx = world.X - positionX;
            var dy = world.Y - positionY;

            var mapX =
                (dx * cosine + dy * sine) / scaleX;

            var mapY =
                (dx * sine - dy * cosine) / scaleY;

            minX = MathF.Min(minX, mapX);
            minY = MathF.Min(minY, mapY);
            maxX = MathF.Max(maxX, mapX);
            maxY = MathF.Max(maxY, mapY);
        }

        if (maxX < 0.0f ||
            minX > definition.Width ||
            maxY < 0.0f ||
            minY > definition.Height)
        {
            return false;
        }

        startX = (int)MathF.Floor(
            Math.Clamp(minX - 1.0f, 0.0f, definition.Width - 1.0f));

        endX = (int)MathF.Ceiling(
            Math.Clamp(maxX + 1.0f, 0.0f, definition.Width - 1.0f));

        startY = (int)MathF.Floor(
            Math.Clamp(minY - 1.0f, 0.0f, definition.Height - 1.0f));

        endY = (int)MathF.Ceiling(
            Math.Clamp(maxY + 1.0f, 0.0f, definition.Height - 1.0f));

        return startX <= endX && startY <= endY;
    }

    private static Rectangle FlipUv(
        Rectangle uv,
        bool flipX,
        bool flipY)
    {
        return new Rectangle(
            flipX ? uv.X + uv.Width : uv.X,
            flipY ? uv.Y + uv.Height : uv.Y,
            flipX ? -uv.Width : uv.Width,
            flipY ? -uv.Height : uv.Height);
    }

    private static void DrawMissingTile(
        UiRenderContext context,
        UiRect destination,
        uint tileId,
        float zoom,
        int layer)
    {
        if (destination.Width < 2.0f ||
            destination.Height < 2.0f)
        {
            return;
        }

        context.DrawRectangle(
            destination,
            new UiColor(180, 75, 150, 220),
            filled: false,
            layer: layer);

        if (zoom >= 8.0f)
        {
            context.DrawText(
                tileId.ToString(),
                new Vector2(
                    destination.X + 2.0f,
                    destination.Y + 2.0f),
                MathF.Min(11.0f, destination.Height * 0.35f),
                new UiColor(245, 245, 245, 255),
                layer: layer + 1);
        }
    }

    public void DrawSelectionOutline(
    UiRenderContext context,
    EditorDocument document,
    string? assetsRoot,
    UiRect viewportBounds)
    {
        if (string.IsNullOrWhiteSpace(assetsRoot))
        {
            return;
        }

        var fullAssetsRoot = Path.GetFullPath(assetsRoot);

        if (_assetsRoot is null ||
            !PathComparer.Equals(_assetsRoot, fullAssetsRoot))
        {
            _tilemaps.Clear();
            _tileSets.Clear();
            _assetsRoot = fullAssetsRoot;
        }

        var ecs = document.World.EcsWorld;
        var zoom = document.Viewport.State.Zoom;

        foreach (var entity in document.EntitySelection.Items)
        {
            if (!ecs.Exists(entity) ||
                !ecs.Has<TilemapComponent>(entity) ||
                !ecs.Has<WorldTransform2D>(entity))
            {
                continue;
            }

            var component = ecs.Get<TilemapComponent>(entity);

            if (string.IsNullOrWhiteSpace(component.TilemapPath) ||
                !TryLoadTilemap(
                    fullAssetsRoot,
                    component.TilemapPath,
                    out var definition))
            {
                continue;
            }

            var transform = ecs.Get<WorldTransform2D>(entity);

            var scaleX = transform.Scale.X.ToFloat();
            var scaleY = transform.Scale.Y.ToFloat();
            var rotation = transform.Rotation.ToFloat();

            if (!float.IsFinite(scaleX) ||
                !float.IsFinite(scaleY) ||
                !float.IsFinite(rotation) ||
                MathF.Abs(scaleX) < 0.0001f ||
                MathF.Abs(scaleY) < 0.0001f)
            {
                continue;
            }

            var width = definition.Width * scaleX;
            var height = definition.Height * scaleY;

            var positionX = transform.Position.X.ToFloat();
            var positionY = transform.Position.Y.ToFloat();

            var cosine = MathF.Cos(rotation);
            var sine = MathF.Sin(rotation);

            // Позиция трансформа соответствует верхнему левому углу.
            // Локальная ось Y карты направлена вниз по экрану.
            var corners = new[]
            {
            new Vector2(0.0f, 0.0f),
            new Vector2(width, 0.0f),
            new Vector2(width, -height),
            new Vector2(0.0f, -height)
        };

            var screenCorners = new Vector2[corners.Length];

            for (var i = 0; i < corners.Length; i++)
            {
                var corner = corners[i];

                var worldCorner = new Vector2(
                    positionX +
                    corner.X * cosine -
                    corner.Y * sine,

                    positionY +
                    corner.X * sine +
                    corner.Y * cosine);

                var localScreen =
                    document.Viewport.Transform.WorldToScreen(worldCorner);

                screenCorners[i] = new Vector2(
                    viewportBounds.X + localScreen.X,
                    viewportBounds.Y + localScreen.Y);
            }

            DrawSelectionOutlineLines(
                context,
                screenCorners,
                new UiColor(255, 220, 80, 255),
                zoom,
                Math.Max(component.Layer + 1, 2));
        }
    }

    private static void DrawSelectionOutlineLines(
        UiRenderContext context,
        IReadOnlyList<Vector2> points,
        UiColor color,
        float zoom,
        int layer)
    {
        for (var i = 0; i < points.Count; i++)
        {
            var start = points[i];
            var end = points[(i + 1) % points.Count];
            var delta = end - start;

            var length = MathF.Sqrt(
                delta.X * delta.X +
                delta.Y * delta.Y);

            if (!float.IsFinite(length) || length <= 0.01f)
            {
                continue;
            }

            var thickness = Math.Clamp(
                zoom * 1.25f,
                1.25f,
                4.0f);

            var center = new Vector2(
                (start.X + end.X) * 0.5f,
                (start.Y + end.Y) * 0.5f);

            var angleDegrees =
                MathF.Atan2(delta.Y, delta.X) *
                (180.0f / MathF.PI);

            context.PushRotation(center, angleDegrees);

            try
            {
                context.DrawRectangle(
                    new UiRect(
                        center.X - length * 0.5f,
                        center.Y - thickness * 0.5f,
                        length,
                        thickness),
                    color,
                    filled: true,
                    layer: layer);
            }
            finally
            {
                context.PopRotation();
            }
        }
    }

    private readonly record struct CachedTilemap(
        DateTime LastWriteTimeUtc,
        TilemapDefinition? Definition);

    private sealed record CachedTileSet(
        DateTime LastWriteTimeUtc,
        IReadOnlyDictionary<uint, TileVisual> Visuals);
}