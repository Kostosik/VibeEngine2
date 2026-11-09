using Engine.ECS.Components;
using Engine.ECS.Prefabs;
using Engine.Editor.Assets;
using Engine.Editor.Commands;
using Engine.Editor.Components;
using Engine.Editor.Documents;
using Engine.Editor.Inspection;
using Engine.Editor.UI.Dialogs;
using Engine.Editor.UI.Inspection;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Sprites;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;
using Engine.Worlds.Spatial;
using System.Reflection;

namespace Engine.Editor.UI.Workspace;

public sealed class PrefabDocumentView : UiPanel
{
    private UiLabel? _hierarchyErrorLabel;
    private readonly HashSet<PrefabEntityId> _expandedEntities = new();
    private readonly UiStackPanel _content;
    private PrefabEntityId _selectedEntityId;
    private readonly EditorComponentTypeRegistry _componentTypes;
    private readonly EditorComponentDependencyRegistry _componentDependencies;
    private readonly InspectorPropertyEditorRegistry? _propertyEditors;
    private readonly UiStackPanel _hierarchyContent;
    private readonly PrefabPreviewView _preview;
    private readonly EditorAssetPickerDialog? _assetPickerDialog;
    public PrefabDocumentView(
    PrefabDocument document,
    EditorComponentTypeRegistry componentTypes,
    EditorComponentDependencyRegistry componentDependencies)
    : this(
        document,
        componentTypes,
        componentDependencies,
        null,
        null,
        null)
    {
    }

    public PrefabDocumentView(
        PrefabDocument document,
        EditorComponentTypeRegistry componentTypes,
        EditorComponentDependencyRegistry componentDependencies,
        InspectorPropertyEditorRegistry? propertyEditors)
        : this(
            document,
            componentTypes,
            componentDependencies,
            propertyEditors,
            null,
            null)
    {
    }

    public PrefabDocumentView(
        PrefabDocument document,
        EditorComponentTypeRegistry componentTypes,
        EditorComponentDependencyRegistry componentDependencies,
        InspectorPropertyEditorRegistry? propertyEditors,
        EditorContext? editor,
        ITextureManager? previewTextureManager)
        : this(
            document,
            componentTypes,
            componentDependencies,
            propertyEditors,
            editor,
            previewTextureManager,
            null)
    {
    }

    public PrefabDocumentView(
        PrefabDocument document,
        EditorComponentTypeRegistry componentTypes,
        EditorComponentDependencyRegistry componentDependencies,
        InspectorPropertyEditorRegistry? propertyEditors,
        EditorContext? editor,
        ITextureManager? previewTextureManager,
        EditorAssetPickerDialog? assetPickerDialog)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(componentTypes);
        ArgumentNullException.ThrowIfNull(componentDependencies);

        Document = document;
        _componentTypes = componentTypes;
        _componentDependencies = componentDependencies;
        _propertyEditors = propertyEditors;
        _assetPickerDialog = assetPickerDialog;
        _selectedEntityId = document.Asset.RootEntity;

        Background = new UiColor(30, 30, 30, 255);
        Padding = new UiThickness(8.0f);

        _hierarchyContent = CreateContent();
        _content = CreateContent();

        _preview = new PrefabPreviewView(
            document,
            editor,
            previewTextureManager);

        var hierarchyScroll = CreateScrollView(
            _hierarchyContent);

        var inspectorScroll = CreateScrollView(
            _content);

