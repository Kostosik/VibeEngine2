using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Editor.Assets;
using Engine.Editor.UI.Authoring;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class EditorUiWorkspaceView :
    UiPanel
{
    private const float DockSplitterThickness = 6.0f;
    private const float MinSidePanelWidth = 140.0f;
    private const float MinPreviewWidth = 220.0f;

    private float _hierarchyRatio = 0.24f;
    private float _inspectorRatio = 0.24f;
    private readonly EditorContext? _editor;
    private readonly EditorDockSplitter _leftSplitter;
    private readonly EditorDockSplitter _rightSplitter;

    private readonly UiPanel _hierarchyHost;
    private readonly UiPanel _previewHost;
    private readonly UiPanel _inspectorHost;

    private readonly AssetBrowserPanelView? _assetBrowser;
    private readonly EditorDockSplitter? _bottomSplitter;

    private float _assetBrowserHeight = 220.0f;

    private const float MinAssetBrowserHeight = 120.0f;
    private const float MinTopWorkspaceHeight = 180.0f;

    public EditorUiWorkspaceView(
        EditorUiDocument document,
        EditorContext? editor = null,
        UiOverlayLayer? overlays = null,
        ITextureResourceManager? assetPreviewTextures = null, ITextureManager? filePreviewTextureManager = null)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        var hasAssetBrowserServices =
    editor is not null ||
    overlays is not null ||
    assetPreviewTextures is not null;

        if (hasAssetBrowserServices)
        {
            ArgumentNullException.ThrowIfNull(editor);
            ArgumentNullException.ThrowIfNull(overlays);
            ArgumentNullException.ThrowIfNull(assetPreviewTextures);
        }

        Document =
            document;

        Background =
            new UiColor(
                18,
                18,
                18,
                255);

        _hierarchyHost =
            CreateHost();

        _previewHost =
            CreateHost();

        _inspectorHost =
            CreateHost();

        _leftSplitter = new EditorDockSplitter(
    EditorDockResizeAxis.Horizontal,
    1.0f,
    () => CalculateDockWidths(Bounds.Width).Hierarchy,
    ResizeHierarchyWidth);

        _rightSplitter = new EditorDockSplitter(
            EditorDockResizeAxis.Horizontal,
            -1.0f,
            () => CalculateDockWidths(Bounds.Width).Inspector,
            ResizeInspectorWidth);

        AddChild(_leftSplitter);
        AddChild(_rightSplitter);

        Hierarchy =
            new EditorUiHierarchyPanelView(
                document);

        Preview =
            new EditorUiCanvasPreviewView(
                document,
                assetPreviewTextures,
                editor,
                filePreviewTextureManager);

        Inspector =
            new EditorUiInspectorPanelView(
                document,
                GetSelectedTexturePath,
                GetAvailableActionIds);

        _hierarchyHost.AddChild(
            Hierarchy);

        _previewHost.AddChild(
            Preview);

        _inspectorHost.AddChild(
            Inspector);

        if (editor is not null &&
    overlays is not null &&
    assetPreviewTextures is not null)
        {
            _assetBrowser =
                new AssetBrowserPanelView(
                    editor,
                    overlays,
                    assetPreviewTextures,
                    filePreviewTextureManager);

            _assetBrowser.AssetActivated +=
                entry => AssetActivated?.Invoke(entry);

            _bottomSplitter =
                new EditorDockSplitter(
                    EditorDockResizeAxis.Vertical,
                    -1.0f,
                    () => _assetBrowserHeight,
                    ResizeAssetBrowserHeight);

            AddChild(_bottomSplitter);
            AddChild(_assetBrowser);
        }

        AddChild(
            _hierarchyHost);

        AddChild(
            _previewHost);

        AddChild(
            _inspectorHost);
    }

    public AssetBrowserPanelView? AssetBrowser =>
    _assetBrowser;

    public event Action<EditorAssetEntry>? AssetActivated;

    public EditorUiDocument Document { get; }

    public EditorUiHierarchyPanelView Hierarchy { get; }

    public EditorUiCanvasPreviewView Preview { get; }

    public EditorUiInspectorPanelView Inspector { get; }

    public void ReleaseResources()
    {
        Preview.ReleaseResources();
        _assetBrowser?.ReleaseResources();
    }

    public void Refresh()
    {
        Hierarchy.Refresh();
        Inspector.Refresh();
        _assetBrowser?.Refresh();
    }

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        var browserHeight =
            _assetBrowser is null
                ? 0.0f
                : CalculateAssetBrowserHeight(
                    availableSize.Y);

        var splitterHeight =
            _bottomSplitter is null
                ? 0.0f
                : DockSplitterThickness;

        var topHeight = MathF.Max(
            0.0f,
            availableSize.Y -
            browserHeight -
            splitterHeight);

        var topSize = new Vector2(
            availableSize.X,
            topHeight);

        _hierarchyHost.Measure(
            context,
            topSize);

        _previewHost.Measure(
            context,
            topSize);

        _inspectorHost.Measure(
            context,
            topSize);

        _leftSplitter.Measure(
            context,
            topSize);

        _rightSplitter.Measure(
            context,
            topSize);

        if (_bottomSplitter is not null)
        {
            _bottomSplitter.Measure(
                context,
                new Vector2(
                    availableSize.X,
                    splitterHeight));
        }

        if (_assetBrowser is not null)
        {
            _assetBrowser.Measure(
                context,
                new Vector2(
                    availableSize.X,
                    browserHeight));
        }

        return availableSize;
    }

    private IReadOnlyList<string> GetAvailableActionIds()
    {
        var actionIds =
            new HashSet<string>(
                StringComparer.Ordinal);

        void Collect(EditorUiDocument document)
        {
            foreach (var element in document.EnumerateDepthFirst())
            {
                if (!string.IsNullOrWhiteSpace(element.Action))
                {
                    actionIds.Add(element.Action);
                }
            }
        }

        Collect(Document);

        if (_editor is not null)
        {
            foreach (var document in
                     _editor.Session.AllDocuments
                         .OfType<EditorUiDocument>())
            {
                Collect(document);
            }

            var assetsRoot =
                _editor.ActiveGameProject?.AssetsDirectoryPath;

            if (!string.IsNullOrWhiteSpace(assetsRoot) &&
                Directory.Exists(assetsRoot))
            {
                string[] paths;

                try
                {
                    paths =
                        Directory.GetFiles(
                            assetsRoot,
                            "*.ui",
                            SearchOption.AllDirectories);
                }
                catch (Exception exception) when (
                    exception is IOException or
                    UnauthorizedAccessException)
                {
                    paths = Array.Empty<string>();
                }

                foreach (var path in paths)
                {
                    try
                    {
                        Collect(
                            EditorUiAssetFile.Load(path));
                    }
                    catch (Exception exception) when (
                        exception is IOException or
                        InvalidDataException or
                        UnauthorizedAccessException or
                        ArgumentException or
                        FormatException or
                        OverflowException)
                    {
                        // Неисправный или недоступный файл
                        // не должен ломать Inspector.
                    }
                }
            }
        }

        return actionIds
            .OrderBy(
                static id => id,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    protected override void ArrangeCore(
    UiRect finalRect)
    {
        var widths = CalculateDockWidths(
            finalRect.Width);

        var x = finalRect.X;
        var y = finalRect.Y;

        var browserHeight =
            _assetBrowser is null
                ? 0.0f
                : CalculateAssetBrowserHeight(
                    finalRect.Height);

        var splitterHeight =
            _assetBrowser is null
                ? 0.0f
                : DockSplitterThickness;

        var height = MathF.Max(
            0.0f,
            finalRect.Height - browserHeight - splitterHeight);

        _hierarchyHost.Arrange(
            new UiRect(
                x,
                y,
                widths.Hierarchy,
                height));

        x += widths.Hierarchy;

        _leftSplitter.Arrange(
            new UiRect(
                x,
                y,
                DockSplitterThickness,
                height));

        x += DockSplitterThickness;

        _previewHost.Arrange(
            new UiRect(
                x,
                y,
                widths.Preview,
                height));

        x += widths.Preview;

        _rightSplitter.Arrange(
            new UiRect(
                x,
                y,
                DockSplitterThickness,
                height));

        x += DockSplitterThickness;

        _inspectorHost.Arrange(
            new UiRect(
                x,
                y,
                widths.Inspector,
                height));

        if (_assetBrowser is not null &&
    _bottomSplitter is not null)
        {
            var splitterY =
                finalRect.Y + height;

            _bottomSplitter.Arrange(
                new UiRect(
                    finalRect.X,
                    splitterY,
                    finalRect.Width,
                    DockSplitterThickness));

            _assetBrowser.Arrange(
                new UiRect(
                    finalRect.X,
                    splitterY + DockSplitterThickness,
                    finalRect.Width,
                    browserHeight));
        }
    }

    private (
    float Hierarchy,
    float Preview,
    float Inspector)
    CalculateDockWidths(float totalWidth)
    {
        var availableWidth = MathF.Max(
            0.0f,
            totalWidth - DockSplitterThickness * 2.0f);

        var minSideWidth = MathF.Min(
            MinSidePanelWidth,
            availableWidth / 3.0f);

        var minPreviewWidth = MathF.Min(
            MinPreviewWidth,
            MathF.Max(
                0.0f,
                availableWidth - minSideWidth * 2.0f));

        var hierarchyMax = MathF.Max(
            minSideWidth,
            availableWidth - minPreviewWidth - minSideWidth);

        var hierarchy = Math.Clamp(
            totalWidth * _hierarchyRatio,
            minSideWidth,
            hierarchyMax);

        var inspectorMax = MathF.Max(
            minSideWidth,
            availableWidth - minPreviewWidth - hierarchy);

        var inspector = Math.Clamp(
            totalWidth * _inspectorRatio,
            minSideWidth,
            inspectorMax);

        var preview = MathF.Max(
            0.0f,
            availableWidth - hierarchy - inspector);

        return (
            hierarchy,
            preview,
            inspector);
    }

    private void ResizeHierarchyWidth(float width)
    {
        var totalWidth = Bounds.Width;

        if (totalWidth <= 0.0f)
        {
            return;
        }

        var sizes = CalculateDockWidths(totalWidth);

        var availableWidth = MathF.Max(
            0.0f,
            totalWidth - DockSplitterThickness * 2.0f);

        var minSideWidth = MathF.Min(
            MinSidePanelWidth,
            availableWidth / 3.0f);

        var minPreviewWidth = MathF.Min(
            MinPreviewWidth,
            MathF.Max(
                0.0f,
                availableWidth - minSideWidth * 2.0f));

        var maxWidth = MathF.Max(
            minSideWidth,
            availableWidth -
            minPreviewWidth -
            sizes.Inspector);

        _hierarchyRatio = Math.Clamp(
            width,
            minSideWidth,
            maxWidth) / totalWidth;
    }

    private string? GetSelectedTexturePath()
    {
        var browser =
            _assetBrowser?.Editor.AssetBrowser;

        if (browser?.SelectedAsset is not
            { } selectedAsset)
        {
            return null;
        }

        var extension =
            Path.GetExtension(
                selectedAsset.Name);

        if (!extension.Equals(
                ".png",
                StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(
                ".jpg",
                StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(
                ".jpeg",
                StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(
                ".bmp",
                StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(
                ".tga",
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var relativePath =
            Path.GetRelativePath(
                browser.RootPath,
                selectedAsset.Path);

        return new AssetPath(
            relativePath).Value;
    }

    private void ResizeInspectorWidth(float width)
    {
        var totalWidth = Bounds.Width;

        if (totalWidth <= 0.0f)
        {
            return;
        }

        var sizes = CalculateDockWidths(totalWidth);

        var availableWidth = MathF.Max(
            0.0f,
            totalWidth - DockSplitterThickness * 2.0f);

        var minSideWidth = MathF.Min(
            MinSidePanelWidth,
            availableWidth / 3.0f);

        var minPreviewWidth = MathF.Min(
            MinPreviewWidth,
            MathF.Max(
                0.0f,
                availableWidth - minSideWidth * 2.0f));

        var maxWidth = MathF.Max(
            minSideWidth,
            availableWidth -
            minPreviewWidth -
            sizes.Hierarchy);

        _inspectorRatio = Math.Clamp(
            width,
            minSideWidth,
            maxWidth) / totalWidth;
    }

    private float CalculateAssetBrowserHeight(
    float totalHeight)
    {
        var availableHeight = MathF.Max(
            0.0f,
            totalHeight - DockSplitterThickness);

        var minBrowserHeight = MathF.Min(
            MinAssetBrowserHeight,
            availableHeight / 3.0f);

        var minMainHeight = MathF.Min(
            MinTopWorkspaceHeight,
            MathF.Max(
                0.0f,
                availableHeight - minBrowserHeight));

        var maxBrowserHeight = MathF.Max(
            minBrowserHeight,
            availableHeight - minMainHeight);

        return Math.Clamp(
            _assetBrowserHeight,
            minBrowserHeight,
            maxBrowserHeight);
    }

    private void ResizeAssetBrowserHeight(
        float height)
    {
        if (Bounds.Height <= 0.0f)
        {
            return;
        }

        var availableHeight = MathF.Max(
            0.0f,
            Bounds.Height - DockSplitterThickness);

        var minBrowserHeight = MathF.Min(
            MinAssetBrowserHeight,
            availableHeight / 3.0f);

        var minMainHeight = MathF.Min(
            MinTopWorkspaceHeight,
            MathF.Max(
                0.0f,
                availableHeight - minBrowserHeight));

        var maxBrowserHeight = MathF.Max(
            minBrowserHeight,
            availableHeight - minMainHeight);

        _assetBrowserHeight = Math.Clamp(
            height,
            minBrowserHeight,
            maxBrowserHeight);
    }

    private static UiPanel CreateHost()
    {
        return new UiPanel
        {
            Background =
                new UiColor(
                    28,
                    28,
                    28,
                    255),

            Padding =
                new UiThickness(
                    6.0f),

            HorizontalAlignment =
                UiHorizontalAlignment.Stretch,

            VerticalAlignment =
                UiVerticalAlignment.Stretch
        };
    }
}