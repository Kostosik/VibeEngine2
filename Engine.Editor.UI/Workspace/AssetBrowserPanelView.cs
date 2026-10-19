using Engine.Core.Math;
using Engine.Editor.Assets;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class AssetBrowserPanelView : UiPanel
{
    private readonly UiStackPanel _toolbar;
    private readonly UiButton _upButton;
    private readonly UiLabel _pathLabel;
    private readonly UiList _list;
    private readonly UiStackPanel _filterToolbar;
    private readonly UiDropdown _typeFilter;
    private readonly UiStackPanel _searchToolbar;
    private readonly UiTextBox _searchBox;
    private readonly UiButton _sortButton;

    private readonly EditorAssetBrowserViewState _viewState = new();

    public AssetBrowserPanelView(
        EditorContext editor)
    {
        ArgumentNullException.ThrowIfNull(
            editor);

        Editor = editor;

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

                if (Editor.AssetBrowser.NavigateUp())
                {
                    Refresh();
                }
            };

        _toolbar.AddChild(
            _upButton);

        _pathLabel =
            new UiLabel()
            {
                FontSize = 12.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Center,

                Color =
                    new UiColor(
                        170,
                        170,
                        170,
                        255)
            };

        _toolbar.AddChild(
            _pathLabel);

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

        AddChild(_toolbar);
        AddChild(_searchToolbar);
        AddChild(_filterToolbar);
        AddChild(_list);
    }

    public EditorContext Editor { get; }

    public void Refresh()
    {
        var browser = Editor.AssetBrowser;

        if (browser is null)
        {
            _pathLabel.Text =
                "Asset browser is not initialized.";

            _upButton.Enabled = false;

            RefreshEntries(null);
            return;
        }

        _pathLabel.Text = browser.CurrentPath;

        _upButton.Enabled =
            !string.Equals(
                browser.CurrentPath,
                browser.RootPath,
                StringComparison.OrdinalIgnoreCase);

        RefreshEntries(browser);
    }

    private void RefreshEntries(EditorAssetBrowser? browser)
    {
        _list.ClearItems();

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
            var selected =
                !entry.IsDirectory &&
                browser.Selection.Contains(entry.Path);

            var prefix = selected ? "> " : "  ";
            var icon = entry.IsDirectory ? "[DIR] " : "[FILE] ";

            var button = new UiButton(
                prefix + icon + entry.Name)
            {
                Height = 28.0f,
                HorizontalAlignment = UiHorizontalAlignment.Stretch
            };

            if (entry.IsDirectory)
            {
                button.Clicked += () =>
                {
                    if (browser.NavigateTo(entry.Path))
                    {
                        Refresh();
                    }
                };
            }
            else
            {
                button.Clicked += () =>
                {
                    browser.Select(entry.Path);

                    Refresh();

                    Console.WriteLine(
                        $"[EDITOR ASSET] Selected: {entry.Path}");
                };
            }

            _list.AddItem(button);
        }

        if (entries.Count == 0)
        {
            _list.AddItem(
                new UiLabel(
                    browser.Entries.Count == 0
                        ? "Folder is empty"
                        : "No matching assets")
                {
                    FontSize = 13.0f,
                    Color = new UiColor(150, 150, 150, 255)
                });
        }
    }

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        _toolbar.Measure(
            context,
            new Vector2(
                availableSize.X,
                28.0f));

        _searchToolbar.Measure(
    context,
    new Vector2(
        availableSize.X,
        28.0f));

        _filterToolbar.Measure(
    context,
    new Vector2(
        availableSize.X,
        28.0f));

        _list.Measure(
            context,
            new Vector2(
                availableSize.X,
                float.PositiveInfinity));

        return availableSize;
    }

    protected override void ArrangeCore(
        UiRect finalRect)
    {
        var content = finalRect.Deflate(Padding);

        const float rowHeight = 28.0f;
        const float spacing = 4.0f;

        _toolbar.Arrange(
            new UiRect(
                content.X,
                content.Y,
                content.Width,
                rowHeight));

        var searchTop = content.Y + rowHeight + spacing;

        _searchToolbar.Arrange(
            new UiRect(
                content.X,
                searchTop,
                content.Width,
                rowHeight));

        var filterTop = searchTop + rowHeight + spacing;

        _filterToolbar.Arrange(
            new UiRect(
                content.X,
                filterTop,
                content.Width,
                rowHeight));

        var listTop = filterTop + rowHeight + spacing;

        _list.Arrange(
            new UiRect(
                content.X,
                listTop,
                content.Width,
                MathF.Max(
                    0.0f,
                    content.Bottom - listTop)));
    }
}