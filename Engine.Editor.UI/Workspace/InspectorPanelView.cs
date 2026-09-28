using Engine.ECS.Entities;
using Engine.Editor;
using Engine.Editor.Commands;
using Engine.Editor.Inspection;
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

        Padding =
            new UiThickness(
                6.0f);

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

        var addComponent =
            new UiDropdown();

        foreach (var componentType in
                 Editor.ComponentTypes.Types
                     .OrderBy(
                         static type =>
                             type.FullName ??
                             type.Name,
                         StringComparer.Ordinal))
        {
            if (componentType ==
                typeof(Engine.Worlds.Spatial.WorldPositionComponent))
            {
                continue;
            }

            if (document.Inspector
                    .GetComponentTypes(entity)
                    .Contains(componentType))
            {
                continue;
            }

            addComponent.AddOption(
                componentType.Name);
        }

        addComponent.SelectionChanged +=
            (_, text) =>
            {
                var componentType =
                    Editor.ComponentTypes.Types
                        .FirstOrDefault(
                            type =>
                                type.Name ==
                                text);

                if (componentType is null)
                {
                    return;
                }

                document.Execute(
                    new AddEditorComponentCommand(
                        document.World,
                        document.GetEntityReference(entity),
                        componentType));
            };

        _content.AddChild(
            new UiLabel(
                "Add Component")
            {
                FontSize = 13.0f,

                Color =
                    new UiColor(
                        170,
                        170,
                        170,
                        255)
            });

        _content.AddChild(
            addComponent);

        var componentTypes =
            document.Inspector.GetComponentTypes(
                entity);

        foreach (var componentType in componentTypes)
        {
            AddComponentSection(
                document,
                entity,
                componentType);
        }
    }

    private void AddComponentSection(
        Engine.Editor.Documents.EditorDocument document,
        EntityId entity,
        Type componentType)
    {
        var header =
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

        var label =
            new UiLabel(
                componentType.Name)
            {
                FontSize = 14.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Center,

                Color =
                    new UiColor(
                        210,
                        210,
                        210,
                        255)
            };

        header.AddChild(
            label);

        if (componentType !=
            typeof(Engine.Worlds.Spatial.WorldPositionComponent))
        {
            var remove =
                new UiButton("Remove")
                {
                    Width = 80.0f,
                    Height = 28.0f
                };

            remove.Clicked +=
                () =>
                {
                    if (!document.World.EcsWorld.Exists(
                            entity))
                    {
                        return;
                    }

                    document.Execute(
                        new RemoveEditorComponentCommand(
                            document.World,
                            document.GetEntityReference(entity),
                            componentType));
                };

            header.AddChild(
                remove);
        }

        _content.AddChild(
            header);

        foreach (var property in
                 document.Inspector.GetProperties(
                     entity,
                     componentType))
        {
            _content.AddChild(
                InspectorPropertyEditorFactory.Create(
                    document,
                    property));
        }
    }
}