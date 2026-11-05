using Engine.ECS.Prefabs;
using Engine.Editor.Components;
using Engine.Editor.Documents;
using Engine.Editor.Inspection;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Layout;
using Engine.Worlds.Spatial;
using System.Reflection;

namespace Engine.Editor.UI.Workspace;

public sealed class PrefabDocumentView : UiPanel
{
    private readonly UiStackPanel _content;
    private PrefabEntityId _selectedEntityId;
    private readonly EditorComponentTypeRegistry _componentTypes;
    private readonly EditorComponentDependencyRegistry _componentDependencies;
    public PrefabDocumentView(
    PrefabDocument document,
    EditorComponentTypeRegistry componentTypes,
    EditorComponentDependencyRegistry componentDependencies)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(componentTypes);
        ArgumentNullException.ThrowIfNull(componentDependencies);

        Document = document;
        _componentTypes = componentTypes;
        _componentDependencies = componentDependencies;

        _selectedEntityId = document.Asset.RootEntity;

        Background = new UiColor(30, 30, 30, 255);
        Padding = new UiThickness(8.0f);

        _content = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 5.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        var scrollView = new UiScrollView
        {
            VerticalScrolling = true,
            HorizontalScrolling = false,
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true,
            ScrollSpeed = 32.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        scrollView.SetContent(_content);
        AddChild(scrollView);

        Refresh();
    }

    public PrefabDocument Document { get; }

    public void Refresh()
    {
        var asset = Document.Asset;

        _content.ClearChildren();

        _content.AddChild(new UiLabel(asset.Name)
        {
            FontSize = 20.0f,
            Color = UiColor.White
        });

        _content.AddChild(new UiLabel(
            $"Entities: {asset.Entities.Count}")
        {
            FontSize = 13.0f,
            Color = new UiColor(170, 170, 170, 255)
        });

        _content.AddChild(new UiLabel(
            $"Root: Entity {asset.RootEntity.Value}")
        {
            FontSize = 13.0f,
            Color = new UiColor(170, 170, 170, 255)
        });

        _content.AddChild(new UiLabel("Hierarchy")
        {
            FontSize = 16.0f,
            Color = UiColor.White
        });

        foreach (var entity in asset.Entities)
        {
            var isRoot = entity.Id == asset.RootEntity;
            var isSelected = entity.Id == _selectedEntityId;

            var prefix = isSelected ? "> " : "  ";
            var rootSuffix = isRoot ? " [Root]" : string.Empty;

            var button = new UiButton(
                $"{prefix}Entity {entity.Id.Value}{rootSuffix}")
            {
                Height = 30.0f,
                FontSize = 13.0f,
                HorizontalAlignment = UiHorizontalAlignment.Stretch
            };

            var entityId = entity.Id;

            button.Clicked += () =>
            {
                _selectedEntityId = entityId;
                Refresh();
            };

            _content.AddChild(button);
        }

        _content.AddChild(new UiLabel("Selected Entity")
        {
            FontSize = 16.0f,
            Color = UiColor.White
        });

        var selectedEntity = asset.Entities.FirstOrDefault(
            entity => entity.Id == _selectedEntityId);

        if (selectedEntity is null)
        {
            _content.AddChild(new UiLabel("No entity selected")
            {
                FontSize = 13.0f,
                Color = new UiColor(170, 170, 170, 255)
            });

            return;
        }

        _content.AddChild(new UiLabel(
            $"Entity {selectedEntity.Id.Value}")
        {
            FontSize = 14.0f,
            Color = new UiColor(210, 210, 210, 255)
        });

        _content.AddChild(new UiLabel(
            $"Components: {selectedEntity.Components.Count}")
        {
            FontSize = 13.0f,
            Color = new UiColor(170, 170, 170, 255)
        });

        var existingComponentTypes = selectedEntity.Components
    .Select(component => component.ComponentType)
    .ToHashSet();

        foreach (var component in selectedEntity.Components)
        {
            AddComponentSection(
                selectedEntity.Id,
                component,
                existingComponentTypes);
        }

        AddAddComponentSection(
            selectedEntity,
            existingComponentTypes);
        AddEntityActions(selectedEntity);
        if (selectedEntity.Components.Count == 0)
        {
            _content.AddChild(new UiLabel("No components")
            {
                FontSize = 13.0f,
                Color = new UiColor(140, 140, 140, 255)
            });
        }
    }

