using Engine.Core.Math;
using Engine.Editor;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class EditorWorkspaceView : UiPanel
{
    private readonly UiPanel _hierarchyHost;
    private readonly UiPanel _viewportHost;
    private readonly UiPanel _inspectorHost;
    private readonly AssetBrowserPanelView _assetBrowser;
    private readonly AssetPreviewPanelView _assetPreview;
    public AssetPreviewPanelView AssetPreview =>
    _assetPreview;
    public EditorWorkspaceView(
        EditorContext editor,
        ITextureResourceManager assetPreviewTextures)
    {
        ArgumentNullException.ThrowIfNull(
            editor);

        Editor = editor;

        Background =
            new UiColor(
                18,
                18,
                18,
                255);

        _hierarchyHost =
            CreateHost(
                "Hierarchy");

        _viewportHost =
            CreateHost(
                "Viewport");

        _inspectorHost =
            CreateHost(
                "Inspector");

        AddChild(
            _hierarchyHost);

        AddChild(
            _viewportHost);

        AddChild(
            _inspectorHost);

        Hierarchy =
            new HierarchyPanelView(
                editor);

        Viewport =
            new ViewportPanelView(
                editor);

        Inspector =
            new InspectorPanelView(
                editor);

        _hierarchyHost.AddChild(
            Hierarchy);

        _viewportHost.AddChild(
            Viewport);

        _inspectorHost.AddChild(
            Inspector);

        _assetBrowser =
            new AssetBrowserPanelView(
                editor);

        _assetPreview =
    new AssetPreviewPanelView(
        editor,
        assetPreviewTextures);

        AddChild(
            _assetPreview);

        AddChild(
            _assetBrowser);
    }

    public EditorContext Editor { get; }

    public HierarchyPanelView Hierarchy { get; }

    public ViewportPanelView Viewport { get; }

    public InspectorPanelView Inspector { get; }

    public AssetBrowserPanelView AssetBrowser =>
        _assetBrowser;

    public void Refresh()
    {
        Hierarchy.Refresh();
        Inspector.Refresh();
        Viewport.Refresh();
        _assetBrowser.Refresh();
        _assetPreview.Refresh();
    }

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        _hierarchyHost.Measure(
            context,
            availableSize);

        _viewportHost.Measure(
            context,
            availableSize);

        _inspectorHost.Measure(
            context,
            availableSize);

        _assetBrowser.Measure(
            context,
            availableSize);

        _assetPreview.Measure(
    context,
    availableSize);

        return availableSize;
    }

    protected override void ArrangeCore(
    UiRect finalRect)
    {
        const float hierarchyWidthRatio = 0.22f;
        const float inspectorWidthRatio = 0.22f;
        const float assetAreaHeightRatio = 0.25f;
        const float assetBrowserWidthRatio = 0.65f;

        var assetAreaHeight =
            finalRect.Height *
            assetAreaHeightRatio;

        var topHeight =
            finalRect.Height -
            assetAreaHeight;

        var hierarchyWidth =
            finalRect.Width *
            hierarchyWidthRatio;

        var inspectorWidth =
            finalRect.Width *
            inspectorWidthRatio;

        var viewportWidth =
            finalRect.Width -
            hierarchyWidth -
            inspectorWidth;

        _hierarchyHost.Arrange(
            new UiRect(
                finalRect.X,
                finalRect.Y,
                hierarchyWidth,
                topHeight));

        _viewportHost.Arrange(
            new UiRect(
                finalRect.X +
                hierarchyWidth,
                finalRect.Y,
                viewportWidth,
                topHeight));

        _inspectorHost.Arrange(
            new UiRect(
                finalRect.Right -
                inspectorWidth,
                finalRect.Y,
                inspectorWidth,
                topHeight));

        var assetBrowserWidth =
            finalRect.Width *
            assetBrowserWidthRatio;

        var assetPreviewWidth =
            finalRect.Width -
            assetBrowserWidth;

        var assetAreaY =
            finalRect.Y +
            topHeight;

        _assetBrowser.Arrange(
            new UiRect(
                finalRect.X,
                assetAreaY,
                assetBrowserWidth,
                assetAreaHeight));

        _assetPreview.Arrange(
            new UiRect(
                finalRect.X +
                assetBrowserWidth,
                assetAreaY,
                assetPreviewWidth,
                assetAreaHeight));
    }

    private static UiPanel CreateHost(
        string title)
    {
        var host =
            new UiPanel
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

        var content =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical,

                Spacing = 6.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Stretch
            };

        content.AddChild(
            new UiLabel(
                title)
            {
                FontSize = 14.0f,

                Height = 22.0f,

                VerticalAlignment =
                    UiVerticalAlignment.Top,

                Color =
                    new UiColor(
                        190,
                        190,
                        190,
                        255)
            });

        host.AddChild(
            content);

        return host;
    }
}