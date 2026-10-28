using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Editor;
using Engine.Editor.Assets;
using Engine.Editor.Documents;
using Engine.Editor.UI.Assets;
using Engine.Editor.UI.Dialogs;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.Graphics.Tilemaps;
using Engine.Input;
using Engine.Serialization.Content;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class TilemapEditorPanelView : UiPanel
{
    private readonly TilemapDocument _document;
    private readonly EditorContext _editor;
    private readonly EditorAssetPickerDialog? _assetPickerDialog;
    private readonly ITextureManager? _textureManager;
    private readonly ViewportProjectTextureCache _textureCache;

    private readonly UiPickerField _tileSetPicker;
    private readonly UiStackPanel _paletteRows;
    private readonly UiLabel _messageLabel;
    private readonly UiLabel _selectionLabel;

    private readonly TileSet _visuals = new();
    private TileSetDefinition? _tileSetDefinition;
    private uint _selectedTileId;
    private bool _isPainting;
    private InputMouseButton _strokeButton;
    private (int X, int Y)? _lastStrokeCell;

    private readonly Dictionary<(int X, int Y), uint> _strokeChanges = new();
    public TilemapDocument Document => _document;

    public TilemapEditorPanelView(
        TilemapDocument document,
        EditorContext editor,
        EditorAssetPickerDialog? assetPickerDialog = null,
        ITextureManager? textureManager = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(editor);

        _document = document;
        _editor = editor;
        _assetPickerDialog = assetPickerDialog;
        _textureManager = textureManager;
        _textureCache = new ViewportProjectTextureCache(textureManager);

        Background = new UiColor(24, 24, 24, 255);

        var root = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 0.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        var sidebar = new UiPanel
        {
            Width = 310.0f,
            Padding = new UiThickness(10.0f),
            Background = new UiColor(30, 30, 30, 255),
            HorizontalAlignment = UiHorizontalAlignment.Left,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        var sidebarLayout = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 8.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        sidebarLayout.AddChild(new UiLabel("Tilemap")
        {
            Height = 30.0f,
            FontSize = 20.0f
        });

        sidebarLayout.AddChild(new UiLabel(document.DisplayName)
        {
            Height = 24.0f,
            FontSize = 13.0f,
            Color = new UiColor(170, 170, 170, 255)
        });

        sidebarLayout.AddChild(new UiLabel("Tile Set")
        {
            Height = 24.0f,
            FontSize = 15.0f
        });

        _tileSetPicker = new UiPickerField
        {
            Height = 30.0f,
            FontSize = 12.0f,
            Placeholder = "Select .tileset.json"
        };
        _tileSetPicker.SelectedText = document.Definition.TileSetPath;
        _tileSetPicker.SelectionRequested += SelectTileSet;

        sidebarLayout.AddChild(_tileSetPicker);

        _selectionLabel = new UiLabel("Selected tile: none")
        {
            Height = 24.0f,
            FontSize = 12.0f,
            Color = new UiColor(190, 190, 190, 255)
        };
        sidebarLayout.AddChild(_selectionLabel);

        sidebarLayout.AddChild(new UiLabel("Tile Palette")
        {
            Height = 24.0f,
            FontSize = 15.0f
        });

        var paletteScroll = new UiScrollView
        {
            HorizontalScrolling = false,
            VerticalScrolling = true,
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        _paletteRows = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 4.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch
        };

        paletteScroll.SetContent(_paletteRows);
        sidebarLayout.AddChild(paletteScroll);

        _messageLabel = new UiLabel
        {
            Height = 54.0f,
            FontSize = 11.0f,
            Color = new UiColor(220, 180, 100, 255)
        };
        sidebarLayout.AddChild(_messageLabel);

        sidebar.AddChild(sidebarLayout);
        root.AddChild(sidebar);

        // Область карты занимает всё оставшееся место справа.
        root.AddChild(new UiPanel
        {
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        });

        AddChild(root);

        _document.Changed += OnDocumentChanged;
        ReloadTileSet();
    }

    public void ReleaseResources()
    {
        _document.Changed -= OnDocumentChanged;
        _textureCache.ReleaseResources();
    }

    private void SelectTileSet()
    {
        if (_assetPickerDialog is null)
        {
            _messageLabel.Text = "Asset Picker is not available.";
            return;
        }

        var opened = _assetPickerDialog.Show(
            "Select TileSet",
            IsTileSetAsset,
            SetTileSetPath);

        if (!opened)
        {
            _messageLabel.Text = "Open a game project to select assets.";
        }
    }

    private static bool IsTileSetAsset(EditorAssetEntry entry)
    {
        return !entry.IsDirectory &&
               entry.Path.EndsWith(
                   ".tileset.json",
                   StringComparison.OrdinalIgnoreCase);
    }

    private void SetTileSetPath(string path)
    {
        try
        {
            var current = _document.Definition;

            _document.UpdateDefinition(
                new TilemapDefinition(
                    current.FormatVersion,
                    path,
                    current.Width,
                    current.Height,
                    current.Tiles));

            _tileSetPicker.SelectedText = path;
            ReloadTileSet();
        }
        catch (Exception exception)
        {
            _messageLabel.Text = exception.Message;
        }
    }

    private void OnDocumentChanged()
    {
        _tileSetPicker.SelectedText = _document.Definition.TileSetPath;
        ReloadTileSet();
    }

    private void ReloadTileSet()
    {
        _visuals.Clear();
        _tileSetDefinition = null;

        var assetsRoot = _editor.ActiveGameProject?.AssetsDirectoryPath;

        if (string.IsNullOrWhiteSpace(assetsRoot) ||
            !Directory.Exists(assetsRoot))
        {
            RefreshPalette();
            _messageLabel.Text = "Open a game project to load TileSet assets.";
            return;
        }

        try
        {
            var fullPath = EditorProjectAssetPathResolver.Resolve(
                assetsRoot,
                _document.Definition.TileSetPath,
                "TileSet");

            var definition = JsonTileSetDefinitionSerializer.Deserialize(
                new AssetPath(Path.GetFileName(fullPath)),
                File.ReadAllBytes(fullPath));

            _tileSetDefinition = definition;

            if (_textureManager is not null)
            {
                BuildVisuals(definition, assetsRoot);
            }

            RefreshPalette();

            _messageLabel.Text = _textureManager is null
                ? "TileSet loaded. Texture preview is unavailable."
                : $"Loaded {definition.Tiles.Count} tile(s).";
        }
        catch (Exception exception)
        {
            RefreshPalette();
            _messageLabel.Text = $"Could not load TileSet: {exception.Message}";
        }
    }

    private void BuildVisuals(
        TileSetDefinition definition,
        string assetsRoot)
    {
        _textureCache.SetAssetsRoot(assetsRoot);

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

            var description = _textureManager!.GetDescription(texture);

            var atlas = new TextureAtlas(
                texture,
                description,
                atlasDefinition.TileWidth,
                atlasDefinition.TileHeight);

            atlases.Add(atlasDefinition.Id, atlas);
        }

        foreach (var tile in definition.Tiles)
        {
            if (!atlases.TryGetValue(tile.AtlasId, out var atlas))
            {
                continue;
            }

            if ((uint)tile.AtlasIndex >= (uint)atlas.Count)
            {
                continue;
            }

            _visuals.Set(
                tile.Id,
                atlas.Texture,
                atlas.GetRegion(tile.AtlasIndex).UV);
        }
    }

    private void RefreshPalette()
    {
        _paletteRows.ClearChildren();

        if (_tileSetDefinition is null ||
            _tileSetDefinition.Tiles.Count == 0)
        {
            _paletteRows.AddChild(new UiLabel(
                "Select a TileSet containing tiles.")
            {
                Height = 36.0f,
                FontSize = 12.0f,
                Color = new UiColor(165, 165, 165, 255)
            });

            _selectedTileId = 0;
            _selectionLabel.Text = "Selected tile: none";
            return;
        }

        var validSelection = _tileSetDefinition.Tiles.Any(
            tile => tile.Id == _selectedTileId);

        if (!validSelection)
        {
            _selectedTileId = 0;
        }

        foreach (var tile in _tileSetDefinition.Tiles)
        {
            var tileId = tile.Id;

            var button = new UiButton(
                $"{(tileId == _selectedTileId ? "> " : string.Empty)}" +
                $"ID {tile.Id}  •  {tile.Name}")
            {
                Height = 32.0f,
                HorizontalAlignment = UiHorizontalAlignment.Stretch
            };

            button.Clicked += () =>
            {
                _selectedTileId = tileId;
                _selectionLabel.Text = $"Selected tile: {tileId} — {tile.Name}";
                RefreshPalette();
            };

            _paletteRows.AddChild(button);
        }

        var selected = _tileSetDefinition.Tiles.FirstOrDefault(
            tile => tile.Id == _selectedTileId);

        _selectionLabel.Text = selected is null
            ? "Selected tile: none"
            : $"Selected tile: {selected.Id} — {selected.Name}";
    }

    protected override void OnPointerDown(UiPointerEvent pointer)
    {
        base.OnPointerDown(pointer);

        if (pointer.Button != InputMouseButton.Left &&
            pointer.Button != InputMouseButton.Right)
        {
            return;
        }

        if (!TryGetMapCell(pointer.Position, out var x, out var y))
        {
            return;
        }

        // Не рисуем, пока пользователь не выбрал тайл в палитре.
        if (pointer.Button == InputMouseButton.Left &&
            _selectedTileId == 0)
        {
            return;
        }

        _isPainting = true;
        _strokeButton = pointer.Button;
        _lastStrokeCell = (x, y);
        _strokeChanges.Clear();

        RecordStrokeCell(x, y);

        pointer.RequestCapture();
        pointer.Handled = true;
    }

    protected override void OnPointerMove(UiPointerEvent pointer)
    {
        base.OnPointerMove(pointer);

        if (!_isPainting)
        {
            return;
        }

        pointer.Handled = true;

        if (!TryGetMapCell(pointer.Position, out var x, out var y))
        {
            _lastStrokeCell = null;
            return;
        }

        if (_lastStrokeCell is { } previous)
        {
            RecordStrokeLine(
                previous.X,
                previous.Y,
                x,
                y);
        }
        else
        {
            RecordStrokeCell(x, y);
        }

        _lastStrokeCell = (x, y);
    }

    protected override void OnPointerUp(UiPointerEvent pointer)
    {
        base.OnPointerUp(pointer);

        if (!_isPainting)
        {
            return;
        }

        FinishStroke();

        pointer.ReleaseCapture();
        pointer.Handled = true;
    }

    private void RecordStrokeCell(int x, int y)
    {
        var tileId = _strokeButton == InputMouseButton.Right
            ? 0u
            : _selectedTileId;

        _strokeChanges[(x, y)] = tileId;
    }

    private void RecordStrokeLine(
        int x0,
        int y0,
        int x1,
        int y1)
    {
        // Алгоритм Брезенхэма: заполняем промежуточные ячейки,
        // чтобы быстрый мазок не оставлял пропусков.
        var dx = Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;

        var dy = -Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;

        var error = dx + dy;

        while (true)
        {
            RecordStrokeCell(x0, y0);

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            var twiceError = 2 * error;

            if (twiceError >= dy)
            {
                error += dy;
                x0 += sx;
            }

            if (twiceError <= dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    private void FinishStroke()
    {
        _isPainting = false;
        _lastStrokeCell = null;

        var changes = new Dictionary<(int X, int Y), uint>(
            _strokeChanges);

        _strokeChanges.Clear();

        if (changes.Count > 0)
        {
            _document.ApplyTileStroke(changes);
        }
    }

    protected override void OnDetachingFromTree()
    {
        if (_isPainting)
        {
            FinishStroke();
        }

        base.OnDetachingFromTree();
    }
    private bool TryGetGridLayout(
    out float originX,
    out float originY,
    out float cellSize)
    {
        const float sidebarWidth = 310.0f;
        const float margin = 12.0f;

        var map = _document.Definition;

        originX = 0.0f;
        originY = 0.0f;
        cellSize = 0.0f;

        var availableWidth =
            Bounds.Width - sidebarWidth - margin * 3.0f;

        var availableHeight =
            Bounds.Height - margin * 3.0f;

        if (availableWidth <= 0.0f || availableHeight <= 0.0f)
        {
            return false;
        }

        cellSize = MathF.Min(
            40.0f,
            MathF.Min(
                availableWidth / map.Width,
                availableHeight / map.Height));

        if (cellSize < 1.0f)
        {
            return false;
        }

        var mapWidth = cellSize * map.Width;
        var mapHeight = cellSize * map.Height;

        originX =
            Bounds.X + sidebarWidth + margin +
            MathF.Max(0.0f, (availableWidth - mapWidth) * 0.5f);

        originY =
            Bounds.Y + margin +
            MathF.Max(0.0f, (availableHeight - mapHeight) * 0.5f);

        return true;
    }

    private bool TryGetMapCell(
        Vector2 position,
        out int x,
        out int y)
    {
        x = -1;
        y = -1;

        if (!TryGetGridLayout(
                out var originX,
                out var originY,
                out var cellSize))
        {
            return false;
        }

        var map = _document.Definition;

        x = (int)MathF.Floor((position.X - originX) / cellSize);
        y = (int)MathF.Floor((position.Y - originY) / cellSize);

        return
            x >= 0 && x < map.Width &&
            y >= 0 && y < map.Height;
    }

    private uint GetDisplayedTileId(int x, int y)
    {
        if (_strokeChanges.TryGetValue((x, y), out var previewTileId))
        {
            return previewTileId;
        }

        var map = _document.Definition;
        return map.Tiles[y * map.Width + x];
    }
    protected override void OnRender(UiRenderContext context)
    {
        base.OnRender(context);

        var map = _document.Definition;

        // Оставляем слева место под палитру.
        const float sidebarWidth = 310.0f;
        const float margin = 12.0f;

        var availableWidth = Bounds.Width - sidebarWidth - margin * 3.0f;
        var availableHeight = Bounds.Height - margin * 3.0f;

        if (availableWidth <= 0.0f || availableHeight <= 0.0f)
        {
            return;
        }

        var cellSize = MathF.Min(
            40.0f,
            MathF.Min(
                availableWidth / map.Width,
                availableHeight / map.Height));

        if (cellSize < 1.0f)
        {
            return;
        }

        var mapWidth = cellSize * map.Width;
        var mapHeight = cellSize * map.Height;

        var originX = Bounds.X + sidebarWidth +
                      margin +
                      MathF.Max(0.0f, (availableWidth - mapWidth) * 0.5f);

        var originY = Bounds.Y + margin +
                      MathF.Max(0.0f, (availableHeight - mapHeight) * 0.5f);

        context.DrawText(
            $"{map.Width} × {map.Height}",
            new Vector2(originX, Bounds.Y + 8.0f),
            13.0f,
            new UiColor(210, 210, 210, 255));

        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                var tileId = GetDisplayedTileId(x, y);

                var cell = new UiRect(
                    originX + x * cellSize,
                    originY + y * cellSize,
                    cellSize,
                    cellSize);

                if (tileId != 0 &&
                    _visuals.TryGet(tileId, out var visual))
                {
                    context.DrawTexture(
                        visual.Texture,
                        cell,
                        visual.UV);
                }
                else
                {
                    context.DrawRectangle(
                        cell,
                        tileId == 0
                            ? new UiColor(36, 36, 36, 255)
                            : new UiColor(70, 65, 75, 255));

                    if (tileId != 0 && cellSize >= 18.0f)
                    {
                        context.DrawText(
                            tileId.ToString(),
                            new Vector2(cell.X + 2.0f, cell.Y + 2.0f),
                            MathF.Min(11.0f, cellSize * 0.35f),
                            new UiColor(245, 245, 245, 255));
                    }
                }

                context.DrawRectangle(
                    cell,
                    new UiColor(65, 65, 65, 255),
                    filled: false);
            }
        }
    }
}