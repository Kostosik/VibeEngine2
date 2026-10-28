using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Editor.Assets;
using Engine.Editor.Commands;
using Engine.Editor.Inspection;
using Engine.Editor.UI.Dialogs;
using Engine.Editor.UI.Inspection;
using Engine.Graphics.Commands;
using Engine.Graphics.Tilemaps;
using Engine.Graphics2D.Sprites;
using Engine.Particles;
using Engine.Physics.Components;
using Engine.Physics.Joints;
using Engine.Scripting;
using Engine.UI.Controls;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class InspectorPanelView : UiPanel
{
    private readonly UiScrollView _scrollView;
    private readonly UiStackPanel _content;
    private readonly EditorAssetPickerDialog? _assetPickerDialog;
    private readonly InspectorPropertyEditorRegistry _propertyEditors;
    private readonly Func<string?>? _getSelectedParticleEffectPath;
    private readonly Func<string?>? _getSelectedTexturePath;
    private readonly Dictionary<Type, bool> _componentExpansionStates = new();
    private bool _addComponentExpanded;
    private bool _distanceJointExpanded;
    private bool _scriptsExpanded = true;
    public InspectorPanelView(
        EditorContext editor,
        InspectorPropertyEditorRegistry? propertyEditors = null,
        Func<string?>? getSelectedTexturePath = null, Func<string?>? getSelectedParticleEffectPath = null, EditorAssetPickerDialog? assetPickerDialog = null)
    {
        ArgumentNullException.ThrowIfNull(editor);

        Editor = editor;
        _assetPickerDialog = assetPickerDialog;
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
        AddScriptsSection(
    document,
    entity);
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

        var addComponentContent = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 6.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        var addComponent =
            new UiDropdown();

        foreach (var componentType in addableComponentTypes)
        {
            if (componentType == typeof(ScriptAssignmentsComponent))
            {
                continue;
            }
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
                _addComponentExpanded = true;
                document.Execute(
                    new AddEditorComponentCommand(
                        document.World,
                        document.GetEntityReference(entity),
                        componentType,
                        Editor.ComponentDependencies,
                        Editor.ComponentTypes));
            };

        addComponentContent.AddChild(addComponent);

        foreach (var blocked in blockedComponentTypes)
        {
            var missingNames =
                string.Join(
                    ", ",
                    blocked.MissingDependencies.Select(
                        static type => type.Name));

            addComponentContent.AddChild(
                new UiLabel(
                    $"{blocked.ComponentType.Name} requires: {missingNames}.")
                {
                    FontSize = 11.0f,
                    HorizontalAlignment = UiHorizontalAlignment.Stretch,
                    Color = new UiColor(175, 165, 140, 255)
                });
        }

        var addComponentExpander = new UiExpander("Add Component")
        {
            IsExpanded = _addComponentExpanded,
            ContentPadding = new UiThickness(8, 4, 4, 4),
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        addComponentExpander.ExpandedChanged += expanded =>
            _addComponentExpanded = expanded;

        addComponentExpander.SetContent(addComponentContent);
        _content.AddChild(addComponentExpander);

        // Отображаем именно существующие компоненты.
        foreach (var componentType in existingComponentTypes)
        {
            AddComponentSection(
                document,
                entity,
                componentType);
        }
    }
    private void AddScriptsSection(
    Engine.Editor.Documents.EditorDocument document,
    EntityId entity)
    {
        var content = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 6.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        var world = document.World.EcsWorld;

        if (world.Has<ScriptAssignmentsComponent>(entity))
        {
            var assignments =
                world.Get<ScriptAssignmentsComponent>(entity);

            foreach (var assignment in assignments.Assignments)
            {
                content.AddChild(
                    new UiLabel(assignment.ScriptId)
                    {
                        FontSize = 12.0f
                    });
            }
        }
        else
        {
            content.AddChild(
                new UiLabel("No scripts assigned")
                {
                    FontSize = 12.0f,
                    Color = new UiColor(150, 150, 150, 255)
                });
        }

        var errorLabel = new UiLabel(string.Empty)
        {
            FontSize = 11.0f,
            Color = new UiColor(230, 130, 130, 255)
        };

        if (_assetPickerDialog is not null &&
            Editor.AssetBrowser is not null)
        {
            var assignButton = new UiButton("Assign Script")
            {
                Width = 130.0f,
                Height = 30.0f
            };

            assignButton.Clicked += () =>
            {
                _assetPickerDialog.Show(
                    "Select Script Definition",
                    IsScriptDefinitionAsset,
                    relativePath =>
                    {
                        try
                        {
                            var rootPath =
                                Editor.AssetBrowser?.RootPath
                                ?? throw new InvalidOperationException(
                                    "Asset Browser is unavailable.");

                            var fullPath = Path.GetFullPath(
                                Path.Combine(
                                    rootPath,
                                    relativePath.Replace(
                                        '/',
                                        Path.DirectorySeparatorChar)));

                            var definition =
                                ReadScriptDefinition(fullPath);

                            document.Execute(
                                new AssignEditorScriptCommand(
                                    document.World,
                                    document.GetEntityReference(entity),
                                    definition));

                            Refresh();
                        }
                        catch (Exception exception)
                        {
                            errorLabel.Text = exception.Message;
                        }
                    });
            };

            content.AddChild(assignButton);
        }
        else
        {
            content.AddChild(
                new UiLabel(
                    "Open a game project to select script assets.")
                {
                    FontSize = 11.0f,
                    Color = new UiColor(150, 150, 150, 255)
                });
        }

        content.AddChild(errorLabel);

        var expander = new UiExpander("Scripts")
        {
            IsExpanded = _scriptsExpanded,
            ContentPadding = new UiThickness(8, 4, 4, 4),
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        expander.ExpandedChanged += expanded =>
            _scriptsExpanded = expanded;

        expander.SetContent(content);

        _content.AddChild(expander);
    }

    private static bool IsScriptDefinitionAsset(
    EditorAssetEntry entry)
    {
        return !entry.IsDirectory &&
               entry.Name.EndsWith(
                   ".script.json",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static ScriptDefinition ReadScriptDefinition(
        string path)
    {
        var json = File.ReadAllText(path);

        using var document = System.Text.Json.JsonDocument.Parse(json);

        var root = document.RootElement;

        if (root.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            throw new InvalidDataException(
                "Script definition must be a JSON object.");
        }

        if (!root.TryGetProperty(
                "formatVersion",
                out var versionElement) ||
            !versionElement.TryGetInt32(out var version))
        {
            throw new InvalidDataException(
                "Script definition has no valid formatVersion.");
        }

        if (!root.TryGetProperty(
                "scriptId",
                out var idElement) ||
            idElement.ValueKind != System.Text.Json.JsonValueKind.String)
        {
            throw new InvalidDataException(
                "Script definition has no valid scriptId.");
        }

        var scriptId = idElement.GetString();

        if (string.IsNullOrWhiteSpace(scriptId))
        {
            throw new InvalidDataException(
                "Script definition has an empty scriptId.");
        }

        var configurationJson = "{}";

        if (root.TryGetProperty(
                "defaultConfiguration",
                out var configurationElement))
        {
            if (configurationElement.ValueKind !=
                System.Text.Json.JsonValueKind.Object)
            {
                throw new InvalidDataException(
                    "defaultConfiguration must be a JSON object.");
            }

            configurationJson =
                System.Text.Json.JsonSerializer.Serialize(
                    configurationElement);
        }

        return new ScriptDefinition(
            version,
            scriptId,
            configurationJson);
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

        var isExpanded =
            !_componentExpansionStates.TryGetValue(
                componentType,
                out var savedState) ||
            savedState;

        var expander = new UiExpander(componentType.Name)
        {
            IsExpanded = isExpanded,
            ContentPadding = new UiThickness(8, 4, 4, 4),
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        expander.ExpandedChanged += expanded =>
            _componentExpansionStates[componentType] = expanded;

        var section = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 6.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        // Кнопка удаления остаётся частью секции.
        // Компоненты, необходимые другим компонентам,
        // удалять отсюда по-прежнему нельзя.
        if (componentType !=
                typeof(Engine.Worlds.Spatial.WorldPositionComponent) &&
            dependentComponentTypes.Count == 0)
        {
            var actions = new UiStackPanel
            {
                Orientation = UiOrientation.Horizontal,
                Spacing = 4.0f,
                HorizontalAlignment = UiHorizontalAlignment.Stretch,
                VerticalAlignment = UiVerticalAlignment.Top
            };

            var remove = new UiButton("Remove")
            {
                Width = 80.0f,
                Height = 28.0f
            };

            remove.Clicked += () =>
            {
                if (!document.World.EcsWorld.Exists(entity))
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

            actions.AddChild(remove);
            section.AddChild(actions);
        }

        if (dependentComponentTypes.Count > 0)
        {
            var dependentNames =
                string.Join(
                    ", ",
                    dependentComponentTypes.Select(
                        static type => type.Name));

            section.AddChild(
                new UiLabel(
                    $"Required by: {dependentNames}.")
                {
                    FontSize = 11.0f,
                    Color = new UiColor(175, 165, 140, 255)
                });
        }

        var showDerivedTransformOnly = false;

        if (componentType == typeof(WorldTransform2D))
        {
            var hasLocalTransform =
                document.World.EcsWorld.Has<LocalTransform2D>(
                    entity);

            section.AddChild(
                new UiLabel(
                    hasLocalTransform
                        ? "Mode: Derived from LocalTransform2D / hierarchy"
                        : "Mode: Direct WorldTransform2D")
                {
                    FontSize = 12.0f,
                    Color = new UiColor(170, 170, 170, 255)
                });

            if (hasLocalTransform)
            {
                var transform =
                    document.World.EcsWorld.Get<WorldTransform2D>(
                        entity);

                AddWorldTransformValue(
                    section,
                    "World Position",
                    transform.Position.ToString());

                AddWorldTransformValue(
                    section,
                    "World Rotation",
                    $"{transform.Rotation} rad");

                AddWorldTransformValue(
                    section,
                    "World Scale",
                    transform.Scale.ToString());

                showDerivedTransformOnly = true;
            }
        }

        if (!showDerivedTransformOnly)
        {
            foreach (var property in
                     document.Inspector.GetProperties(
                         entity,
                         componentType))
            {
                if (componentType == typeof(TilemapComponent) &&
    property.Name == nameof(TilemapComponent.TilemapPath) &&
    _assetPickerDialog is not null)
                {
                    section.AddChild(
                        CreateTilemapPathProperty(
                            document,
                            property));

                    continue;
                }
                if (componentType == typeof(SpriteComponent) &&
                    property.Name == nameof(SpriteComponent.TexturePath))
                {
                    section.AddChild(
                        CreateSpriteTexturePathProperty(
                            document,
                            property));

                    continue;
                }

                if (componentType == typeof(ParticleEffectComponent) &&
                    property.Name == nameof(ParticleEffectComponent.EffectPath))
                {
                    section.AddChild(
                        CreateParticleEffectPathProperty(
                            document,
                            property));

                    continue;
                }

                section.AddChild(
                    _propertyEditors.Create(
                        document,
                        property));
            }
        }

        expander.SetContent(section);
        _content.AddChild(expander);
    }

    private UiStackPanel CreateTilemapPathProperty(
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

        var currentPath = property.GetValue()?.ToString();

        var pickerField = new UiPickerField(
            string.IsNullOrWhiteSpace(currentPath)
                ? null
                : currentPath)
        {
            Width = 150.0f,
            Height = 32.0f,
            FontSize = 12.0f,
            Placeholder = "Select tilemap..."
        };

        pickerField.SelectionRequested += () =>
        {
            if (_assetPickerDialog is null)
            {
                return;
            }

            _assetPickerDialog.Show(
                "Select Tilemap",
                IsTilemapAsset,
                path =>
                {
                    document.Execute(
                        new SetEditorPropertyCommand(
                            property,
                            path));

                    Refresh();
                });
        };

        row.AddChild(pickerField);

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
        container.AddChild(row);

        return container;
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

        var currentPath =
            property.GetValue()?.ToString();

        var pickerField = new UiPickerField(
            string.IsNullOrWhiteSpace(currentPath)
                ? null
                : currentPath)
        {
            Width = 150.0f,
            Height = 32.0f,
            FontSize = 12.0f,
            Placeholder = "Select particle effect..."
        };

        pickerField.SelectionRequested += () =>
        {
            if (_assetPickerDialog is not null)
            {
                _assetPickerDialog.Show(
                    "Select particle effect",
                    IsParticleEffectAsset,
                    path =>
                    {
                        document.Execute(
                            new SetEditorPropertyCommand(
                                property,
                                path));

                        Refresh();
                    });

                return;
            }

            // Совместимость с инспектором без диалогового сервиса.
            AssignSelectedParticleEffectPath(
                document,
                property);
        };

        row.AddChild(pickerField);

        // Сохраняем прежний сценарий для существующих тестов.
        if (_assetPickerDialog is null &&
            _getSelectedParticleEffectPath is not null)
        {
            var assignButton = new UiButton("Use selected")
            {
                Width = 90.0f,
                Height = 32.0f,
                FontSize = 11.0f
            };

            assignButton.Clicked += () =>
                AssignSelectedParticleEffectPath(
                    document,
                    property);

            row.AddChild(assignButton);
        }

        if (_assetPickerDialog is not null ||
            _getSelectedParticleEffectPath is not null)
        {
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

    private void AssignSelectedParticleEffectPath(
        Engine.Editor.Documents.EditorDocument document,
        EditorProperty property)
    {
        var path =
            _getSelectedParticleEffectPath?.Invoke();

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        document.Execute(
            new SetEditorPropertyCommand(
                property,
                path));

        Refresh();
    }

    private static bool IsParticleEffectAsset(
        EditorAssetEntry entry)
    {
        return !entry.IsDirectory &&
               entry.Name.EndsWith(
                   ".particle.json",
                   StringComparison.OrdinalIgnoreCase);
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

        var currentPath =
            property.GetValue()?.ToString();

        var pickerField = new UiPickerField(
            string.IsNullOrWhiteSpace(currentPath)
                ? null
                : currentPath)
        {
            Width = 150.0f,
            Height = 32.0f,
            FontSize = 12.0f,
            Placeholder = "Select texture..."
        };

        pickerField.SelectionRequested += () =>
        {
            if (_assetPickerDialog is not null)
            {
                _assetPickerDialog.Show(
                    "Select texture",
                    IsTextureAsset,
                    path =>
                    {
                        document.Execute(
                            new SetEditorPropertyCommand(
                                property,
                                path));

                        Refresh();
                    });

                return;
            }

            // Fallback для инспектора, созданного без диалогового сервиса.
            AssignSelectedTexturePath(
                document,
                property);
        };

        row.AddChild(pickerField);

        // Сохраняем старый сценарий для тестов и standalone-инспектора,
        // которому не передали модальный выбор ресурсов.
        if (_assetPickerDialog is null &&
            _getSelectedTexturePath is not null)
        {
            var assignButton = new UiButton("Use selected")
            {
                Width = 90.0f,
                Height = 32.0f,
                FontSize = 11.0f
            };

            assignButton.Clicked += () =>
                AssignSelectedTexturePath(
                    document,
                    property);

            row.AddChild(assignButton);
        }

        if (_assetPickerDialog is not null ||
            _getSelectedTexturePath is not null)
        {
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

    private void AssignSelectedTexturePath(
        Engine.Editor.Documents.EditorDocument document,
        EditorProperty property)
    {
        var path = _getSelectedTexturePath?.Invoke();

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        document.Execute(
            new SetEditorPropertyCommand(
                property,
                path));

        Refresh();
    }

    private static bool IsTextureAsset(
        EditorAssetEntry entry)
    {
        if (entry.IsDirectory)
        {
            return false;
        }

        return Path.GetExtension(entry.Name).ToLowerInvariant() is
            ".png" or
            ".jpg" or
            ".jpeg" or
            ".bmp" or
            ".tga";
    }
    private static bool IsTilemapAsset(
    EditorAssetEntry entry)
    {
        return !entry.IsDirectory &&
               entry.Name.EndsWith(
                   ".tilemap.json",
                   StringComparison.OrdinalIgnoreCase);
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

        var expander = new UiExpander("Add Distance Joint 2D")
        {
            IsExpanded = _distanceJointExpanded,
            ContentPadding = new UiThickness(8, 4, 4, 4),
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        expander.ExpandedChanged += expanded =>
            _distanceJointExpanded = expanded;

        var section = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 4.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

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

            expander.SetContent(section);
            _content.AddChild(expander);
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

        expander.SetContent(section);
        _content.AddChild(expander);
    }

    private static void AddWorldTransformValue(
        UiStackPanel target,
        string name,
        string value)
    {
        target.AddChild(
            new UiLabel(
                $"{name}: {value}")
            {
                FontSize = 12.0f,
                HorizontalAlignment = UiHorizontalAlignment.Stretch,
                Color = new UiColor(200, 200, 200, 255)
            });
    }
}