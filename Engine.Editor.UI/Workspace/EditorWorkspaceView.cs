using Engine.Core.Math;
using Engine.Editor;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class EditorWorkspaceView : UiPanel
{
    private readonly UiPanel _hierarchyHost;
    private readonly UiPanel _viewportHost;
    private readonly UiPanel _inspectorHost;

    public EditorWorkspaceView(
        EditorContext editor)
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
    }

    public EditorContext Editor { get; }

    public HierarchyPanelView Hierarchy { get; }

    public ViewportPanelView Viewport { get; }

    public InspectorPanelView Inspector { get; }

    public void Refresh()
    {
        Hierarchy.Refresh();
        Inspector.Refresh();
        Viewport.Refresh();
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

        return availableSize;
    }

    protected override void ArrangeCore(
        UiRect finalRect)
    {
        const float hierarchyWidthRatio = 0.22f;
        const float inspectorWidthRatio = 0.22f;

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
                finalRect.Height));

        _viewportHost.Arrange(
            new UiRect(
                finalRect.X +
                hierarchyWidth,
                finalRect.Y,
                viewportWidth,
                finalRect.Height));

        _inspectorHost.Arrange(
            new UiRect(
                finalRect.Right -
                inspectorWidth,
                finalRect.Y,
                inspectorWidth,
                finalRect.Height));
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