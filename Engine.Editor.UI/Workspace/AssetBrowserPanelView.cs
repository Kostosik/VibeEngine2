using Engine.Core.Math;
using Engine.Editor.Assets;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.Input;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class AssetBrowserPanelView : UiPanel
{
    private readonly UiStackPanel _toolbar;
    private readonly UiButton _upButton;
    private readonly UiButton _backButton;
    private readonly UiButton _forwardButton;
    private readonly UiScrollView _breadcrumbScrollView;
    private readonly UiStackPanel _breadcrumbItems;
    private readonly UiList _list;
    private readonly UiStackPanel _filterToolbar;
    private readonly UiDropdown _typeFilter;
    private readonly UiStackPanel _searchToolbar;
    private readonly UiTextBox _searchBox;
    private readonly UiButton _sortButton;
    private const long DoubleClickIntervalMilliseconds = 500;
    private readonly ITextureResourceManager _textures;
    private readonly UiButton _listModeButton;
    private readonly UiButton _gridModeButton;
    private readonly UiPanel _grid;

    private bool _isGridMode;
    private const float TileWidth = 116.0f;
    private const float TileHeight = 72.0f;
    private const float TileSpacing = 6.0f;

    private string? _lastClickedEntryPath;
    private string? _lastClickedDirectoryPath;
    private long _lastClickTimestamp;
    private readonly EditorAssetBrowserViewState _viewState = new();

    private readonly UiContextMenu _contextMenu;
    private readonly AssetPreviewPanelView _preview;
    private readonly List<(
        UiButton Button,
        EditorAssetEntry Entry)> _entryButtons = new();

    public event Action<EditorAssetEntry>? AssetActivated;


    public AssetBrowserPanelView(
        EditorContext editor,
        UiOverlayLayer overlays,
        ITextureResourceManager previewTextures)
    {
        ArgumentNullException.ThrowIfNull(
            editor);

        ArgumentNullException.ThrowIfNull(previewTextures);

        Editor = editor;

        _preview = new AssetPreviewPanelView(
            editor,
            previewTextures);
        _textures = previewTextures;
        _contextMenu = new UiContextMenu(overlays)
        {
            Width = 220.0f
        };
        Background =
            new UiColor(
                28,
                28,
                28,
                255);

        Padding =
            new UiThickness(
                6.0f);

        _toolbar =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Horizontal,

                Spacing = 4.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Top
            };

        _backButton = new UiButton("Back")
        {
            Width = 48.0f,
            Height = 28.0f,
            FontSize = 12.0f
        };

        _backButton.Clicked += () =>
        {
            var browser = Editor.AssetBrowser;

            if (browser is null)
            {
                return;
            }

            browser.NavigateBack();
            Refresh();
        };

        _forwardButton = new UiButton("Forward")
        {
            Width = 64.0f,
            Height = 28.0f,
            FontSize = 12.0f
        };

        _forwardButton.Clicked += () =>
        {
            var browser = Editor.AssetBrowser;

            if (browser is null)
            {
                return;
            }

            browser.NavigateForward();
            Refresh();
        };

        _upButton =
            new UiButton("Up")
            {
                Width = 60.0f,
                Height = 28.0f
            };

        _upButton.Clicked +=
            () =>
            {
                if (Editor.AssetBrowser is null)
                {
                    return;
                }

                ResetEntryClickTracking();

                if (Editor.AssetBrowser.NavigateUp())
                {
                    Refresh();
                }
            };

        _toolbar.AddChild(_backButton);
        _toolbar.AddChild(_forwardButton);
        _toolbar.AddChild(_upButton);

        _breadcrumbItems = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 2.0f,
            HorizontalAlignment = UiHorizontalAlignment.Left,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        _breadcrumbScrollView = new UiScrollView
        {
            Height = 36.0f,
            HorizontalScrolling = true,
            VerticalScrolling = false,
            ShowHorizontalScrollBar = true,
            ShowVerticalScrollBar = false,
            ScrollBarThickness = 8.0f,
            ScrollSpeed = 48.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        _breadcrumbScrollView.SetContent(_breadcrumbItems);

        _searchToolbar = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 4.0f,
            HorizontalAlignment = UiHorizontalAlignment.Left,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        _searchBox = new UiTextBox
        {
            Width = 172.0f,
            Height = 28.0f,
            FontSize = 12.0f,
            MaxLength = 256,
            Placeholder = "Search assets"
        };

        _searchBox.TextChanged += text =>
        {
            _viewState.SetSearchText(text);

            // Обновляем только список, а не всю панель:
            // поле поиска не будет пересоздаваться при вводе.
            RefreshEntries(Editor.AssetBrowser);
        };

        _sortButton = new UiButton("Name A-Z")
        {
            Width = 84.0f,
            Height = 28.0f,
            FontSize = 12.0f
        };

        _sortButton.Clicked += () =>
        {
            _viewState.ToggleSortDirection();

            _sortButton.Text =
                _viewState.SortDirection ==
                EditorAssetBrowserSortDirection.Ascending
                    ? "Name A-Z"
                    : "Name Z-A";

            RefreshEntries(Editor.AssetBrowser);
        };

        _searchToolbar.AddChild(_searchBox);
        _searchToolbar.AddChild(_sortButton);

        _filterToolbar = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 4.0f,
            HorizontalAlignment = UiHorizontalAlignment.Left,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        _typeFilter = new UiDropdown
        {
            Width = 160.0f,
            Height = 28.0f
        };

        _typeFilter.AddOption("All types");
        _typeFilter.AddOption("Images");
        _typeFilter.AddOption("Audio");
        _typeFilter.AddOption("Video");
        _typeFilter.AddOption("Fonts");
        _typeFilter.AddOption("Scripts");
        _typeFilter.AddOption("Models");
        _typeFilter.AddOption("Data");
        _typeFilter.AddOption("Other");

        _typeFilter.SetSelectedIndex(0);

        _typeFilter.SelectionChanged += (index, _) =>
        {
            _viewState.SetKindFilter(index switch
            {
                1 => EditorAssetKind.Image,
                2 => EditorAssetKind.Audio,
                3 => EditorAssetKind.Video,
                4 => EditorAssetKind.Font,
                5 => EditorAssetKind.Script,
                6 => EditorAssetKind.Model,
                7 => EditorAssetKind.Data,
                8 => EditorAssetKind.Other,
                _ => null
            });

            RefreshEntries(Editor.AssetBrowser);
        };

        _filterToolbar.AddChild(_typeFilter);

        _list =
            new UiList
            {
                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Stretch
            };

        _grid = new UiPanel
        {
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch,
            Visible = false
        };

        _listModeButton = new UiButton("List")
        {
            Width = 48.0f,
            Height = 28.0f,
            FontSize = 12.0f
        };

        _gridModeButton = new UiButton("Grid")
        {
            Width = 48.0f,
            Height = 28.0f,
            FontSize = 12.0f
        };

        _listModeButton.Clicked += () => SetViewMode(false);
        _gridModeButton.Clicked += () => SetViewMode(true);

        _toolbar.AddChild(_listModeButton);
        _toolbar.AddChild(_gridModeButton);

        AddChild(_toolbar);
        AddChild(_breadcrumbScrollView);
        AddChild(_searchToolbar);
        AddChild(_filterToolbar);
        AddChild(_list);
        AddChild(_grid);
        AddChild(_preview);
    }
    public AssetPreviewPanelView Preview => _preview;
    public EditorContext Editor { get; }

    private void SetViewMode(bool gridMode)
    {
        if (_isGridMode == gridMode)
        {
            return;
        }

        _isGridMode = gridMode;

        _list.Visible = !gridMode;
        _grid.Visible = gridMode;

        _listModeButton.Enabled = gridMode;
        _gridModeButton.Enabled = !gridMode;

        RefreshEntries(Editor.AssetBrowser);
    }

    private bool RegisterEntryClick(
    string entryPath,
    string currentDirectory)
    {
        var now = Environment.TickCount64;

        var isDoubleClick =
            string.Equals(
                _lastClickedEntryPath,
                entryPath,
                StringComparison.OrdinalIgnoreCase) &&
            string.Equals(
                _lastClickedDirectoryPath,
                currentDirectory,
                StringComparison.OrdinalIgnoreCase) &&
            now - _lastClickTimestamp <= DoubleClickIntervalMilliseconds;

        if (isDoubleClick)
        {
            ResetEntryClickTracking();
            return true;
        }

        _lastClickedEntryPath = entryPath;
        _lastClickedDirectoryPath = currentDirectory;
        _lastClickTimestamp = now;

        return false;
    }

    private void ResetEntryClickTracking()
    {
        _lastClickedEntryPath = null;
        _lastClickedDirectoryPath = null;
        _lastClickTimestamp = 0;
    }

    private void RefreshBreadcrumbs(EditorAssetBrowser? browser)
    {
        _breadcrumbItems.ClearChildren();

        if (browser is null)
        {
            return;
        }

        var rootName = Path.GetFileName(
            Path.TrimEndingDirectorySeparator(browser.RootPath));

        if (string.IsNullOrWhiteSpace(rootName))
        {
            rootName = browser.RootPath;
        }

        AddBreadcrumb(rootName, browser.RootPath, browser);

        var relativePath = Path.GetRelativePath(
            browser.RootPath,
            browser.CurrentPath);

        if (relativePath == ".")
        {
            return;
        }

        var currentPath = browser.RootPath;

        var segments = relativePath.Split(
            new[]
            {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
            },
            StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in segments)
        {
            currentPath = Path.Combine(currentPath, segment);

            AddBreadcrumb(segment, currentPath, browser);
        }
    }

    private void AddBreadcrumb(
        string name,
        string path,
        EditorAssetBrowser browser)
    {
        if (_breadcrumbItems.Children.Count > 0)
        {
            _breadcrumbItems.AddChild(
                new UiLabel("/")
                {
                    FontSize = 12.0f,
                    VerticalAlignment = UiVerticalAlignment.Center,
                    Color = new UiColor(140, 140, 140, 255)
                });
        }

        // Сохраняем компактность даже для длинных имён папок.
        var label = name.Length > 24
            ? name[..21] + "..."
            : name;

        var button = new UiButton(label)
        {
            Width = Math.Clamp(
                label.Length * 7.0f + 24.0f,
                52.0f,
                180.0f),
            Height = 26.0f,
            FontSize = 12.0f,
            Enabled = !string.Equals(
                path,
                browser.CurrentPath,
                StringComparison.OrdinalIgnoreCase),
            HandleRightClick = false
        };

        button.Clicked += () =>
        {
            if (browser.NavigateTo(path))
            {
                Refresh();
            }
        };

        _breadcrumbItems.AddChild(button);
    }

    public void Refresh()
    {
        var browser = Editor.AssetBrowser;

        if (browser is null)
        {
            RefreshBreadcrumbs(null);

            _upButton.Enabled = false;
            _backButton.Enabled = false;
            _forwardButton.Enabled = false;
            _preview.Refresh();
            RefreshEntries(null);
            return;
        }

        RefreshBreadcrumbs(browser);

        _upButton.Enabled =
            !string.Equals(
                browser.CurrentPath,
                browser.RootPath,
                StringComparison.OrdinalIgnoreCase);
        _backButton.Enabled = browser.CanNavigateBack;
        _forwardButton.Enabled = browser.CanNavigateForward;
        RefreshEntries(browser);
        _preview.Refresh();
    }

    private void RefreshEntries(EditorAssetBrowser? browser)
    {
        _entryButtons.Clear();
        _list.ClearItems();
        _grid.ClearChildren();

        _list.Visible = !_isGridMode;
        _grid.Visible = _isGridMode;

        if (browser is null)
        {
            _list.AddItem(
                new UiLabel("No asset source."));

            return;
        }

        var entries =
            _viewState.GetVisibleEntries(browser.Entries);

        foreach (var entry in entries)
        {

            var selected = browser.Selection.Contains(entry.Path);

            var icon = entry.IsDirectory ? "📁 " : "📄 ";
            var displayName = entry.Name.Length > 18
                ? entry.Name[..15] + "..."
                : entry.Name;

            var button = new UiButton(
                _isGridMode
                    ? icon + displayName
                    : (selected ? "> " : "  ") +
                      (entry.IsDirectory ? "[DIR] " : "[FILE] ") +
                      entry.Name)
            {
                Height = _isGridMode ? TileHeight : 28.0f,
                FontSize = _isGridMode ? 11.0f : 13.0f,
                HorizontalAlignment = UiHorizontalAlignment.Stretch
            };

            if (_isGridMode)
            {
                button.Width = TileWidth;
            }

            _entryButtons.Add((button, entry));

            button.Clicked += () =>
            {
                var isDoubleClick = RegisterEntryClick(
                    entry.Path,
                    browser.CurrentPath);

                if (entry.IsDirectory)
                {
                    if (!isDoubleClick)
                    {
                        browser.Select(entry.Path);
                        Refresh();
                        return;
                    }

                    if (browser.NavigateTo(entry.Path))
                    {
                        Refresh();
                    }

                    return;
                }

                browser.Select(entry.Path);

                if (isDoubleClick)
                {
                    AssetActivated?.Invoke(entry);
                }

                Refresh();
            };

            if (_isGridMode)
            {
                _grid.AddChild(button);
            }
            else
            {
                _list.AddItem(button);
            }
        }

        if (entries.Count == 0)
        {
            var emptyLabel = new UiLabel(
                browser.Entries.Count == 0
                    ? "Folder is empty"
                    : "No matching assets")
            {
                FontSize = 13.0f,
                Color = new UiColor(150, 150, 150, 255)
            };

            if (_isGridMode)
            {
                _grid.AddChild(emptyLabel);
            }
            else
            {
                _list.AddItem(emptyLabel);
            }
        }
    }


    protected override void OnPointerDown(
        UiPointerEvent pointer)
    {
        if (pointer.Button != InputMouseButton.Right)
        {
            return;
        }

        var browser = Editor.AssetBrowser;

        if (browser is null)
        {
            return;
        }

        var entryIndex = _entryButtons.FindIndex(
            item => item.Button.Bounds.Contains(
                pointer.Position));

        // Пустая область вне списка не должна открывать меню.
        if (entryIndex < 0 &&
            !_list.Bounds.Contains(pointer.Position) &&
            !_grid.Bounds.Contains(pointer.Position))
        {
            return;
        }

        _contextMenu.ClearItems();

        if (entryIndex >= 0)
        {
            var entry = _entryButtons[entryIndex].Entry;

            if (!browser.Select(entry.Path))
            {
                return;
            }

            if (entry.IsDirectory)
            {
                _contextMenu.AddItem(
                    "Open Folder",
                    () =>
                    {
                        var currentBrowser = Editor.AssetBrowser;

                        if (currentBrowser?.NavigateTo(entry.Path) == true)
                        {
                            Refresh();
                        }
                    });
            }
            else
            {
                _contextMenu.AddItem(
                    "Open / Preview",
                    () => AssetActivated?.Invoke(entry));
            }
        }

        _contextMenu.AddItem(
            "Refresh Current Folder",
            () =>
            {
                var currentBrowser = Editor.AssetBrowser;

                if (currentBrowser is null)
                {
                    return;
                }

                currentBrowser.Refresh();
                Refresh();
            });

        _contextMenu.Open(pointer.Position);

        pointer.Handled = true;
    }



    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        var browserWidth = availableSize.X * 0.62f;
        var previewWidth = MathF.Max(
            0.0f,
            availableSize.X - browserWidth - 8.0f);

        _toolbar.Measure(
            context,
            new Vector2(browserWidth, 28.0f));

        _breadcrumbScrollView.Measure(
            context,
            new Vector2(browserWidth, 36.0f));

        _searchToolbar.Measure(
            context,
            new Vector2(browserWidth, 28.0f));

        _filterToolbar.Measure(
            context,
            new Vector2(browserWidth, 28.0f));

        _list.Measure(
            context,
            new Vector2(
                browserWidth,
                float.PositiveInfinity));

        _grid.Measure(
    context,
    new Vector2(
        browserWidth,
        float.PositiveInfinity));

        _preview.Measure(
            context,
            new Vector2(previewWidth, availableSize.Y));

        return availableSize;
    }

    protected override void ArrangeCore(
        UiRect finalRect)
    {
        var content = finalRect.Deflate(Padding);

        const float rowHeight = 28.0f;
        const float breadcrumbHeight = 36.0f;
        const float spacing = 4.0f;
        const float paneSpacing = 8.0f;

        var browserWidth = MathF.Max(
            0.0f,
            (content.Width - paneSpacing) * 0.62f);

        var previewX =
            content.X + browserWidth + paneSpacing;

        var previewWidth =
            MathF.Max(0.0f, content.Right - previewX);

        _toolbar.Arrange(
            new UiRect(
                content.X,
                content.Y,
                browserWidth,
                rowHeight));

        var breadcrumbTop =
            content.Y + rowHeight + spacing;

        _breadcrumbScrollView.Arrange(
            new UiRect(
                content.X,
                breadcrumbTop,
                browserWidth,
                breadcrumbHeight));

        var searchTop =
            breadcrumbTop + breadcrumbHeight + spacing;

        _searchToolbar.Arrange(
            new UiRect(
                content.X,
                searchTop,
                browserWidth,
                rowHeight));

        var filterTop =
            searchTop + rowHeight + spacing;

        _filterToolbar.Arrange(
            new UiRect(
                content.X,
                filterTop,
                browserWidth,
                rowHeight));

        var listTop =
            filterTop + rowHeight + spacing;

        var listBounds = new UiRect(
            content.X,
            listTop,
            browserWidth,
            MathF.Max(0.0f, content.Bottom - listTop));

        _list.Arrange(listBounds);
        _grid.Arrange(listBounds);

        var columns = Math.Max(
            1,
            (int)((listBounds.Width + TileSpacing) /
                  (TileWidth + TileSpacing)));

        for (var i = 0; i < _entryButtons.Count; i++)
        {
            if (!_isGridMode)
            {
                break;
            }

            var column = i % columns;
            var row = i / columns;

            _entryButtons[i].Button.Arrange(
                new UiRect(
                    listBounds.X + column * (TileWidth + TileSpacing),
                    listBounds.Y + row * (TileHeight + TileSpacing),
                    TileWidth,
                    TileHeight));
        }

        _preview.Arrange(
            new UiRect(
                previewX,
                content.Y,
                previewWidth,
                content.Height));
    }
}