    private void AddComponentSection(
        PrefabEntityId entityId,
        PrefabComponent component,
        IReadOnlySet<Type> existingComponentTypes)
    {
        var componentType = component.ComponentType;

        var properties = componentType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property =>
                property.GetMethod is { IsPublic: true } &&
                property.GetIndexParameters().Length == 0)
            .OrderBy(property =>
                property.GetCustomAttribute<EditorPropertyAttribute>()?.Order ?? 0)
            .ThenBy(property => property.Name, StringComparer.Ordinal)
            .ToArray();

        var section = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 6.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        foreach (var propertyInfo in properties)
        {
            var metadata =
                propertyInfo.GetCustomAttribute<EditorPropertyAttribute>();

            Action<object?>? setter = null;

            if (propertyInfo.SetMethod is { IsPublic: true })
            {
                setter = value =>
                {
                    // Получаем актуальную версию компонента:
                    // предыдущие свойства могли быть уже изменены.
                    var boxedComponent = Document
                        .GetComponent(entityId, componentType)
                        .BoxedValue;

                    propertyInfo.SetValue(boxedComponent, value);

                    Document.SetComponentValue(
                        entityId,
                        componentType,
                        boxedComponent);

                    Refresh();
                };
            }

            var editorProperty = new EditorProperty(
                propertyInfo.Name,
                propertyInfo.PropertyType,
                () =>
                {
                    var currentComponent = Document.GetComponent(
                        entityId,
                        componentType);

                    return propertyInfo.GetValue(
                        currentComponent.BoxedValue);
                },
                setter,
                metadata?.DisplayName,
                metadata?.Order ?? 0);

            section.AddChild(
                PrefabInspectorPropertyEditorFactory.Create(
                    editorProperty));
        }

        if (properties.Length == 0)
        {
            section.AddChild(new UiLabel(
                component.BoxedValue.ToString() ?? componentType.Name)
            {
                FontSize = 12.0f,
                Color = new UiColor(190, 190, 190, 255)
            });
        }

        var expander = new UiExpander(componentType.Name)
        {
            IsExpanded = true,
            ContentPadding = new UiThickness(8, 4, 4, 4),
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        var dependents = _componentDependencies.GetDependents(
            componentType,
            existingComponentTypes);

        if (componentType != typeof(WorldPositionComponent) &&
            dependents.Count == 0)
        {
            var removeButton = new UiButton("Remove Component")
            {
                Width = 140.0f,
                Height = 28.0f
            };

            removeButton.Clicked += () =>
            {
                Document.RemoveComponent(entityId, componentType);
                Refresh();
            };

            section.AddChild(removeButton);
        }
        else if (dependents.Count > 0)
        {
            section.AddChild(new UiLabel(
                "Required by: " +
                string.Join(", ", dependents.Select(type => type.Name)))
            {
                FontSize = 11.0f,
                Color = new UiColor(175, 165, 140, 255)
            });
        }

        expander.SetContent(section);
        _content.AddChild(expander);
    }

