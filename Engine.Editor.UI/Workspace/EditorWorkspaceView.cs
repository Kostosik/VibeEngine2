using Engine.Core.Math;
using Engine.Editor;
using Engine.Editor.Panels;
using Engine.Editor.Workspace;
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
        ApplyPanelState();

        Hierarchy.Refresh();
        Inspector.Refresh();
        Viewport.Refresh();
        _assetBrowser.Refresh();
        _assetPreview.Refresh();
    }

    private void ApplyPanelState()
    {
        foreach (var panel in GetPanelBindings())
        {
            panel.View.Visible =
                panel.Panel.IsOpen &&
                panel.Layout.Area !=
                EditorDockArea.Floating;
        }

        var centerPanels =
            GetPanels(
                EditorDockArea.Center);

        var activeCenter =
            centerPanels.FirstOrDefault(
                static panel =>
                    panel.Layout.IsActive);

        foreach (var panel in centerPanels)
        {
            panel.View.Visible =
                panel.Panel.IsOpen &&
                ReferenceEquals(
                    panel.View,
                    activeCenter.View);
        }
    }

    private List<
        (IEditorPanel Panel,
         EditorPanelLayout Layout,
         UiWidget View)>
        GetPanels(
            EditorDockArea area)
    {
        return GetPanelBindings()
            .Where(
                panel =>
                    panel.Panel.IsOpen &&
                    panel.Layout.Area == area)
            .OrderBy(
                panel => panel.Layout.Order)
            .ToList();
    }

    private IEnumerable<
        (IEditorPanel Panel,
         EditorPanelLayout Layout,
         UiWidget View)>
        GetPanelBindings()
    {
        yield return GetPanelBinding(
            "Hierarchy",
            _hierarchyHost);

        yield return GetPanelBinding(
            "Viewport",
            _viewportHost);

        yield return GetPanelBinding(
            "Inspector",
            _inspectorHost);

        yield return GetPanelBinding(
            "AssetBrowser",
            _assetBrowser);

        yield return GetPanelBinding(
            "AssetPreview",
            _assetPreview);
    }

    private (
        IEditorPanel Panel,
        EditorPanelLayout Layout,
        UiWidget View)
        GetPanelBinding(
            string panelId,
            UiWidget view)
    {
        var panel =
            Editor.Workspace.FindPanel(
                panelId)
            ?? throw new InvalidOperationException(
                $"Editor panel '{panelId}' is not registered.");

        var layout =
            Editor.Workspace.Layout.GetPanel(
                panelId);

        return (
            panel,
            layout,
            view);
    }

    private static float GetDockSize(
        IReadOnlyList<
            (IEditorPanel Panel,
             EditorPanelLayout Layout,
             UiWidget View)>
            panels,
        float availableSize,
        float fallback)
    {
        if (panels.Count == 0)
        {
            return 0.0f;
        }

        var size =
            panels
                .Select(
                    static panel =>
                        panel.Layout.Size)
                .DefaultIfEmpty(
                    fallback)
                .Max();

        if (size <= 0.0f)
        {
            size =
                fallback;
        }

        return MathF.Min(
            size,
            MathF.Max(
                0.0f,
                availableSize));
    }

    private static void ArrangeHorizontalPanels(
        IReadOnlyList<
            (IEditorPanel Panel,
             EditorPanelLayout Layout,
             UiWidget View)>
            panels,
        UiRect rect)
    {
        if (panels.Count == 0)
        {
            return;
        }

        var width =
            rect.Width /
            panels.Count;

        for (var i = 0;
             i < panels.Count;
             i++)
        {
            panels[i].View.Arrange(
                new UiRect(
                    rect.X +
                    width * i,
                    rect.Y,
                    i == panels.Count - 1
                        ? rect.Right -
                          (rect.X +
                           width * i)
                        : width,
                    rect.Height));
        }
    }

    private static void ArrangeVerticalPanels(
        IReadOnlyList<
            (IEditorPanel Panel,
             EditorPanelLayout Layout,
             UiWidget View)>
            panels,
        UiRect rect)
    {
        if (panels.Count == 0)
        {
            return;
        }

        var height =
            rect.Height /
            panels.Count;

        for (var i = 0;
             i < panels.Count;
             i++)
        {
            panels[i].View.Arrange(
                new UiRect(
                    rect.X,
                    rect.Y +
                    height * i,
                    rect.Width,
                    i == panels.Count - 1
                        ? rect.Bottom -
                          (rect.Y +
                           height * i)
                        : height));
        }
    }

    private static void ArrangeCenterPanel(
        IReadOnlyList<
            (IEditorPanel Panel,
             EditorPanelLayout Layout,
             UiWidget View)>
            panels,
        UiRect rect)
    {
        if (panels.Count == 0)
        {
            return;
        }

        var active =
            panels.FirstOrDefault(
                static panel =>
                    panel.Layout.IsActive);

        if (active.View is null)
        {
            active =
                panels[0];
        }

        active.View.Arrange(
            rect);
    }

    private void SetPanelVisibility(
        string panelId,
        UiWidget view)
    {
        var panel =
            Editor.Workspace.FindPanel(
                panelId);

        view.Visible =
            panel?.IsOpen == true;
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
        var leftPanels =
            GetPanels(
                EditorDockArea.Left);

        var rightPanels =
            GetPanels(
                EditorDockArea.Right);

        var topPanels =
            GetPanels(
                EditorDockArea.Top);

        var bottomPanels =
            GetPanels(
                EditorDockArea.Bottom);

        var centerPanels =
            GetPanels(
                EditorDockArea.Center);

        var leftWidth =
            GetDockSize(
                leftPanels,
                finalRect.Width,
                280.0f);

        var rightWidth =
            GetDockSize(
                rightPanels,
                finalRect.Width,
                300.0f);

        var topHeight =
            GetDockSize(
                topPanels,
                finalRect.Height,
                180.0f);

        var bottomHeight =
            GetDockSize(
                bottomPanels,
                finalRect.Height,
                220.0f);

        var contentX =
            finalRect.X +
            leftWidth;

        var contentY =
            finalRect.Y +
            topHeight;

        var contentWidth =
            MathF.Max(
                0.0f,
                finalRect.Width -
                leftWidth -
                rightWidth);

        var contentHeight =
            MathF.Max(
                0.0f,
                finalRect.Height -
                topHeight -
                bottomHeight);

        if (topPanels.Count > 0)
        {
            ArrangeHorizontalPanels(
                topPanels,
                new UiRect(
                    contentX,
                    finalRect.Y,
                    contentWidth,
                    topHeight));
        }

        if (bottomPanels.Count > 0)
        {
            ArrangeHorizontalPanels(
                bottomPanels,
                new UiRect(
                    contentX,
                    finalRect.Bottom -
                    bottomHeight,
                    contentWidth,
                    bottomHeight));
        }

        if (leftPanels.Count > 0)
        {
            ArrangeVerticalPanels(
                leftPanels,
                new UiRect(
                    finalRect.X,
                    contentY,
                    leftWidth,
                    contentHeight));
        }

        if (rightPanels.Count > 0)
        {
            ArrangeVerticalPanels(
                rightPanels,
                new UiRect(
                    finalRect.Right -
                    rightWidth,
                    contentY,
                    rightWidth,
                    contentHeight));
        }

        ArrangeCenterPanel(
            centerPanels,
            new UiRect(
                contentX,
                contentY,
                contentWidth,
                contentHeight));
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