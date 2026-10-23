using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Editor.Commands;
using Engine.Editor.Inspection;
using Engine.Editor.UI.Inspection;
using Engine.Graphics.Commands;
using Engine.Graphics2D.Sprites;
using Engine.Particles;
using Engine.Physics.Components;
using Engine.Physics.Joints;
using Engine.UI.Controls;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class InspectorPanelView : UiPanel
{
    private readonly UiScrollView _scrollView;
    private readonly UiStackPanel _content;
    private readonly InspectorPropertyEditorRegistry _propertyEditors;
    private readonly Func<string?>? _getSelectedParticleEffectPath;
    private readonly Func<string?>? _getSelectedTexturePath;
    public InspectorPanelView(
        EditorContext editor,
        InspectorPropertyEditorRegistry? propertyEditors = null,
        Func<string?>? getSelectedTexturePath = null, Func<string?>? getSelectedParticleEffectPath = null)
    {
        ArgumentNullException.ThrowIfNull(editor);

        Editor = editor;

        _propertyEditors =
            propertyEditors ??
            InspectorPropertyEditorRegistry.CreateDefault();

        _getSelectedTexturePath = getSelectedTexturePath;
        _getSelectedParticleEffectPath =
    getSelectedParticleEffectPath;
        Background = new UiColor(28, 28, 28, 255);
        Padding = new UiThickness(6.0f);

        _content = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 6.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        _scrollView = new UiScrollView
        {
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch,
            HorizontalScrolling = false,
            VerticalScrolling = true,
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true
        };

        _scrollView.SetContent(_content);
        AddChild(_scrollView);
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

        AddDistanceJointSection(
    document,
    entity,
    existingComponentTypeSet);

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
                        Editor.ComponentDependencies,
                        Editor.ComponentTypes));
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

        var existingComponentTypes =
    document.Inspector
        .GetComponentTypes(entity)
        .ToHashSet();

        var dependentComponentTypes =
            Editor.ComponentDependencies.GetDependents(
                componentType,
                existingComponentTypes);

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
                typeof(Engine.Worlds.Spatial.WorldPositionComponent) &&
            dependentComponentTypes.Count == 0)
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
                            componentType,
                            Editor.ComponentDependencies));
                };

            header.AddChild(
                remove);
        }

        if (dependentComponentTypes.Count > 0)
        {
            var dependentNames =
                string.Join(
                    ", ",
                    dependentComponentTypes.Select(
                        static type => type.Name));

            _content.AddChild(
                new UiLabel(
                    $"Required by: {dependentNames}.")
                {
                    FontSize = 11.0f,

                    Color =
                        new UiColor(
                            175,
                            165,
                            140,
                            255)
                });
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
            if (componentType == typeof(SpriteComponent) &&
                property.Name == nameof(SpriteComponent.TexturePath))
            {
                _content.AddChild(
                    CreateSpriteTexturePathProperty(
                        document,
                        property));

                continue;
            }

            if (componentType == typeof(ParticleEffectComponent) &&
    property.Name == nameof(ParticleEffectComponent.EffectPath))
            {
                _content.AddChild(
                    CreateParticleEffectPathProperty(
                        document,
                        property));

                continue;
            }

            _content.AddChild(
                _propertyEditors.Create(
                    document,
                    property));
        }
    }

    private UiStackPanel CreateParticleEffectPathProperty(
    Engine.Editor.Documents.EditorDocument document,
    EditorProperty property)
    {
        var container = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 4.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        container.AddChild(
            new UiLabel(property.DisplayName)
            {
                FontSize = 12.0f
            });

        var row = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 4.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        var textBox = new UiTextBox(
            property.GetValue()?.ToString() ?? string.Empty)
        {
            Width = 150.0f,
            Height = 32.0f,
            FontSize = 12.0f
        };

        textBox.Submitted += value =>
            document.Execute(
                new SetEditorPropertyCommand(
                    property,
                    value));

        row.AddChild(textBox);

        if (_getSelectedParticleEffectPath is not null)
        {
            var assignButton = new UiButton("Use selected")
            {
                Width = 90.0f,
                Height = 32.0f,
                FontSize = 11.0f
            };

            assignButton.Clicked += () =>
            {
                var path = _getSelectedParticleEffectPath();

                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }

                document.Execute(
                    new SetEditorPropertyCommand(
                        property,
                        path));

                Refresh();
            };

            row.AddChild(assignButton);

            var clearButton = new UiButton("Clear")
            {
                Width = 50.0f,
                Height = 32.0f,
                FontSize = 11.0f
            };

            clearButton.Clicked += () =>
            {
                document.Execute(
                    new SetEditorPropertyCommand(
                        property,
                        string.Empty));

                Refresh();
            };

            row.AddChild(clearButton);
        }

        container.AddChild(row);

        return container;
    }

    private UiStackPanel CreateSpriteTexturePathProperty(
    Engine.Editor.Documents.EditorDocument document,
    EditorProperty property)
    {
        var container = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 4.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        container.AddChild(
            new UiLabel(property.DisplayName)
            {
                FontSize = 12.0f
            });

        var row = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 4.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        var textBox = new UiTextBox(
            property.GetValue()?.ToString() ?? string.Empty)
        {
            Width = 150.0f,
            Height = 32.0f,
            FontSize = 12.0f
        };

        textBox.Submitted += value =>
            document.Execute(
                new SetEditorPropertyCommand(
                    property,
                    value));

        row.AddChild(textBox);

        if (_getSelectedTexturePath is not null)
        {
            var assignButton = new UiButton("Use selected")
            {
                Width = 90.0f,
                Height = 32.0f,
                FontSize = 11.0f
            };

            assignButton.Clicked += () =>
            {
                var path = _getSelectedTexturePath();

                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }

                document.Execute(
                    new SetEditorPropertyCommand(
                        property,
                        path));

                Refresh();
            };

            row.AddChild(assignButton);

            var clearButton = new UiButton("Clear")
            {
                Width = 50.0f,
                Height = 32.0f,
                FontSize = 11.0f
            };

            clearButton.Clicked += () =>
            {
                document.Execute(
                    new SetEditorPropertyCommand(
                        property,
                        string.Empty));

                Refresh();
            };

            row.AddChild(clearButton);
        }

        container.AddChild(row);

        return container;
    }

    private void AddDistanceJointSection(
    Engine.Editor.Documents.EditorDocument document,
    EntityId entity,
    IReadOnlySet<Type> existingComponentTypes)
    {
        if (existingComponentTypes.Contains(typeof(DistanceJoint2D)))
        {
            return;
        }

        var world = document.World.EcsWorld;

        if (!world.Has<WorldTransform2D>(entity) ||
            !world.Has<PhysicsBody2D>(entity))
        {
            return;
        }

        var targets = world.Inspector
            .GetEntities()
            .Where(candidate =>
                candidate != entity &&
                world.Has<WorldTransform2D>(candidate) &&
                world.Has<PhysicsBody2D>(candidate))
            .OrderBy(candidate => candidate.Index)
            .ToArray();

        var section = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 4.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        section.AddChild(
            new UiLabel("Distance Joint 2D")
            {
                FontSize = 13.0f,
                Color = new UiColor(170, 170, 170, 255)
            });

        if (targets.Length == 0)
        {
            section.AddChild(
                new UiLabel(
                    "Create another entity with PhysicsBody2D " +
                    "and WorldTransform2D to add a joint.")
                {
                    FontSize = 11.0f,
                    Color = new UiColor(175, 165, 140, 255)
                });

            _content.AddChild(section);
            return;
        }

        var targetDropdown = new UiDropdown();

        foreach (var target in targets)
        {
            var name = $"Entity {target.Index}";

            if (world.Has<EntityName>(target))
            {
                var entityName = world.Get<EntityName>(target);

                if (entityName.IsValid)
                {
                    name = entityName.Value;
                }
            }

            targetDropdown.AddOption(
                $"{name} [{target.Index}]");
        }

        var selectedTargetIndex = 0;

        targetDropdown.SetSelectedIndex(0);

        targetDropdown.SelectionChanged +=
            (index, _) => selectedTargetIndex = index;

        section.AddChild(
            new UiLabel("Connect to entity")
            {
                FontSize = 12.0f
            });

        section.AddChild(targetDropdown);

        var lengthBox = new UiTextBox("1")
        {
            Width = 140.0f,
            Height = 30.0f,
            FontSize = 12.0f
        };

        var lengthRow = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 6.0f,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        lengthRow.AddChild(
            new UiLabel("Length")
            {
                Width = 90.0f,
                FontSize = 12.0f
            });

        lengthRow.AddChild(lengthBox);
        section.AddChild(lengthRow);

        var errorLabel = new UiLabel(string.Empty)
        {
            FontSize = 11.0f,
            Color = new UiColor(230, 130, 130, 255)
        };

        var addButton = new UiButton("Add Joint")
        {
            Width = 120.0f,
            Height = 30.0f
        };

        addButton.Clicked += () =>
        {
            if (!Fixed32InspectorPropertyEditor.TryParseFixed32(
                    lengthBox.Text,
                    out var length) ||
                length <= Fixed32.Zero)
            {
                errorLabel.Text =
                    "Length must be a finite number greater than zero.";

                return;
            }

            try
            {
                document.Execute(
                    new AddDistanceJoint2DCommand(
                        document.World,
                        document.GetEntityReference(entity),
                        targets[selectedTargetIndex],
                        length));

                errorLabel.Text = string.Empty;
            }
            catch (Exception exception) when (
                exception is ArgumentException or InvalidOperationException)
            {
                errorLabel.Text = exception.Message;
            }
        };

        section.AddChild(addButton);
        section.AddChild(errorLabel);

        _content.AddChild(section);
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