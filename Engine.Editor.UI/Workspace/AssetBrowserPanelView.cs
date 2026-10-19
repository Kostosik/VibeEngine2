using Engine.Core.Assets;
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
    private readonly Dictionary<string, TextureHandle> _iconTextures =
    new(StringComparer.OrdinalIgnoreCase);
    private readonly UiButton _listModeButton;
    private readonly UiButton _gridModeButton;
    private readonly UiPanel _grid;
    private readonly UiScrollView _gridScrollView;

    private const float PaneSpacing = 8.0f;
    private const float MinimumPaneWidth = 180.0f;

    private float _browserWidthRatio = 0.62f;
    private UiRect _contentBounds;

    private readonly AssetBrowserSplitter _splitter;

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
        AssetBrowserEntryWidget Button,
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
        _splitter = new AssetBrowserSplitter(ResizeBrowserAt);
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
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        _gridScrollView = new UiScrollView
        {
            HorizontalScrolling = false,
            VerticalScrolling = true,
            ShowVerticalScrollBar = true,
            ScrollSpeed = 48.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        _gridScrollView.SetContent(_grid);
        _gridScrollView.Visible = false;

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
        AddChild(_gridScrollView);
        AddChild(_preview);
        AddChild(_splitter);
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
        _gridScrollView.Visible = gridMode;

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

    private TextureHandle GetEntryIcon(EditorAssetEntry entry)
    {
        var extension = Path.GetExtension(entry.Name).ToLowerInvariant();
        System.Diagnostics.Debug.WriteLine(
    $"[AssetBrowser] Loading icon for: {entry.Name}");
        var iconName = entry.IsDirectory
            ? "folder.png"
            : extension switch
            {
                ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tga"
                    => "image.png",

                ".wav" or ".mp3" or ".ogg" or ".flac"
                    => "audio.png",

                ".mp4" or ".avi" or ".mov" or ".webm"
                    => "video.png",

                ".ttf" or ".otf"
                    => "font.png",

                ".cs" or ".script" or ".json"
                    => "script.png",

                ".obj" or ".fbx" or ".gltf" or ".glb"
                    => "model.png",

                ".xml" or ".yaml" or ".yml" or ".toml"
                    => "data.png",

                _ => "file.png"
            };

        if (_iconTextures.TryGetValue(iconName, out var cached))
        {
            return cached;
        }

        try
        {
            var texture = _textures.Load(
                new AssetPath($"UI/EditorIcons/{iconName}"));
            System.Diagnostics.Debug.WriteLine(
    $"[AssetBrowser] Icon '{iconName}', valid: {texture.IsValid}");
            _iconTextures[iconName] = texture;

            return texture;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
    $"[AssetBrowser] Failed to load icon '{iconName}': {ex}");
            // Отсутствующая иконка не должна ломать Asset Browser.
            _iconTextures[iconName] = default;
            return default;
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
        _gridScrollView.ClearChildren();

        _list.Visible = !_isGridMode;
        _gridScrollView.Visible = _isGridMode;

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

            var button = new AssetBrowserEntryWidget(
                entry.Name,
                GetEntryIcon(entry),
                _isGridMode,
                selected);

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
            !_gridScrollView.Bounds.Contains(pointer.Position))
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
        var contentWidth = MathF.Max(
            0.0f,
            availableSize.X -
            Padding.Left -
            Padding.Right);

        var browserWidth = CalculateBrowserWidth(contentWidth);

        var previewWidth = MathF.Max(
            0.0f,
            contentWidth -
            browserWidth -
            PaneSpacing);

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

        var columns = Math.Max(
            1,
            (int)((browserWidth + TileSpacing) /
                  (TileWidth + TileSpacing)));

        var itemCount = Math.Max(
            1,
            _entryButtons.Count);

        var rows = (itemCount + columns - 1) / columns;

        _grid.Height =
            rows * TileHeight +
            (rows - 1) * TileSpacing;

        _gridScrollView.Measure(
            context,
            new Vector2(
                browserWidth,
                availableSize.Y));

        _preview.Measure(
            context,
            new Vector2(previewWidth, availableSize.Y));

        return availableSize;
    }

    protected override void ArrangeCore(
        UiRect finalRect)
    {
        var content = finalRect.Deflate(Padding);
        _contentBounds = content;
        const float rowHeight = 28.0f;
        const float breadcrumbHeight = 36.0f;
        const float spacing = 4.0f;
        const float paneSpacing = 8.0f;

        var browserWidth = CalculateBrowserWidth(content.Width);

        var previewX =
            content.X +
            browserWidth +
            PaneSpacing;

        var previewWidth = MathF.Max(
            0.0f,
            content.Right - previewX);

        var splitterBounds = new UiRect(
            content.X + browserWidth,
            content.Y,
            PaneSpacing,
            content.Height);

        _splitter.Arrange(splitterBounds);

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
        _gridScrollView.Arrange(listBounds);

        var gridBounds = _grid.Bounds;

        var columns = Math.Max(
            1,
            (int)((gridBounds.Width + TileSpacing) /
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
                    gridBounds.X +
                        column * (TileWidth + TileSpacing),
                    gridBounds.Y +
                        row * (TileHeight + TileSpacing),
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

    private float CalculateBrowserWidth(float contentWidth)
    {
        var availablePaneWidth = MathF.Max(
            0.0f,
            contentWidth - PaneSpacing);

        var minimumWidth = MathF.Min(
            MinimumPaneWidth,
            availablePaneWidth * 0.3f);

        var maximumWidth = MathF.Max(
            minimumWidth,
            availablePaneWidth - minimumWidth);

        return Math.Clamp(
            availablePaneWidth * _browserWidthRatio,
            minimumWidth,
            maximumWidth);
    }

    private void ResizeBrowserAt(float pointerX)
    {
        var availablePaneWidth = MathF.Max(
            0.0f,
            _contentBounds.Width - PaneSpacing);

        if (availablePaneWidth <= 0.0f)
        {
            return;
        }

        var minimumWidth = MathF.Min(
            MinimumPaneWidth,
            availablePaneWidth * 0.3f);

        var maximumWidth = MathF.Max(
            minimumWidth,
            availablePaneWidth - minimumWidth);

        var requestedWidth =
            pointerX - _contentBounds.X;

        var browserWidth = Math.Clamp(
            requestedWidth,
            minimumWidth,
            maximumWidth);

        _browserWidthRatio =
            browserWidth / availablePaneWidth;
    }

    private sealed class AssetBrowserSplitter : UiWidget
    {
        private readonly Action<float> _onDragged;
        private bool _isDragging;

        public AssetBrowserSplitter(Action<float> onDragged)
        {
            ArgumentNullException.ThrowIfNull(onDragged);

            _onDragged = onDragged;
        }

        protected override void OnRender(UiRenderContext context)
        {
            var color = IsHovered || _isDragging
                ? new UiColor(105, 145, 190, 255)
                : new UiColor(65, 68, 74, 255);

            var lineBounds = new UiRect(
                Bounds.X + (Bounds.Width - 2.0f) * 0.5f,
                Bounds.Y,
                2.0f,
                Bounds.Height);

            context.DrawRectangle(
                lineBounds,
                color,
                filled: true);
        }

        protected override void OnPointerDown(UiPointerEvent pointer)
        {
            if (pointer.Button != InputMouseButton.Left)
            {
                return;
            }

            _isDragging = true;

            pointer.RequestCapture();
            pointer.Handled = true;
        }

        protected override void OnPointerMove(UiPointerEvent pointer)
        {
            if (!_isDragging)
            {
                return;
            }

            _onDragged(pointer.Position.X);
            pointer.Handled = true;
        }

        protected override void OnPointerUp(UiPointerEvent pointer)
        {
            if (!_isDragging ||
                pointer.Button != InputMouseButton.Left)
            {
                return;
            }

            _isDragging = false;

            pointer.ReleaseCapture();
            pointer.Handled = true;
        }
    }
}