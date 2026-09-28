using Engine.Core.Math;
using Engine.Editor.UI.Authoring;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class EditorUiWorkspaceView :
    UiPanel
{
    private readonly UiPanel _hierarchyHost;
    private readonly UiPanel _previewHost;
    private readonly UiPanel _inspectorHost;

    public EditorUiWorkspaceView(
        EditorUiDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

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

        Hierarchy =
            new EditorUiHierarchyPanelView(
                document);

        Preview =
            new EditorUiCanvasPreviewView(
                document);

        Inspector =
            new EditorUiInspectorPanelView(
                document);

        _hierarchyHost.AddChild(
            Hierarchy);

        _previewHost.AddChild(
            Preview);

        _inspectorHost.AddChild(
            Inspector);

        AddChild(
            _hierarchyHost);

        AddChild(
            _previewHost);

        AddChild(
            _inspectorHost);
    }

    public EditorUiDocument Document { get; }

    public EditorUiHierarchyPanelView Hierarchy { get; }

    public EditorUiCanvasPreviewView Preview { get; }

    public EditorUiInspectorPanelView Inspector { get; }

    public void Refresh()
    {
        Hierarchy.Refresh();
        Inspector.Refresh();
    }

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        _hierarchyHost.Measure(
            context,
            availableSize);

        _previewHost.Measure(
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
        const float hierarchyRatio = 0.24f;
        const float inspectorRatio = 0.24f;

        var hierarchyWidth =
            finalRect.Width *
            hierarchyRatio;

        var inspectorWidth =
            finalRect.Width *
            inspectorRatio;

        var previewWidth =
            finalRect.Width -
            hierarchyWidth -
            inspectorWidth;

        _hierarchyHost.Arrange(
            new UiRect(
                finalRect.X,
                finalRect.Y,
                hierarchyWidth,
                finalRect.Height));

        _previewHost.Arrange(
            new UiRect(
                finalRect.X +
                hierarchyWidth,
                finalRect.Y,
                previewWidth,
                finalRect.Height));

        _inspectorHost.Arrange(
            new UiRect(
                finalRect.Right -
                inspectorWidth,
                finalRect.Y,
                inspectorWidth,
                finalRect.Height));
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