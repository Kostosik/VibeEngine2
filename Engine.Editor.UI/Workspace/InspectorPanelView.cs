using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Editor.Commands;
using Engine.Editor.UI.Inspection;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class InspectorPanelView : UiPanel
{
    private readonly UiScrollView _scrollView;
    private readonly UiStackPanel _content;
    private readonly InspectorPropertyEditorRegistry _propertyEditors;
    public InspectorPanelView(
    EditorContext editor,
    InspectorPropertyEditorRegistry? propertyEditors = null)
    {
        ArgumentNullException.ThrowIfNull(editor);

        Editor = editor;

        _propertyEditors =
            propertyEditors ??
            InspectorPropertyEditorRegistry.CreateDefault();

        Background =
            new UiColor(28, 28, 28, 255);

        Padding =
            new UiThickness(6.0f);

        _content =
            new UiStackPanel
            {
                Orientation = UiOrientation.Vertical,
                Spacing = 6.0f,
                HorizontalAlignment = UiHorizontalAlignment.Stretch,
                VerticalAlignment = UiVerticalAlignment.Top
            };

        _scrollView =
    new UiScrollView
    {
        HorizontalAlignment =
            UiHorizontalAlignment.Stretch,

        VerticalAlignment =
            UiVerticalAlignment.Stretch,

        HorizontalScrolling = false,
        VerticalScrolling = true,

        ShowHorizontalScrollBar = false,
        ShowVerticalScrollBar = true
    };

        _scrollView.SetContent(
            _content);

        AddChild(
            _scrollView);
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

        // Компоненты, которые уже присутствуют у сущности.
        var existingComponentTypes =
            document.Inspector
                .GetComponentTypes(entity)
                .OrderBy(
                    static type =>
                        type.FullName ?? type.Name,
                    StringComparer.Ordinal)
                .ToArray();

        var existingComponentTypeSet =
            existingComponentTypes.ToHashSet();

        // Компоненты, которые можно добавить.
        var candidateComponentTypes =
            Editor.ComponentTypes.Types
                .Where(
                    componentType =>
                        componentType !=
                            typeof(Engine.Worlds.Spatial.WorldPositionComponent) &&
                        !existingComponentTypeSet.Contains(
                            componentType))
                .ToArray();

        var addableComponentTypes =
            candidateComponentTypes
                .Where(
                    componentType =>
                        Editor.ComponentDependencies.CanAdd(
                            componentType,
                            existingComponentTypeSet))
                .OrderBy(
                    static type =>
                        type.FullName ?? type.Name,
                    StringComparer.Ordinal)
                .ToArray();

        var blockedComponentTypes =
            candidateComponentTypes
                .Select(
                    componentType => new
                    {
                        ComponentType = componentType,
                        MissingDependencies =
                            Editor.ComponentDependencies.GetMissingDependencies(
                                componentType,
                                existingComponentTypeSet)
                    })
                .Where(
                    static item =>
                        item.MissingDependencies.Count > 0)
                .OrderBy(
                    static item =>
                        item.ComponentType.FullName ??
                        item.ComponentType.Name,
                    StringComparer.Ordinal)
                .ToArray();

        var duplicateNames =
            addableComponentTypes
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

        var addComponent =
            new UiDropdown();

        foreach (var componentType in addableComponentTypes)
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
                        componentType,
                        Editor.ComponentDependencies));
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

        foreach (var blocked in blockedComponentTypes)
        {
            var missingNames =
                string.Join(
                    ", ",
                    blocked.MissingDependencies.Select(
                        static type => type.Name));

            _content.AddChild(
                new UiLabel(
                    $"{blocked.ComponentType.Name} requires: {missingNames}.")
                {
                    FontSize = 11.0f,

                    HorizontalAlignment =
                        UiHorizontalAlignment.Stretch,

                    Color = new UiColor(
                        175,
                        165,
                        140,
                        255)
                });
        }

        // Отображаем именно существующие компоненты.
        foreach (var componentType in existingComponentTypes)
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

        if (componentType == typeof(WorldTransform2D))
        {
            var hasLocalTransform =
                document.World.EcsWorld.Has<LocalTransform2D>(
                    entity);

            _content.AddChild(
                new UiLabel(
                    hasLocalTransform
                        ? "Mode: Derived from LocalTransform2D / hierarchy"
                        : "Mode: Direct WorldTransform2D")
                {
                    FontSize = 12.0f,

                    Color = new UiColor(
                        170,
                        170,
                        170,
                        255)
                });

            if (hasLocalTransform)
            {
                var transform =
                    document.World.EcsWorld.Get<WorldTransform2D>(
                        entity);

                AddWorldTransformValue(
                    "World Position",
                    transform.Position.ToString());

                AddWorldTransformValue(
                    "World Rotation",
                    $"{transform.Rotation} rad");

                AddWorldTransformValue(
                    "World Scale",
                    transform.Scale.ToString());

                return;
            }
        }

        foreach (var property in
                 document.Inspector.GetProperties(
                     entity,
                     componentType))
        {
            _content.AddChild(
                _propertyEditors.Create(
                    document,
                    property));
        }
    }
    private void AddWorldTransformValue(
    string name,
    string value)
    {
        _content.AddChild(
            new UiLabel(
                $"{name}: {value}")
            {
                FontSize = 12.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                Color = new UiColor(
                    200,
                    200,
                    200,
                    255)
            });
    }
}