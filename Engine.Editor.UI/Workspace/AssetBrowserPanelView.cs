using Engine.Editor;
using Engine.Editor.Assets;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.Core.Math;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class AssetBrowserPanelView : UiPanel
{
    private readonly UiStackPanel _toolbar;
    private readonly UiButton _upButton;
    private readonly UiLabel _pathLabel;
    private readonly UiList _list;

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

        _list =
            new UiList
            {
                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Stretch
            };

        AddChild(
            _toolbar);

        AddChild(
            _list);
    }

    public EditorContext Editor { get; }

    public void Refresh()
    {
        _list.ClearItems();

        var browser =
            Editor.AssetBrowser;

        if (browser is null)
        {
            _pathLabel.Text =
                "Asset browser is not initialized.";

            _upButton.Enabled = false;

            _list.AddItem(
                new UiLabel(
                    "No asset source."));

            return;
        }

        _pathLabel.Text =
            browser.CurrentPath;

        _upButton.Enabled =
            !string.Equals(
                browser.CurrentPath,
                browser.RootPath,
                StringComparison.OrdinalIgnoreCase);

        foreach (var entry in browser.Entries)
        {
            var selected =
                !entry.IsDirectory &&
                browser.Selection.Contains(
                    entry.Path);

            var prefix =
                selected
                    ? "● "
                    : "  ";

            var icon =
                entry.IsDirectory
                    ? "[DIR] "
                    : "[FILE] ";

            var button =
                new UiButton(
                    prefix +
                    icon +
                    entry.Name)
                {
                    Height = 28.0f,

                    HorizontalAlignment =
                        UiHorizontalAlignment.Stretch
                };

            if (entry.IsDirectory)
            {
                button.Clicked +=
                    () =>
                    {
                        if (browser.NavigateTo(
                                entry.Path))
                        {
                            Refresh();
                        }
                    };
            }
            else
            {
                button.Clicked +=
                    () =>
                    {
                        browser.Select(
                            entry.Path);

                        Refresh();

                        Console.WriteLine(
                            $"[EDITOR ASSET] Selected: {entry.Path}");
                    };
            }

            _list.AddItem(
                button);
        }

        if (browser.Entries.Count == 0)
        {
            _list.AddItem(
                new UiLabel(
                    "Folder is empty")
                {
                    FontSize = 13.0f,

                    Color =
                        new UiColor(
                            150,
                            150,
                            150,
                            255)
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
        var content =
            finalRect.Deflate(
                Padding);

        const float toolbarHeight = 28.0f;

        _toolbar.Arrange(
            new UiRect(
                content.X,
                content.Y,
                content.Width,
                toolbarHeight));

        _list.Arrange(
            new UiRect(
                content.X,
                content.Y +
                toolbarHeight +
                4.0f,
                content.Width,
                MathF.Max(
                    0.0f,
                    content.Height -
                    toolbarHeight -
                    4.0f)));
    }
}