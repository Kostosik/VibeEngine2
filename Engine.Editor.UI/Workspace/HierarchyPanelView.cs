using Engine.ECS.Entities;
using Engine.Editor;
using Engine.Editor.Hierarchy;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class HierarchyPanelView :
    UiPanel
{
    private readonly UiStackPanel _items;

    public HierarchyPanelView(
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

        _items =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical,

                Spacing = 2.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Top
            };

        AddChild(
            _items);
    }

    public EditorContext Editor { get; }

    public void Refresh()
    {
        _items.ClearChildren();

        var document =
            Editor.ActiveDocument;

        if (document is null)
        {
            _items.AddChild(
                new UiLabel(
                    "No document")
                {
                    FontSize = 13.0f,

                    Color =
                        new UiColor(
                            150,
                            150,
                            150,
                            255)
                });

            return;
        }

        var source =
            new EcsHierarchySource(
                document.World);

        foreach (var node in
                 source.GetNodes())
        {
            if (node.Id is not EntityId entity)
            {
                continue;
            }

            var selected =
                document.EntitySelection.Contains(
                    entity);

            var prefix =
                selected
                    ? "● "
                    : "  ";

            var button =
                new UiButton(
                    prefix + node.Name)
                {
                    Height = 30.0f,

                    HorizontalAlignment =
                        UiHorizontalAlignment.Stretch
                };

            button.Clicked +=
                () =>
                {
                    document.EntitySelection.Set(
                        entity);
                };

            _items.AddChild(
                button);
        }

        if (_items.Children.Count == 0)
        {
            _items.AddChild(
                new UiLabel(
                    "No entities")
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
}