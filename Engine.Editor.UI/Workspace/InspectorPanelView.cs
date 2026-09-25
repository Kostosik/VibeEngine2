using Engine.ECS.Entities;
using Engine.Editor;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class InspectorPanelView : UiPanel
{
    private readonly UiStackPanel _content;

    public InspectorPanelView(
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

        _content =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical,
                Spacing = 6.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Top
            };

        AddChild(
            _content);
    }

    public EditorContext Editor { get; }

    public void Refresh()
    {
        _content.ClearChildren();

        var document =
            Editor.ActiveDocument;

        if (document is null ||
            !document.Inspector.TryGetSelectedEntity(
                document.EntitySelection,
                out var entity))
        {
            _content.AddChild(
                new UiLabel(
                    "Nothing selected"));

            return;
        }

        _content.AddChild(
            new UiLabel(
                $"Entity {entity.Index}")
            {
                FontSize = 16.0f
            });

        var componentTypes =
            document.Inspector.GetComponentTypes(
                entity);

        foreach (var componentType in componentTypes)
        {
            _content.AddChild(
                new UiLabel(
                    componentType.Name)
                {
                    FontSize = 14.0f,

                    Color =
                        new UiColor(
                            210,
                            210,
                            210,
                            255)
                });

            foreach (var property in
                     document.Inspector.GetProperties(
                         entity,
                         componentType))
            {
                var value =
                    property.GetValue();

                _content.AddChild(
                    InspectorPropertyEditorFactory.Create(
                        document,
                        property));
            }
        }
    }
}