using Engine.Camera;
using Engine.Content;
using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.Graphics;
using Engine.Graphics.Resources;
using Engine.Graphics.Tilemaps;

namespace Engine.Runtime.Tilemaps;

public sealed class TilemapRenderSystem : IDisposable
{
    private readonly World _world;
    private readonly IGraphicsDevice _graphics;
    private readonly IContentManager _content;
    private readonly Camera2D _camera;
    private readonly TextureResourceManager _textureResources;

    private readonly Dictionary<AssetPath, TilemapDefinition> _tilemaps =
        new();

    private readonly HashSet<AssetPath> _loadedTilemapPaths =
        new();

    private readonly Dictionary<AssetPath, TilemapRenderer> _renderers =
        new();

    private readonly HashSet<AssetPath> _loadedTileSetPaths =
        new();

    private bool _disposed;

    public TilemapRenderSystem(
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
        _graphics = graphics;
        _content = content;
        _camera = camera;

        _textureResources = new TextureResourceManager(
            content,
            graphics.Textures);
    }

    public void Render()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var item in _world.QueryReadOnly<
                     TilemapComponent,
                     WorldTransform2D>())
        {
            var component = item.First;
            var transform = item.Second;

            // Пустая ссылка не требует загрузки ресурсов.
            if (string.IsNullOrWhiteSpace(component.TilemapPath))
            {
                continue;
            }

            var tilemapPath = NormalizeAssetPath(
                component.TilemapPath,
                ".tilemap.json",
                "Tilemap");

            var definition = GetTilemap(tilemapPath);
            var renderer = GetRenderer(definition.TileSetPath);

            renderer.RenderMap(
                definition,
                new Vector2(
                    transform.Position.X.ToFloat(),
                    transform.Position.Y.ToFloat()),
                new Vector2(
                    transform.Scale.X.ToFloat(),
                    transform.Scale.Y.ToFloat()),
                transform.Rotation.ToFloat(),
                component.Layer);
        }
    }

    private TilemapDefinition GetTilemap(AssetPath path)
    {
        if (_tilemaps.TryGetValue(path, out var definition))
        {
            return definition;
        }

        definition = _content.Load<TilemapDefinition>(path);

        _loadedTilemapPaths.Add(path);
        _tilemaps.Add(path, definition);

        return definition;
    }

    private TilemapRenderer GetRenderer(string tileSetPath)
    {
        var path = NormalizeAssetPath(
            tileSetPath,
            ".tileset.json",
            "TileSet");

        if (_renderers.TryGetValue(path, out var renderer))
        {
            return renderer;
        }

        var definition = _content.Load<TileSetDefinition>(path);
        _loadedTileSetPaths.Add(path);

        try
        {
            var tileSet = TileSetFactory.Create(
                definition,
                _textureResources);

            renderer = new TilemapRenderer(
                _graphics,
                _camera,
                tileSet,
                tileSize: 1.0f);

            _renderers.Add(path, renderer);

            return renderer;
        }
        catch
        {
            _content.Unload<TileSetDefinition>(path);
            _loadedTileSetPaths.Remove(path);
            throw;
        }
    }

    private static AssetPath NormalizeAssetPath(
        string value,
        string requiredSuffix,
        string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim().Replace('\\', '/');

        var isDriveRooted =
            normalized.Length >= 2 &&
            char.IsAsciiLetter(normalized[0]) &&
            normalized[1] == ':';

        if (Path.IsPathRooted(normalized) ||
            normalized.StartsWith('/') ||
            isDriveRooted)
        {
            throw new InvalidDataException(
                $"{description} path must be relative to Assets.");
        }

        var segments = normalized.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0 ||
            segments.Any(segment => segment is "." or "..") ||
            !normalized.EndsWith(
                requiredSuffix,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"{description} path is invalid.");
        }

        return new AssetPath(string.Join("/", segments));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        var errors = new List<Exception>();

        void TryCleanup(Action cleanup)
        {
            try
            {
                cleanup();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        TryCleanup(_textureResources.Dispose);

        foreach (var path in _loadedTilemapPaths)
        {
            TryCleanup(
                () => _content.Unload<TilemapDefinition>(path));
        }

        foreach (var path in _loadedTileSetPaths)
        {
            TryCleanup(
                () => _content.Unload<TileSetDefinition>(path));
        }

        _tilemaps.Clear();
        _loadedTilemapPaths.Clear();
        _renderers.Clear();
        _loadedTileSetPaths.Clear();

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "One or more Tilemap resources could not be released.",
                errors);
        }
    }
}