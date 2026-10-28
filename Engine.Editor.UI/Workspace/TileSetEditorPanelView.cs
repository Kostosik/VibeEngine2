using Engine.Editor.Assets;
using Engine.Editor.Documents;
using Engine.Editor.UI.Dialogs;
using Engine.Graphics.Commands;
using Engine.Graphics.Tilemaps;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;
using System.Globalization;

namespace Engine.Editor.UI.Workspace;

public sealed class TileSetEditorPanelView : UiPanel
{
    private readonly TileSetDocument _document;
    private readonly EditorAssetPickerDialog? _assetPickerDialog;

    private readonly UiLabel _summaryLabel;
    private readonly UiLabel _messageLabel;

    private readonly UiStackPanel _atlasRows;
    private readonly UiStackPanel _tileRows;

    private readonly UiTextBox _newAtlasId;
    private readonly UiPickerField _newAtlasTexture;
    private readonly UiTextBox _newAtlasTileWidth;
    private readonly UiTextBox _newAtlasTileHeight;

    private readonly UiTextBox _newTileId;
    private readonly UiTextBox _newTileName;
    private readonly UiDropdown _newTileAtlas;
    private readonly UiTextBox _newTileAtlasIndex;
    private readonly UiButton _addTileButton;

    public TileSetDocument Document => _document;

    public TileSetEditorPanelView(
        TileSetDocument document,
        EditorAssetPickerDialog? assetPickerDialog = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        _document = document;
        _assetPickerDialog = assetPickerDialog;

        Padding = new UiThickness(12.0f);
        Background = new UiColor(28, 28, 28, 255);

        _summaryLabel = new UiLabel
        {
            Height = 28.0f,
            FontSize = 13.0f
        };

        _messageLabel = new UiLabel
        {
            Height = 30.0f,
            FontSize = 12.0f,
            Color = new UiColor(220, 180, 100, 255)
        };

        _atlasRows = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 6.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch
        };

