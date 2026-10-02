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

        var componentTypes =
            Editor.ComponentTypes.Types
                .Where(
                    componentType =>
                        componentType !=
                        typeof(Engine.Worlds.Spatial.WorldPositionComponent) &&
                        !document.Inspector
                            .GetComponentTypes(entity)
                            .Contains(componentType))
                .OrderBy(
                    static type =>
                        type.FullName ??
                        type.Name,
                    StringComparer.Ordinal)
                .ToArray();

        var duplicateNames =
            componentTypes
                .GroupBy(
                    static type =>
                        type.Name,
                    StringComparer.Ordinal)
                .Where(
                    static group =>
                        group.Count() > 1)
                .Select(
                    static group =>
                        group.Key)
                .ToHashSet(
                    StringComparer.Ordinal);

        var componentOptions =
            new Dictionary<string, Type>(
                StringComparer.Ordinal);

        foreach (var componentType in componentTypes)
        {
            var optionText =
                duplicateNames.Contains(
                    componentType.Name)
                    ? componentType.FullName ??
                      componentType.Name
                    : componentType.Name;

            componentOptions.Add(
                optionText,
                componentType);

            addComponent.AddOption(
                optionText);
        }

        addComponent.SelectionChanged +=
            (_, text) =>
            {
                if (!componentOptions.TryGetValue(
                        text,
                        out var componentType))
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
        if (!document.Inspector.TryGetComponent(
                entity,
                componentType,
                out _))
        {
            return;
        }

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