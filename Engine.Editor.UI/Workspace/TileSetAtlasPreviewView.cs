using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Editor;
using Engine.Editor.Documents;
using Engine.Editor.UI.Assets;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.Graphics.Tilemaps;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class TileSetAtlasPreviewView : UiPanel
{
    private const float MinZoom = 0.1f;
    private const float MaxZoom = 16.0f;
    private const float ZoomStep = 1.15f;

    private float _zoom = 1.0f;
    private Vector2 _panOffset;

    private bool _isPanning;
    private Vector2 _lastPanPosition;

    private UiRect _imageViewportBounds;

    private readonly TileSetDocument _document;
    private readonly EditorContext? _editor;
    private readonly ITextureManager? _textureManager;
    private readonly Func<string?> _getSelectedAtlasId;

    private readonly ImageTextureLoader _textureLoader = new();

    private TextureHandle _texture;
    private string? _textureKey;
    private string? _textureError;
    private UiRect _imageBounds;

    private string? _displayedAtlasId;
    private string? _lastAtlasId;

    private int _textureWidth;
    private int _textureHeight;
    private int _tileWidth;
    private int _tileHeight;
    private int _columns;
    private int _rows;

    private int _selectedIndex = -1;
    private string? _selectedIndexAtlasId;
    public event Action<string, int>? AtlasCellSelected;
    public TileSetAtlasPreviewView(
        TileSetDocument document,
        EditorContext? editor,
        ITextureManager? textureManager,
        Func<string?> getSelectedAtlasId)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(getSelectedAtlasId);

        _document = document;
        _editor = editor;
        _textureManager = textureManager;
        _getSelectedAtlasId = getSelectedAtlasId;

        Width = 420.0f;
        Height = 430.0f;

        HorizontalAlignment = UiHorizontalAlignment.Stretch;
        VerticalAlignment = UiVerticalAlignment.Top;

        Background = new UiColor(15, 17, 21, 255);
    }

    protected override void OnRender(UiRenderContext context)
    {
        base.OnRender(context);
        _imageBounds = default;
        _displayedAtlasId = null;
        if (Bounds.Width <= 0.0f || Bounds.Height <= 0.0f)
        {
            return;
        }

        context.PushClip(Bounds);

        try
        {
            context.DrawText(
                "Atlas Preview",
                new Vector2(Bounds.X + 8.0f, Bounds.Y + 8.0f),
                13.0f,
                new UiColor(220, 225, 235, 255));

            var atlasId = _getSelectedAtlasId();

            var atlas = _document.Definition.Atlases.FirstOrDefault(
                item => string.Equals(
                    item.Id,
                    atlasId,
                    StringComparison.Ordinal));

            // Если выбор ещё не установлен, показываем первый атлас.
            atlas ??= _document.Definition.Atlases.FirstOrDefault();

            if (atlas is null)
            {
                ReleaseTexture();
                _textureKey = null;

                context.DrawText(
                    "Add an atlas to preview its texture.",
                    new Vector2(Bounds.X + 8.0f, Bounds.Y + 36.0f),
                    12.0f,
                    new UiColor(170, 175, 185, 255));

                return;
            }

            if (!string.Equals(
                    _lastAtlasId,
                    atlas.Id,
                    StringComparison.Ordinal))
            {
                _lastAtlasId = atlas.Id;

                if (!string.Equals(
                        _selectedIndexAtlasId,
                        atlas.Id,
                        StringComparison.Ordinal))
                {
                    _selectedIndex = -1;
                    _selectedIndexAtlasId = null;
                }
            }

            if (!EnsureTextureLoaded(atlas))
            {
                context.DrawText(
                    _textureError ?? "Texture preview unavailable.",
                    new Vector2(Bounds.X + 8.0f, Bounds.Y + 36.0f),
                    12.0f,
                    new UiColor(225, 160, 110, 255));

                return;
            }

            var description = _textureManager!.GetDescription(_texture);

            var textureWidth = description.Width;
            var textureHeight = description.Height;

            if (textureWidth <= 0 || textureHeight <= 0)
            {
                context.DrawText(
                    "Texture has invalid dimensions.",
                    new Vector2(Bounds.X + 8.0f, Bounds.Y + 36.0f),
                    12.0f,
                    new UiColor(225, 160, 110, 255));

                return;
            }

            if (textureWidth % atlas.TileWidth != 0 ||
    textureHeight % atlas.TileHeight != 0)
            {
                context.DrawText(
                    "Texture dimensions must be divisible by tile size.",
                    new Vector2(Bounds.X + 8.0f, Bounds.Y + 36.0f),
                    12.0f,
                    new UiColor(225, 160, 110, 255));

                return;
            }

            _textureWidth = textureWidth;
            _textureHeight = textureHeight;
            _tileWidth = atlas.TileWidth;
            _tileHeight = atlas.TileHeight;

            _columns = textureWidth / atlas.TileWidth;
            _rows = textureHeight / atlas.TileHeight;

            _displayedAtlasId = atlas.Id;

            const float horizontalMargin = 10.0f;
            const float topMargin = 34.0f;
            const float bottomMargin = 34.0f;

            var availableWidth = MathF.Max(
                1.0f,
                Bounds.Width - horizontalMargin * 2.0f);

            var availableHeight = MathF.Max(
                1.0f,
                Bounds.Height - topMargin - bottomMargin);

            var scale = MathF.Min(
                availableWidth / textureWidth,
                availableHeight / textureHeight);

            _imageViewportBounds = new UiRect(
                Bounds.X + horizontalMargin,
                Bounds.Y + topMargin,
                availableWidth,
                availableHeight);

            var imageWidth = textureWidth * scale * _zoom;
            var imageHeight = textureHeight * scale * _zoom;

            var centerX =
                _imageViewportBounds.X +
                _imageViewportBounds.Width * 0.5f +
                _panOffset.X;

            var centerY =
                _imageViewportBounds.Y +
                _imageViewportBounds.Height * 0.5f +
                _panOffset.Y;

            var destination = new UiRect(
                centerX - imageWidth * 0.5f,
                centerY - imageHeight * 0.5f,
                imageWidth,
                imageHeight);

            _imageBounds = destination;
            _imageBounds = destination;
            context.PushClip(_imageViewportBounds);

            try
            {
                context.DrawTexture(
                    _texture,
                    destination,
                    new Rectangle(0.0f, 0.0f, 1.0f, 1.0f));

                DrawSelectedCell(context, destination);

                DrawAtlasGrid(
                    context,
                    destination,
                    textureWidth,
                    textureHeight,
                    atlas.TileWidth,
                    atlas.TileHeight);
            }
            finally
            {
                context.PopClip();
            }

            var columns = (textureWidth + atlas.TileWidth - 1) /
                          atlas.TileWidth;

            var rows = (textureHeight + atlas.TileHeight - 1) /
                       atlas.TileHeight;

            var selectionText = _selectedIndex >= 0
                ? $" | Selected index: {_selectedIndex}"
                : string.Empty;

            context.DrawText(
                $"{textureWidth} × {textureHeight}  |  " +
                $"Cell: {atlas.TileWidth} × {atlas.TileHeight}  |  " +
                $"Grid: {_columns} × {_rows}{selectionText}",
                new Vector2(Bounds.X + 8.0f, Bounds.Bottom - 20.0f),
                11.0f,
                new UiColor(190, 200, 215, 255));
        }
        finally
        {
            context.PopClip();
        }
    }

    protected override void OnPointerWheel(
    UiPointerEvent pointer)
    {
        base.OnPointerWheel(pointer);

        if (MathF.Abs(pointer.ScrollDelta) < 0.0001f ||
            _imageBounds.Width <= 0.0f ||
            _imageBounds.Height <= 0.0f ||
            !ContainsPoint(_imageViewportBounds, pointer.Position))
        {
            return;
        }

        var newZoom = Math.Clamp(
            _zoom * MathF.Pow(ZoomStep, pointer.ScrollDelta),
            MinZoom,
            MaxZoom);

        if (MathF.Abs(newZoom - _zoom) < 0.000001f)
        {
            return;
        }

        // Координаты точки под курсором в текущем изображении.
        var u =
            (pointer.Position.X - _imageBounds.X) /
            _imageBounds.Width;

        var v =
            (pointer.Position.Y - _imageBounds.Y) /
            _imageBounds.Height;

        var factor = newZoom / _zoom;

        var newImageWidth = _imageBounds.Width * factor;
        var newImageHeight = _imageBounds.Height * factor;

        // Сохраняем точку изображения под курсором неподвижной.
        var targetCenterX =
            pointer.Position.X +
            (0.5f - u) * newImageWidth;

        var targetCenterY =
            pointer.Position.Y +
            (0.5f - v) * newImageHeight;

        _panOffset = new Vector2(
            targetCenterX -
            (_imageViewportBounds.X + _imageViewportBounds.Width * 0.5f),

            targetCenterY -
            (_imageViewportBounds.Y + _imageViewportBounds.Height * 0.5f));

        _zoom = newZoom;

        pointer.Handled = true;
    }

    private static bool ContainsPoint(
    UiRect rect,
    Vector2 point)
    {
        return point.X >= rect.X &&
               point.Y >= rect.Y &&
               point.X <= rect.X + rect.Width &&
               point.Y <= rect.Y + rect.Height;
    }

    protected override void OnPointerDown(
        UiPointerEvent pointer)
    {
        // Средняя кнопка — панорамирование.
        if (pointer.Button == Engine.Input.InputMouseButton.Middle)
        {
            if (!ContainsPoint(_imageViewportBounds, pointer.Position))
            {
                return;
            }

            _isPanning = true;
            _lastPanPosition = pointer.Position;

            pointer.RequestCapture();
            pointer.Handled = true;
            return;
        }

        // Левая кнопка — выбор ячейки.
        if (pointer.Button != Engine.Input.InputMouseButton.Left ||
            string.IsNullOrWhiteSpace(_displayedAtlasId) ||
            _imageBounds.Width <= 0.0f ||
            _imageBounds.Height <= 0.0f ||
            !ContainsPoint(_imageViewportBounds, pointer.Position))
        {
            return;
        }

        var relativeX =
            (pointer.Position.X - _imageBounds.X) /
            _imageBounds.Width;

        var relativeY =
            (pointer.Position.Y - _imageBounds.Y) /
            _imageBounds.Height;

        if (relativeX < 0.0f || relativeX >= 1.0f ||
            relativeY < 0.0f || relativeY >= 1.0f)
        {
            return;
        }

        var pixelX = (int)(relativeX * _textureWidth);
        var pixelY = (int)(relativeY * _textureHeight);

        var column = pixelX / _tileWidth;
        var row = pixelY / _tileHeight;

        var index = row * _columns + column;

        _selectedIndex = index;
        _selectedIndexAtlasId = _displayedAtlasId;

        // Событие вызываем только один раз.
        AtlasCellSelected?.Invoke(
            _displayedAtlasId,
            index);

        pointer.Handled = true;
    }

    protected override void OnPointerMove(
        UiPointerEvent pointer)
    {
        base.OnPointerMove(pointer);

        if (!_isPanning ||
            pointer.Button != Engine.Input.InputMouseButton.Middle)
        {
            return;
        }

        var delta = new Vector2(
            pointer.Position.X - _lastPanPosition.X,
            pointer.Position.Y - _lastPanPosition.Y);

        _lastPanPosition = pointer.Position;

        _panOffset = new Vector2(
            _panOffset.X + delta.X,
            _panOffset.Y + delta.Y);

        pointer.Handled = true;
    }

    protected override void OnPointerUp(
        UiPointerEvent pointer)
    {
        base.OnPointerUp(pointer);

        if (!_isPanning ||
            pointer.Button != Engine.Input.InputMouseButton.Middle)
        {
            return;
        }

        _isPanning = false;

        pointer.ReleaseCapture();
        pointer.Handled = true;
    }

    public void SelectCell(
    string atlasId,
    int atlasIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(atlasId);

        if (atlasIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(atlasIndex));
        }

        _selectedIndexAtlasId = atlasId;
        _selectedIndex = atlasIndex;
    }

    private void DrawSelectedCell(
    UiRenderContext context,
    UiRect destination)
    {
        if (_selectedIndex < 0 ||
            _selectedIndex >= _columns * _rows)
        {
            return;
        }

        var column = _selectedIndex % _columns;
        var row = _selectedIndex / _columns;

        var scaleX = destination.Width / _textureWidth;
        var scaleY = destination.Height / _textureHeight;

        var cellBounds = new UiRect(
            destination.X + column * _tileWidth * scaleX,
            destination.Y + row * _tileHeight * scaleY,
            _tileWidth * scaleX,
            _tileHeight * scaleY);

        context.DrawRectangle(
            cellBounds,
            new UiColor(255, 200, 80, 90));
    }

    private void DrawAtlasGrid(
        UiRenderContext context,
        UiRect destination,
        int textureWidth,
        int textureHeight,
        int tileWidth,
        int tileHeight)
    {
        if (tileWidth <= 0 || tileHeight <= 0)
        {
            return;
        }

        var scaleX = destination.Width / textureWidth;
        var scaleY = destination.Height / textureHeight;

        var gridColor = new UiColor(100, 210, 235, 190);

        // Вертикальные границы ячеек.
        for (var pixelX = tileWidth;
             pixelX < textureWidth;
             pixelX += tileWidth)
        {
            var screenX = destination.X + pixelX * scaleX;

            context.DrawRectangle(
                new UiRect(
                    screenX,
                    destination.Y,
                    1.0f,
                    destination.Height),
                gridColor);
        }

        // Горизонтальные границы ячеек.
        for (var pixelY = tileHeight;
             pixelY < textureHeight;
             pixelY += tileHeight)
        {
            var screenY = destination.Y + pixelY * scaleY;

            context.DrawRectangle(
                new UiRect(
                    destination.X,
                    screenY,
                    destination.Width,
                    1.0f),
                gridColor);
        }
    }

    private bool EnsureTextureLoaded(
        TileSetAtlasDefinition atlas)
    {
        var assetsRoot = _editor?.ActiveGameProject?.AssetsDirectoryPath;

        if (_textureManager is null ||
            string.IsNullOrWhiteSpace(assetsRoot))
        {
            ReleaseTexture();
            _textureKey = null;

            _textureError = _textureManager is null
                ? "Texture preview is unavailable."
                : "Open a game project to preview textures.";

            return false;
        }

        string key;

        try
        {
            key = Path.GetFullPath(assetsRoot) +
                  "|" +
                  atlas.TexturePath;
        }
        catch (Exception)
        {
            ReleaseTexture();
            _textureKey = null;
            _textureError = "Invalid Assets directory path.";

            return false;
        }

        if (string.Equals(
                key,
                _textureKey,
                StringComparison.Ordinal))
        {
            return _texture.IsValid &&
                   _textureManager.Exists(_texture);
        }

        ReleaseTexture();

        _textureKey = key;
        _textureError = null;

        try
        {
            var texturePath = EditorProjectAssetPathResolver.Resolve(
                assetsRoot,
                atlas.TexturePath,
                "Tile Set atlas texture");

            var imageData = _textureLoader.Load(
                File.ReadAllBytes(texturePath));

            _texture = _textureManager.Create(imageData);

            if (!_texture.IsValid ||
                !_textureManager.Exists(_texture))
            {
                throw new InvalidOperationException(
                    "The graphics backend could not create the preview texture.");
            }

            return true;
        }
        catch (Exception)
        {
            ReleaseTexture();

            _textureError = $"Texture unavailable: {atlas.TexturePath}";

            return false;
        }
    }

    public void ReleaseResources()
    {
        ReleaseTexture();
        _textureKey = null;
    }

    private void ReleaseTexture()
    {
        if (_texture.IsValid &&
            _textureManager?.Exists(_texture) == true)
        {
            _textureManager.Destroy(_texture);
        }

        _texture = default;
    }
}