        _tileRows = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 6.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch
        };

        _newAtlasId = CreateTextBox("Atlas ID", 140.0f);
        _newAtlasTexture = new UiPickerField
        {
            Width = 260.0f,
            Height = 30.0f,
            FontSize = 12.0f,
            Placeholder = "Select atlas texture"
        };

        _newAtlasTileWidth = CreateTextBox("Tile width", 85.0f);
        _newAtlasTileHeight = CreateTextBox("Tile height", 85.0f);

        _newAtlasTileWidth.SetText("32");
        _newAtlasTileHeight.SetText("32");

        _newTileId = CreateTextBox("Tile ID", 80.0f);
        _newTileName = CreateTextBox("Display name", 145.0f);
        _newTileAtlas = new UiDropdown
        {
            Width = 160.0f,
            Height = 30.0f
        };
        _newTileAtlasIndex = CreateTextBox("Atlas index", 95.0f);

        var layout = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 8.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        layout.AddChild(new UiLabel("Tile Set")
        {
            Height = 30.0f,
            FontSize = 20.0f
        });

        layout.AddChild(new UiLabel(document.DisplayName)
        {
            Height = 24.0f,
            FontSize = 13.0f,
            Color = new UiColor(170, 170, 170, 255)
        });

        _addTileButton = new UiButton("Add Tile")
        {
            Width = 95.0f,
            Height = 30.0f
        };

        _addTileButton.Clicked += AddTile;
        layout.AddChild(_summaryLabel);

        layout.AddChild(CreateAtlasSection());
        layout.AddChild(CreateTileSection());
        layout.AddChild(_messageLabel);

        var pageScroll = new UiScrollView
        {
            HorizontalScrolling = false,
            VerticalScrolling = true,
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        pageScroll.SetContent(layout);
        AddChild(pageScroll);



        // Сначала создаём секции и их элементы, затем
        // подписываемся на изменения документа.
        _document.Changed += OnDocumentChanged;

        Refresh();
    }

    public void ReleaseResources()
    {
        _document.Changed -= OnDocumentChanged;
    }

    public void Refresh()
    {
        var definition = _document.Definition;

        _summaryLabel.Text =
            $"Atlases: {definition.Atlases.Count}  •  " +
            $"Tiles: {definition.Tiles.Count}  •  " +
            $"Status: {(_document.IsDirty ? "Modified" : "Saved")}";

        RefreshAtlases(definition);
        RefreshTiles(definition);
        RefreshNewTileAtlas(definition);
    }

    private UiWidget CreateAtlasSection()
    {
        var content = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 8.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch
        };

        content.AddChild(new UiLabel("Add Atlas")
        {
            Height = 24.0f,
            FontSize = 15.0f
        });

        var form = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 6.0f,
            Height = 34.0f
        };

        form.AddChild(_newAtlasId);
        form.AddChild(_newAtlasTexture);
        form.AddChild(_newAtlasTileWidth);
        form.AddChild(_newAtlasTileHeight);

        var addButton = new UiButton("Add Atlas")
        {
            Width = 100.0f,
            Height = 30.0f
        };

        addButton.Clicked += AddAtlas;
        form.AddChild(addButton);

        _newAtlasTexture.SelectionRequested +=
            () => ShowTexturePicker(
                _newAtlasTexture,
                "Select atlas texture");

        content.AddChild(form);

        content.AddChild(new UiLabel("Existing Atlases")
        {
            Height = 24.0f,
            FontSize = 15.0f
        });

        var scroll = CreateScrollView(200.0f);
        scroll.SetContent(_atlasRows);
        content.AddChild(scroll);

        var expander = new UiExpander("Atlases")
        {
            IsExpanded = true,
            HeaderHeight = 32.0f,
            ContentSpacing = 6.0f,
            ContentPadding = new UiThickness(8.0f)
        };

        expander.SetContent(content);

        return expander;
    }

    private UiWidget CreateTileSection()
    {
        var content = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 8.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch
        };

        content.AddChild(new UiLabel("Add Tile")
        {
            Height = 24.0f,
            FontSize = 15.0f
        });

        var form = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 6.0f,
            Height = 34.0f
        };

        form.AddChild(_newTileId);
        form.AddChild(_newTileName);
        form.AddChild(_newTileAtlas);
        form.AddChild(_newTileAtlasIndex);
        form.AddChild(_addTileButton);

        content.AddChild(form);

        content.AddChild(new UiLabel("Existing Tiles")
        {
            Height = 24.0f,
            FontSize = 15.0f
        });

        var scroll = CreateScrollView(260.0f);
        scroll.SetContent(_tileRows);
        content.AddChild(scroll);

        var expander = new UiExpander("Tiles")
        {
            IsExpanded = true,
            HeaderHeight = 32.0f,
            ContentSpacing = 6.0f,
            ContentPadding = new UiThickness(8.0f)
        };

        expander.SetContent(content);

        return expander;
    }

    private void AddAtlas()
    {
        try
        {
            var current = _document.Definition;

            var atlas = new TileSetAtlasDefinition(
                _newAtlasId.Text,
                _newAtlasTexture.SelectedText ?? string.Empty,
                ReadInt(_newAtlasTileWidth, "Tile width"),
                ReadInt(_newAtlasTileHeight, "Tile height"));

            var updated = new TileSetDefinition(
                current.FormatVersion,
                current.Atlases.Append(atlas).ToArray(),
                current.Tiles);

            _document.UpdateDefinition(updated);

            _newAtlasId.SetText(string.Empty);
            _newAtlasTexture.SelectedText = null;

            // Сохраняем размеры по умолчанию для следующего атласа.
            _newAtlasTileWidth.SetText("32");
            _newAtlasTileHeight.SetText("32");

            SelectNewTileAtlas(atlas.Id);

            ShowMessage($"Atlas '{atlas.Id}' added.", success: true);
            Refresh();
        }
        catch (Exception exception)
        {
            ShowMessage(exception.Message);
        }
    }

    private void ApplyAtlas(
        string originalId,
        UiTextBox idBox,
        UiPickerField texturePicker,
        UiTextBox widthBox,
        UiTextBox heightBox)
    {
        try
        {
            var current = _document.Definition;

            var updatedAtlas = new TileSetAtlasDefinition(
                idBox.Text,
                texturePicker.SelectedText ?? string.Empty,
                ReadInt(widthBox, "Tile width"),
                ReadInt(heightBox, "Tile height"));

            var atlases = current.Atlases
                .Select(atlas =>
                    atlas.Id == originalId
                        ? updatedAtlas
                        : atlas)
                .ToArray();

            // При переименовании атласа сохраняем ссылки тайлов
            // на него в пределах текущего TileSet.
            var tiles = current.Tiles
                .Select(tile =>
                    tile.AtlasId == originalId &&
                    updatedAtlas.Id != originalId
                        ? new TileSetTileDefinition(
                            tile.Id,
                            updatedAtlas.Id,
                            tile.AtlasIndex,
                            tile.Name)
                        : tile)
                .ToArray();

            _document.UpdateDefinition(
                new TileSetDefinition(
                    current.FormatVersion,
                    atlases,
                    tiles));

            ShowMessage($"Atlas '{updatedAtlas.Id}' updated.", success: true);
            Refresh();
        }
        catch (Exception exception)
        {
            ShowMessage(exception.Message);
        }
    }

    private void RemoveAtlas(string atlasId)
    {
        var current = _document.Definition;

        if (current.Tiles.Any(tile => tile.AtlasId == atlasId))
        {
            ShowMessage(
                $"Atlas '{atlasId}' is still referenced by tiles. " +
                "Remove or reassign those tiles first.");

            return;
        }

        var atlases = current.Atlases
            .Where(atlas => atlas.Id != atlasId)
            .ToArray();

        UpdateDefinition(atlases, current.Tiles, "Atlas removed.");
    }

    private void AddTile()
    {
        try
        {
            var current = _document.Definition;

            if (string.IsNullOrWhiteSpace(_newTileAtlas.SelectedText))
            {
                ShowMessage("Add an atlas before adding tiles.");
                return;
            }

            var tile = new TileSetTileDefinition(
                ReadUInt(_newTileId, "Tile ID"),
                _newTileAtlas.SelectedText,
                ReadInt(_newTileAtlasIndex, "Atlas index"),
                _newTileName.Text);

            var updated = new TileSetDefinition(
                current.FormatVersion,
                current.Atlases,
                current.Tiles.Append(tile).ToArray());

            _document.UpdateDefinition(updated);

            _newTileId.SetText(string.Empty);
            _newTileName.SetText(string.Empty);
            _newTileAtlasIndex.SetText(string.Empty);

            ShowMessage($"Tile '{tile.Name}' added.", success: true);
            Refresh();
        }
        catch (Exception exception)
        {
            ShowMessage(exception.Message);
        }
    }

    private void ApplyTile(
        uint tileId,
        UiTextBox nameBox,
        UiDropdown atlasDropdown,
        UiTextBox indexBox)
    {
        try
        {
            var current = _document.Definition;

            if (string.IsNullOrWhiteSpace(atlasDropdown.SelectedText))
            {
                throw new InvalidDataException(
                    "Select an atlas for the tile.");
            }

            var updatedTile = new TileSetTileDefinition(
                tileId,
                atlasDropdown.SelectedText,
                ReadInt(indexBox, "Atlas index"),
                nameBox.Text);

            var tiles = current.Tiles
                .Select(tile => tile.Id == tileId ? updatedTile : tile)
                .ToArray();

            UpdateDefinition(
                current.Atlases,
                tiles,
                $"Tile '{updatedTile.Name}' updated.");
        }
        catch (Exception exception)
        {
            ShowMessage(exception.Message);
        }
    }

    private void RemoveTile(uint tileId)
    {
        var current = _document.Definition;

        var tiles = current.Tiles
            .Where(tile => tile.Id != tileId)
            .ToArray();

        UpdateDefinition(
            current.Atlases,
            tiles,
            $"Tile {tileId} removed.");
    }

    private void UpdateDefinition(
        IReadOnlyList<TileSetAtlasDefinition> atlases,
        IReadOnlyList<TileSetTileDefinition> tiles,
        string successMessage)
    {
        try
        {
            var current = _document.Definition;

            _document.UpdateDefinition(
                new TileSetDefinition(
                    current.FormatVersion,
                    atlases,
                    tiles));

            ShowMessage(successMessage, success: true);
            Refresh();
        }
        catch (Exception exception)
        {
            ShowMessage(exception.Message);
        }
    }

    private void RefreshAtlases(TileSetDefinition definition)
    {
        _atlasRows.ClearChildren();

        if (definition.Atlases.Count == 0)
        {
            _atlasRows.AddChild(new UiLabel("No atlases added yet.")
            {
                Height = 28.0f,
                FontSize = 13.0f,
                Color = new UiColor(160, 160, 160, 255)
            });

            return;
        }

        foreach (var atlas in definition.Atlases)
        {
            var row = new UiStackPanel
            {
                Orientation = UiOrientation.Horizontal,
                Spacing = 6.0f,
                Height = 34.0f
            };

            var idBox = CreateTextBox("Atlas ID", 130.0f);
            idBox.SetText(atlas.Id);

            var texturePicker = new UiPickerField(atlas.TexturePath)
            {
                Width = 250.0f,
                Height = 30.0f,
                FontSize = 12.0f,
                Placeholder = "Select atlas texture"
            };

            texturePicker.SelectionRequested +=
                () => ShowTexturePicker(
                    texturePicker,
                    "Select atlas texture");

            var widthBox = CreateTextBox("Tile width", 75.0f);
            widthBox.SetText(
                atlas.TileWidth.ToString(CultureInfo.InvariantCulture));

            var heightBox = CreateTextBox("Tile height", 75.0f);
            heightBox.SetText(
                atlas.TileHeight.ToString(CultureInfo.InvariantCulture));

            var applyButton = new UiButton("Apply")
            {
                Width = 65.0f,
                Height = 30.0f
            };

            applyButton.Clicked += () => ApplyAtlas(
                atlas.Id,
                idBox,
                texturePicker,
                widthBox,
                heightBox);

            var removeButton = new UiButton("Remove")
            {
                Width = 75.0f,
                Height = 30.0f
            };

            var atlasId = atlas.Id;
            removeButton.Clicked += () => RemoveAtlas(atlasId);

            row.AddChild(idBox);
            row.AddChild(texturePicker);
            row.AddChild(widthBox);
            row.AddChild(heightBox);
            row.AddChild(applyButton);
            row.AddChild(removeButton);

            _atlasRows.AddChild(row);
        }
    }

    private void RefreshTiles(TileSetDefinition definition)
    {
        _tileRows.ClearChildren();

        if (definition.Tiles.Count == 0)
        {
            _tileRows.AddChild(new UiLabel("No tiles added yet.")
            {
                Height = 28.0f,
                FontSize = 13.0f,
                Color = new UiColor(160, 160, 160, 255)
            });

            return;
        }

        foreach (var tile in definition.Tiles)
        {
            var row = new UiStackPanel
            {
                Orientation = UiOrientation.Horizontal,
                Spacing = 6.0f,
                Height = 34.0f
            };

            // ID намеренно не редактируется: значения тайлов уже могут
            // храниться в ячейках World. Для изменения ID надо отдельно
            // предусмотреть remap значений в этих World.
            row.AddChild(new UiLabel($"ID {tile.Id}")
            {
                Width = 65.0f,
                Height = 30.0f,
                FontSize = 12.0f,
                VerticalAlignment = UiVerticalAlignment.Center
            });

            var nameBox = CreateTextBox("Display name", 160.0f);
            nameBox.SetText(tile.Name);

            var atlasDropdown = CreateAtlasDropdown(
                definition.Atlases,
                tile.AtlasId);

            var indexBox = CreateTextBox("Atlas index", 90.0f);
            indexBox.SetText(
                tile.AtlasIndex.ToString(CultureInfo.InvariantCulture));

            var applyButton = new UiButton("Apply")
            {
                Width = 65.0f,
                Height = 30.0f
            };

            var tileId = tile.Id;

            applyButton.Clicked += () => ApplyTile(
                tileId,
                nameBox,
                atlasDropdown,
                indexBox);

            var removeButton = new UiButton("Remove")
            {
                Width = 75.0f,
                Height = 30.0f
            };

            removeButton.Clicked += () => RemoveTile(tileId);

            row.AddChild(nameBox);
            row.AddChild(atlasDropdown);
            row.AddChild(indexBox);
            row.AddChild(applyButton);
            row.AddChild(removeButton);

            _tileRows.AddChild(row);
        }
    }

    private void RefreshNewTileAtlas(TileSetDefinition definition)
    {
        var previousSelection = _newTileAtlas.SelectedText;

        _newTileAtlas.ClearOptions();

        foreach (var atlas in definition.Atlases)
        {
            _newTileAtlas.AddOption(atlas.Id);
        }

        var selectedIndex = definition.Atlases
            .Select((atlas, index) => (atlas, index))
            .Where(item => item.atlas.Id == previousSelection)
            .Select(item => item.index)
            .DefaultIfEmpty(0)
            .First();

        if (definition.Atlases.Count > 0)
        {
            _newTileAtlas.SetSelectedIndex(selectedIndex);
        }

        _addTileButton.Enabled = definition.Atlases.Count > 0;
    }

    private void SelectNewTileAtlas(string atlasId)
    {
        var index = _document.Definition.Atlases
            .Select((atlas, index) => (atlas, index))
            .Where(item => item.atlas.Id == atlasId)
            .Select(item => item.index)
            .DefaultIfEmpty(-1)
            .First();

        if (index >= 0)
        {
            _newTileAtlas.SetSelectedIndex(index);
        }
    }

    private static UiDropdown CreateAtlasDropdown(
        IReadOnlyList<TileSetAtlasDefinition> atlases,
        string selectedAtlasId)
    {
        var dropdown = new UiDropdown
        {
            Width = 160.0f,
            Height = 30.0f
        };

        foreach (var atlas in atlases)
        {
            dropdown.AddOption(atlas.Id);
        }

        var selectedIndex = atlases
            .Select((atlas, index) => (atlas, index))
            .Where(item => item.atlas.Id == selectedAtlasId)
            .Select(item => item.index)
            .DefaultIfEmpty(-1)
            .First();

        if (selectedIndex >= 0)
        {
            dropdown.SetSelectedIndex(selectedIndex);
        }

        return dropdown;
    }

    private void ShowTexturePicker(
        UiPickerField field,
        string title)
    {
        if (_assetPickerDialog is null)
        {
            ShowMessage("Asset Picker is not available.");
            return;
        }

        var opened = _assetPickerDialog.Show(
            title,
            IsTextureAsset,
            path => field.SelectedText = path);

        if (!opened)
        {
            ShowMessage("Open a game project to select textures.");
        }
    }

    private static bool IsTextureAsset(EditorAssetEntry entry)
    {
        if (entry.IsDirectory)
        {
            return false;
        }

        var extension = Path.GetExtension(entry.Name);

        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".tga", StringComparison.OrdinalIgnoreCase);
    }

    private void OnDocumentChanged()
    {
        Refresh();
    }

    private void ShowMessage(
        string message,
        bool success = false)
    {
        _messageLabel.Text = message;
        _messageLabel.Color = success
            ? new UiColor(130, 200, 150, 255)
            : new UiColor(220, 180, 100, 255);
    }

    private static int ReadInt(UiTextBox field, string label)
    {
        if (!int.TryParse(
                field.Text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var value))
        {
            throw new InvalidDataException(
                $"'{label}' must be an integer.");
        }

        return value;
    }

    private static uint ReadUInt(UiTextBox field, string label)
    {
        if (!uint.TryParse(
                field.Text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var value))
        {
            throw new InvalidDataException(
                $"'{label}' must be an unsigned integer.");
        }

        return value;
    }

    private static UiTextBox CreateTextBox(
        string placeholder,
        float width)
    {
        return new UiTextBox
        {
            Width = width,
            Height = 30.0f,
            FontSize = 12.0f,
            MaxLength = 256,
            Placeholder = placeholder
        };
    }

    private static UiScrollView CreateScrollView(float height)
    {
        return new UiScrollView
        {
            Height = height,
            HorizontalScrolling = false,
            VerticalScrolling = true,
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };
    }
}