        var hierarchyHost = new UiPanel
        {
            Width = 235.0f,
            Padding = new UiThickness(6.0f),
            Background = new UiColor(28, 28, 28, 255),
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        hierarchyHost.AddChild(hierarchyScroll);

        var previewHost = new UiPanel
        {
            Padding = new UiThickness(4.0f),
            Background = new UiColor(22, 22, 22, 255),
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        previewHost.AddChild(_preview);

        var inspectorHost = new UiPanel
        {
            Width = 320.0f,
            Padding = new UiThickness(6.0f),
            Background = new UiColor(28, 28, 28, 255),
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        inspectorHost.AddChild(inspectorScroll);

        var workspace = new PrefabWorkspaceLayout(
            hierarchyHost,
            inspectorHost,
            previewHost);

        AddChild(workspace);

        foreach (var entity in document.Asset.Entities)
        {
            _expandedEntities.Add(entity.Id);
        }

        Refresh();
    }

    private static UiStackPanel CreateContent()
    {
        return new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 5.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };
    }

    private static UiScrollView CreateScrollView(
        UiStackPanel content)
    {
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

        scrollView.SetContent(content);

        return scrollView;
    }

    public PrefabDocument Document { get; }

    public void Refresh()
    {
        var asset = Document.Asset;

        _hierarchyContent.ClearChildren();
        _content.ClearChildren();

        _preview.SelectedEntityId = _selectedEntityId;

        _content.AddChild(new UiLabel(asset.Name)
        {
            FontSize = 20.0f,
            Color = UiColor.White
        });

        _hierarchyContent.AddChild(new UiLabel(asset.Name)
        {
            FontSize = 20.0f,
            Color = UiColor.White
        });

        _hierarchyContent.AddChild(new UiLabel(
            $"Entities: {asset.Entities.Count}")
        {
            FontSize = 13.0f,
            Color = new UiColor(170, 170, 170, 255)
        });

        _hierarchyContent.AddChild(new UiLabel(
            $"Root: Entity {asset.RootEntity.Value}")
        {
            FontSize = 13.0f,
            Color = new UiColor(170, 170, 170, 255)
        });

        _hierarchyContent.AddChild(new UiLabel("Hierarchy")
        {
            FontSize = 16.0f,
            Color = UiColor.White
        });

        _hierarchyErrorLabel = new UiLabel(string.Empty)
        {
            FontSize = 11.0f,
            Color = new UiColor(230, 130, 130, 255)
        };

        _content.AddChild(_hierarchyErrorLabel);

        var validEntityIds = asset.Entities
    .Select(entity => entity.Id)
    .ToHashSet();

        _expandedEntities.RemoveWhere(
            id => !validEntityIds.Contains(id));

        var children = BuildChildren(asset);
        var roots = GetHierarchyRoots(asset, children);

        var reachable = new HashSet<PrefabEntityId>();

        foreach (var root in roots)
        {
            MarkReachable(root, children, reachable);
        }

        var rendered = new HashSet<PrefabEntityId>();

        foreach (var root in roots)
        {
            AddHierarchyNode(
                root,
                0,
                children,
                rendered);
        }

        // Не теряем Entity из некорректных или циклических иерархий.
        foreach (var entity in asset.Entities)
        {
            if (reachable.Contains(entity.Id))
            {
                continue;
            }

            AddHierarchyNode(
                entity,
                0,
                children,
                rendered);

            MarkReachable(
                entity,
                children,
                reachable);
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

    private static Dictionary<PrefabEntityId, List<PrefabEntity>>
    BuildChildren(PrefabAsset asset)
    {
        var children = asset.Entities.ToDictionary(
            entity => entity.Id,
            _ => new List<PrefabEntity>());

        foreach (var entity in asset.Entities)
        {
            // Root всегда остаётся корнем prefab.
            if (entity.Id == asset.RootEntity)
            {
                continue;
            }

            if (!TryGetParentId(entity, out var parentId))
            {
                continue;
            }

            if (children.TryGetValue(parentId, out var list))
            {
                // Порядок соответствует порядку Entity в документе.
                list.Add(entity);
            }
        }

        return children;
    }

    private static List<PrefabEntity> GetHierarchyRoots(
        PrefabAsset asset,
        IReadOnlyDictionary<PrefabEntityId, List<PrefabEntity>> children)
    {
        var roots = new List<PrefabEntity>
    {
        asset.Entities.Single(
            entity => entity.Id == asset.RootEntity)
    };

        foreach (var entity in asset.Entities)
        {
            if (entity.Id == asset.RootEntity)
            {
                continue;
            }

            if (!TryGetParentId(entity, out var parentId) ||
                !children.ContainsKey(parentId))
            {
                roots.Add(entity);
            }
        }

        return roots;
    }

    private static bool TryGetParentId(
        PrefabEntity entity,
        out PrefabEntityId parentId)
    {
        var parentComponent = entity.Components
            .OfType<PrefabComponent<TransformParent2D>>()
            .FirstOrDefault();

        if (parentComponent is null)
        {
            parentId = PrefabEntityId.Invalid;
            return false;
        }

        var parent = parentComponent.Component.Parent;

        // В prefab допустимы только локальные ссылки.
        if (!parent.IsValid || parent.Generation != 0)
        {
            parentId = PrefabEntityId.Invalid;
            return false;
        }

        parentId = new PrefabEntityId(parent.Index);
        return true;
    }

    private static void MarkReachable(
        PrefabEntity entity,
        IReadOnlyDictionary<PrefabEntityId, List<PrefabEntity>> children,
        HashSet<PrefabEntityId> reachable)
    {
        if (!reachable.Add(entity.Id))
        {
            return;
        }

        if (!children.TryGetValue(entity.Id, out var childEntities))
        {
            return;
        }

        foreach (var child in childEntities)
        {
            MarkReachable(
                child,
                children,
                reachable);
        }
    }

    private void AddHierarchyNode(
        PrefabEntity entity,
        int depth,
        IReadOnlyDictionary<PrefabEntityId, List<PrefabEntity>> children,
        HashSet<PrefabEntityId> rendered)
    {
        // Защита от циклических ссылок.
        if (!rendered.Add(entity.Id))
        {
            return;
        }

        children.TryGetValue(
            entity.Id,
            out var childEntities);

        var hasChildren = childEntities is { Count: > 0 };

        var isExpanded =
            hasChildren &&
            _expandedEntities.Contains(entity.Id);

        var isRoot =
            entity.Id == Document.Asset.RootEntity;

        var isSelected =
            entity.Id == _selectedEntityId;

        var displayName = GetEntityDisplayName(entity);

        var rootSuffix = isRoot
            ? " [Root]"
            : string.Empty;

        var row = new PrefabHierarchyEntityRow(
            Document,
            entity.Id,
            $"{(isSelected ? "● " : "  ")}{displayName}{rootSuffix}",
            depth,
            isRoot,
            hasChildren,
            isExpanded,

            toggleExpansion: () =>
            {
                if (!_expandedEntities.Remove(entity.Id))
                {
                    _expandedEntities.Add(entity.Id);
                }

                Refresh();
            },

            selectEntity: () =>
            {
                _selectedEntityId = entity.Id;
                Refresh();
            },

            onDropped: (movedId, newParentId) =>
            {
                _expandedEntities.Add(newParentId);
                _selectedEntityId = movedId;

                Refresh();
            },

            onDropRejected: message =>
            {
                if (_hierarchyErrorLabel is not null)
                {
                    _hierarchyErrorLabel.Text = message;
                }
            });
        _hierarchyContent.AddChild(row);

        // Рекурсивно добавляем раскрытых потомков.
        if (!isExpanded || childEntities is null)
        {
            return;
        }

        foreach (var child in childEntities)
        {
            AddHierarchyNode(
                child,
                depth + 1,
                children,
                rendered);
        }
    }

    private static string GetEntityDisplayName(
        PrefabEntity entity)
    {
        var nameComponent = entity.Components
            .OfType<PrefabComponent<EntityName>>()
            .FirstOrDefault();

        if (nameComponent is not null &&
            nameComponent.Component.IsValid)
        {
            return nameComponent.Component.Value;
        }

        return $"Entity {entity.Id.Value}";
    }

    public void ReleaseResources()
    {
        _preview.ReleaseResources();
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

            var isSpriteTexturePath =
                componentType == typeof(SpriteComponent) &&
                propertyInfo.Name == nameof(SpriteComponent.TexturePath);

            var propertyEditor =
                isSpriteTexturePath && _assetPickerDialog is not null
                    ? CreateTexturePathEditor(editorProperty)
                    : _propertyEditors is not null
                        ? _propertyEditors.CreatePrefabEditor(
                            Document,
                            editorProperty)
                        : PrefabInspectorPropertyEditorFactory.Create(
                            editorProperty);

            section.AddChild(propertyEditor);
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

    private UiWidget CreateTexturePathEditor(
    EditorProperty property)
    {
        if (property.IsReadOnly)
        {
            return PrefabInspectorPropertyEditorFactory.Create(property);
        }

        var row = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 6.0f,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        row.AddChild(new UiLabel(property.DisplayName)
        {
            Width = 90.0f,
            FontSize = 12.0f
        });

        var picker = new UiPickerField
        {
            Width = 190.0f,
            Height = 32.0f,
            FontSize = 12.0f,
            Placeholder = "Select texture",
            SelectedText = property.GetValue() as string
        };

        picker.SelectionRequested += () =>
        {
            if (_assetPickerDialog is null)
            {
                return;
            }

            var opened = _assetPickerDialog.Show(
                "Select sprite texture",
                IsTextureAsset,
                path =>
                {
                    property.SetValue(path);
                    picker.SelectedText = path;

                    if (_hierarchyErrorLabel is not null)
                    {
                        _hierarchyErrorLabel.Text = string.Empty;
                    }
                });

            if (!opened && _hierarchyErrorLabel is not null)
            {
                _hierarchyErrorLabel.Text =
                    "Open a game project to select textures.";
            }
        };

        row.AddChild(picker);

        return row;
    }

    private static bool IsTextureAsset(EditorAssetEntry entry)
    {
        if (entry.IsDirectory)
        {
            return false;
        }

        var extension = Path.GetExtension(entry.Name);

        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".tga", StringComparison.OrdinalIgnoreCase);
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
                var childId =
                    Document.CreateChildEntity(selectedEntity.Id);

                // Новый дочерний элемент сразу будет виден в дереве.
                _expandedEntities.Add(selectedEntity.Id);

                _selectedEntityId = childId;

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
            var duplicateButton = new UiButton("Duplicate + Children")
            {
                Width = 150.0f,
                Height = 30.0f
            };

            duplicateButton.Clicked += () =>
            {
                try
                {
                    var command = new DuplicatePrefabEntityCommand(
                        Document,
                        selectedEntity.Id);

                    Document.CommandHistory.Execute(command);

                    // Копия находится у того же родителя.
                    if (TryGetParentId(
                            selectedEntity,
                            out var parentId))
                    {
                        _expandedEntities.Add(parentId);
                    }

                    _selectedEntityId = command.DuplicatedRootId;

                    Refresh();
                }
                catch (Exception exception)
                {
                    errorLabel.Text = exception.Message;
                }
            };

            actions.AddChild(duplicateButton);

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