    private void AddAddComponentSection(
    PrefabEntity entity,
    IReadOnlySet<Type> existingComponentTypes)
    {
        var candidates = _componentTypes.Types
            .Where(type =>
                type != typeof(WorldPositionComponent) &&
                !existingComponentTypes.Contains(type))
            .OrderBy(
                type => type.FullName ?? type.Name,
                StringComparer.Ordinal)
            .ToArray();

        var addable = candidates
            .Where(type =>
                _componentDependencies.CanAdd(
                    type,
                    existingComponentTypes))
            .ToArray();

        var blocked = candidates
            .Select(type => new
            {
                Type = type,
                Missing = _componentDependencies.GetMissingDependencies(
                    type,
                    existingComponentTypes)
            })
            .Where(item => item.Missing.Count > 0)
            .ToArray();

        var content = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 6.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        var errorLabel = new UiLabel(string.Empty)
        {
            FontSize = 11.0f,
            Color = new UiColor(230, 130, 130, 255)
        };

        if (addable.Length > 0)
        {
            var duplicateNames = addable
                .GroupBy(type => type.Name, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.Ordinal);

            var options = new Dictionary<string, Type>(
                StringComparer.Ordinal);

            var dropdown = new UiDropdown();

            foreach (var type in addable)
            {
                var option = duplicateNames.Contains(type.Name)
                    ? type.FullName ?? type.Name
                    : type.Name;

                options.Add(option, type);
                dropdown.AddOption(option);
            }

            dropdown.SelectionChanged += (_, selectedName) =>
            {
                if (!options.TryGetValue(selectedName, out var componentType))
                {
                    return;
                }

                try
                {
                    var defaultValue =
                        _componentTypes.CreateDefaultValue(componentType);

                    Document.AddComponent(
                        entity.Id,
                        PrefabComponent.Create(componentType, defaultValue));

                    Refresh();
                }
                catch (Exception exception)
                {
                    errorLabel.Text = exception.Message;
                }
            };

            content.AddChild(dropdown);
        }
        else
        {
            content.AddChild(new UiLabel("No components available to add.")
            {
                FontSize = 12.0f,
                Color = new UiColor(150, 150, 150, 255)
            });
        }

        foreach (var item in blocked)
        {
            content.AddChild(new UiLabel(
                $"{item.Type.Name} requires: " +
                string.Join(", ", item.Missing.Select(type => type.Name)))
            {
                FontSize = 11.0f,
                Color = new UiColor(175, 165, 140, 255)
            });
        }

        content.AddChild(errorLabel);

        var expander = new UiExpander("Add Component")
        {
            IsExpanded = false,
            ContentPadding = new UiThickness(8, 4, 4, 4),
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        expander.SetContent(content);
        _content.AddChild(expander);
    }

    private void AddEntityActions(PrefabEntity selectedEntity)
    {
        var actions = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 6.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        var errorLabel = new UiLabel(string.Empty)
        {
            FontSize = 11.0f,
            Color = new UiColor(230, 130, 130, 255)
        };

        var addChildButton = new UiButton("Add Child Entity")
        {
            Width = 140.0f,
            Height = 30.0f
        };

        addChildButton.Clicked += () =>
        {
            try
            {
                _selectedEntityId =
                    Document.CreateChildEntity(selectedEntity.Id);

                Refresh();
            }
            catch (Exception exception)
            {
                errorLabel.Text = exception.Message;
            }
        };

        actions.AddChild(addChildButton);

        if (selectedEntity.Id != Document.Asset.RootEntity)
        {
            var deleteButton = new UiButton("Delete Entity + Children")
            {
                Width = 180.0f,
                Height = 30.0f
            };

            deleteButton.Clicked += () =>
            {
                try
                {
                    var parentComponent = selectedEntity.Components
                        .FirstOrDefault(component =>
                            component.ComponentType == typeof(
                                Engine.ECS.Components.TransformParent2D));

                    var parent = parentComponent?.BoxedValue
                        is Engine.ECS.Components.TransformParent2D transformParent
                            ? transformParent.Parent
                            : Engine.ECS.Entities.EntityId.Invalid;

                    Document.DeleteEntity(selectedEntity.Id);

                    _selectedEntityId =
                        parent.IsValid && parent.Generation == 0
                            ? new PrefabEntityId(parent.Index)
                            : Document.Asset.RootEntity;

                    Refresh();
                }
                catch (Exception exception)
                {
                    errorLabel.Text = exception.Message;
                }
            };

            actions.AddChild(deleteButton);
        }
        else
        {
            actions.AddChild(new UiLabel("Root cannot be deleted")
            {
                FontSize = 11.0f,
                Color = new UiColor(150, 150, 150, 255)
            });
        }

        _content.AddChild(actions);
        _content.AddChild(errorLabel);
    }
}