using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Editor.Commands;
using Engine.Editor.Documents;
using Engine.Editor.Hierarchy;
using Engine.Graphics.Commands;
using Engine.Input;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;
using Engine.Core.Math;

namespace Engine.Editor.UI.Workspace;

public sealed class HierarchyPanelView :
    UiPanel
{
    private readonly UiStackPanel _toolbar;
    private readonly UiScrollView _scrollView;
    private readonly UiStackPanel _items;

    private readonly UiContextMenu _contextMenu;
    private readonly UiFocusManager? _focus;

    private EntityId? _renamingEntity;
    private UiTextBox? _renameTextBox;

    public EditorHierarchyActionRegistry Actions { get; }

    public HierarchyPanelView(
        EditorContext editor,
        UiOverlayLayer overlays,
        UiFocusManager? focus = null)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(overlays);

        Editor = editor;
        _focus = focus;

        Actions = new EditorHierarchyActionRegistry();
        _contextMenu = new UiContextMenu(overlays);

        RegisterDefaultActions();

        Background = new UiColor(28, 28, 28, 255);
        Padding = new UiThickness(6.0f);

        _toolbar = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 4.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        _items = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 2.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        _scrollView = new UiScrollView
        {
            VerticalScrolling = true,
            HorizontalScrolling = false,
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true,
            ScrollSpeed = 32.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        _scrollView.SetContent(_items);

        AddChild(_toolbar);
        AddChild(_scrollView);
    }

    public EditorContext Editor { get; }

    protected override void ArrangeCore(
    UiRect finalRect)
    {
        var content =
            finalRect.Deflate(Padding);

        var toolbarHeight =
            _toolbar.DesiredSize.Y;

        _toolbar.Arrange(
            new UiRect(
                content.X,
                content.Y,
                content.Width,
                toolbarHeight));

        const float spacing = 6.0f;

        var scrollTop =
            content.Y +
            toolbarHeight +
            spacing;

        _scrollView.Arrange(
            new UiRect(
                content.X,
                scrollTop,
                content.Width,
                MathF.Max(
                    0.0f,
                    content.Bottom - scrollTop)));
    }

    public void Refresh()
    {
        if (_contextMenu.IsOpen)
        {
            _contextMenu.Close();
        }

        _toolbar.ClearChildren();
        _items.ClearChildren();

        _renameTextBox = null;

        var document =
            Editor.ActiveDocument;

        if (document is null)
        {
            _renamingEntity = null;

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

        if (document.EntitySelection.Count == 1)
        {
            var selected =
                document.EntitySelection.Items.First();

            if (!document.World.EcsWorld.Exists(
                    selected))
            {
                document.EntitySelection.Clear();
            }
        }

        if (_renamingEntity is EntityId renameRequest &&
    (document.EntitySelection.Count != 1 ||
     !document.EntitySelection.Contains(renameRequest) ||
     !document.World.EcsWorld.Exists(renameRequest)))
        {
            _renamingEntity = null;
        }

        var createButton = new UiButton("Create")
        {
            Width = 90.0f,
            Height = 30.0f
        };

        createButton.Clicked += () =>
        {
            OpenActionMenu(
                EditorHierarchyActionSurface.CreateMenu,
                EditorHierarchyActionInvocation.CreateMenu,
                document,
                null,
                new Vector2(
                    createButton.Bounds.X,
                    createButton.Bounds.Bottom));
        };

        var actions = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 4.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        actions.AddChild(createButton);
        _toolbar.AddChild(actions);

        if (document.EntitySelection.Count == 1)
        {
            AddParentControls(
                document,
                document.EntitySelection.Items.First());
        }

        if (_renamingEntity is EntityId renamingEntity &&
            document.EntitySelection.Count == 1 &&
            document.EntitySelection.Contains(renamingEntity) &&
            document.World.EcsWorld.Exists(renamingEntity))
        {
            var renameRow = new UiStackPanel
            {
                Orientation = UiOrientation.Horizontal,
                Spacing = 4.0f,
                HorizontalAlignment = UiHorizontalAlignment.Stretch,
                VerticalAlignment = UiVerticalAlignment.Top
            };

            var nameBox = new UiTextBox
            {
                Width = 124.0f,
                Height = 30.0f,
                FontSize = 12.0f,
                MaxLength = 128,
                Placeholder = "Entity name"
            };

            nameBox.SetText(
                GetEntityDisplayName(
                    document,
                    renamingEntity));

            _renameTextBox = nameBox;

            var renameButton = new UiButton("Rename")
            {
                Width = 70.0f,
                Height = 30.0f,
                FontSize = 12.0f
            };

            var cancelButton = new UiButton("Cancel")
            {
                Width = 60.0f,
                Height = 30.0f,
                FontSize = 12.0f
            };

            void CancelRename()
            {
                _renamingEntity = null;
                _renameTextBox = null;
                Refresh();
            }

            void CommitRename()
            {
                var newName = nameBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(newName))
                {
                    return;
                }

                if (!document.World.EcsWorld.Exists(renamingEntity))
                {
                    CancelRename();
                    return;
                }

                var currentName = GetEntityDisplayName(
                    document,
                    renamingEntity);

                if (string.Equals(
                        currentName,
                        newName,
                        StringComparison.Ordinal))
                {
                    CancelRename();
                    return;
                }

                var reference =
                    document.GetEntityReference(renamingEntity);

                document.Execute(
                    new RenameEditorEntityCommand(
                        document.World,
                        reference,
                        newName));

                CancelRename();
            }

            renameButton.Clicked += CommitRename;
            cancelButton.Clicked += CancelRename;

            nameBox.Submitted += _ => CommitRename();

            renameRow.AddChild(nameBox);
            renameRow.AddChild(renameButton);
            renameRow.AddChild(cancelButton);

            _toolbar.AddChild(renameRow);
        }

        var source =
            new EcsHierarchySource(
                document.World);

        var nodes =
            source.GetNodes();

        if (nodes.Count == 0)
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

            return;
        }

        var nodeByEntity =
            nodes
                .OfType<EditorHierarchyNode>()
                .Where(
                    static node =>
                        node.Id is EntityId)
                .ToDictionary(
                    static node =>
                        (EntityId)node.Id);

        var children =
            new Dictionary<
                EntityId,
                List<EditorHierarchyNode>>();

        foreach (var node in nodes)
        {
            if (node.Id is not EntityId entity)
            {
                continue;
            }

            if (node.ParentId is not EntityId parent ||
                !nodeByEntity.ContainsKey(parent))
            {
                continue;
            }

            if (!children.TryGetValue(
                    parent,
                    out var list))
            {
                list =
                    new List<EditorHierarchyNode>();

                children.Add(
                    parent,
                    list);
            }

            list.Add(
                node);
        }

        var rendered =
            new HashSet<EntityId>();

        foreach (var node in nodes)
        {
            if (node.Id is not EntityId entity)
            {
                continue;
            }

            var hasValidParent =
                node.ParentId is EntityId parent &&
                nodeByEntity.ContainsKey(parent);

            if (hasValidParent)
            {
                continue;
            }

            AddHierarchyNode(
                document,
                node,
                0,
                children,
                rendered);
        }

        // Fallback for malformed hierarchies, including cycles.
        foreach (var node in nodes)
        {
            if (node.Id is not EntityId entity)
            {
                continue;
            }

            if (rendered.Contains(
                    entity))
            {
                continue;
            }

            AddHierarchyNode(
                document,
                node,
                0,
                children,
                rendered);
        }
    }

    private void RegisterDefaultActions()
    {
        Actions.Register(
            new EditorHierarchyActionDefinition(
                id: "hierarchy.entity.create",
                label: "Create Entity",
                surfaces: EditorHierarchyActionSurface.CreateMenu,
                canExecute: static context =>
                    context.Document is not null &&
                    ReferenceEquals(
                        context.Editor.ActiveDocument,
                        context.Document),
                execute: CreateEntity,
                order: 10));

        Actions.Register(
            new EditorHierarchyActionDefinition(
                id: "hierarchy.entity.rename",
                label: "Rename",
                surfaces: EditorHierarchyActionSurface.EntityContextMenu,
                canExecute: CanExecuteEntityAction,
                execute: BeginRename,
                shortcut: new EditorHierarchyShortcut(
                    new InputBinding("Keyboard.F2"),
                    "F2"),
                section: "Entity",
                order: 10));

        Actions.Register(
            new EditorHierarchyActionDefinition(
                id: "hierarchy.entity.delete",
                label: "Delete",
                surfaces: EditorHierarchyActionSurface.EntityContextMenu,
                canExecute: CanExecuteEntityAction,
                execute: DeleteEntity,
                shortcut: new EditorHierarchyShortcut(
                    new InputBinding("Keyboard.Delete"),
                    "Delete"),
                section: "Entity",
                order: 90));
    }

    private static bool CanExecuteEntityAction(
        EditorHierarchyActionContext context)
    {
        return context.Document is { } document &&
               ReferenceEquals(
                   context.Editor.ActiveDocument,
                   document) &&
               context.TargetEntity is EntityId entity &&
               document.World.EcsWorld.Exists(entity);
    }

    private void CreateEntity(
        EditorHierarchyActionContext context)
    {
        if (context.Document is not { } document)
        {
            return;
        }

        var command = new CreateEditorEntityCommand(document);

        document.Execute(command);

        if (command.Reference is not null)
        {
            document.EntitySelection.Set(command.Entity);
        }

        _renamingEntity = null;
        Refresh();
    }

    private void BeginRename(
        EditorHierarchyActionContext context)
    {
        if (context.Document is not { } document ||
            context.TargetEntity is not EntityId entity ||
            !document.World.EcsWorld.Exists(entity))
        {
            return;
        }

        _renamingEntity = entity;

        Refresh();

        if (_renameTextBox is not null)
        {
            _focus?.SetFocus(_renameTextBox);
        }
    }

    private void DeleteEntity(
        EditorHierarchyActionContext context)
    {
        if (context.Document is not { } document ||
            context.TargetEntity is not EntityId entity ||
            !document.World.EcsWorld.Exists(entity))
        {
            return;
        }

        document.Execute(
            new DeleteEditorEntityCommand(
                document,
                entity));

        document.EntitySelection.Clear();
        _renamingEntity = null;

        Refresh();
    }

    private void OpenEntityContextMenu(
        EditorDocument document,
        EntityId entity,
        Vector2 position)
    {
        if (!document.World.EcsWorld.Exists(entity))
        {
            return;
        }

        // ПКМ по другой сущности сначала меняет выбор.
        document.EntitySelection.Set(entity);
        _renamingEntity = null;

        Refresh();

        OpenActionMenu(
            EditorHierarchyActionSurface.EntityContextMenu,
            EditorHierarchyActionInvocation.EntityContextMenu,
            document,
            entity,
            position);
    }

    private void OpenActionMenu(
        EditorHierarchyActionSurface surface,
        EditorHierarchyActionInvocation invocation,
        EditorDocument? document,
        EntityId? targetEntity,
        Vector2 position)
    {
        _contextMenu.ClearItems();

        var context = new EditorHierarchyActionContext(
            Editor,
            document,
            targetEntity,
            invocation);

        foreach (var action in Actions.GetForSurface(surface))
        {
            var actionId = action.Id;

            _contextMenu.AddItem(
                action.Label,
                () =>
                {
                    Actions.Execute(actionId, context);
                },
                () => Actions.CanExecute(actionId, context));
        }

        _contextMenu.Open(position);
    }

    private static string GetEntityDisplayName(
        EditorDocument document,
        EntityId entity)
    {
        var world = document.World.EcsWorld;

        if (world.Has<EntityName>(entity))
        {
            var entityName = world.Get<EntityName>(entity);

            if (entityName.IsValid)
            {
                return entityName.Value;
            }
        }

        return $"Entity {entity.Index}";
    }

    private void AddParentControls(
    Engine.Editor.Documents.EditorDocument document,
    EntityId entity)
    {
        var world = document.World.EcsWorld;

        if (!world.Exists(entity) ||
            !world.Has<WorldTransform2D>(entity))
        {
            return;
        }

        var candidates = world.Inspector
            .GetEntities()
            .Where(candidate =>
                candidate != entity &&
                world.Has<WorldTransform2D>(candidate))
            .OrderBy(candidate => candidate.Index)
            .ToArray();

        var hasParent = world.Has<TransformParent2D>(entity);

        var currentParent = hasParent
            ? world.Get<TransformParent2D>(entity).Parent
            : EntityId.Invalid;

        var section = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 4.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        section.AddChild(
            new UiLabel("Transform Hierarchy")
            {
                FontSize = 13.0f,
                Color = new UiColor(170, 170, 170, 255)
            });

        var targetDropdown = new UiDropdown();
        var selectedIndex = 0;

        foreach (var candidate in candidates)
        {
            var name = $"Entity {candidate.Index}";

            if (world.Has<EntityName>(candidate))
            {
                var entityName = world.Get<EntityName>(candidate);

                if (entityName.IsValid)
                {
                    name = entityName.Value;
                }
            }

            targetDropdown.AddOption(
                $"{name} [{candidate.Index}]");
        }

        if (candidates.Length > 0)
        {
            var currentIndex = Array.IndexOf(candidates, currentParent);

            selectedIndex = currentIndex >= 0 ? currentIndex : 0;
            targetDropdown.SetSelectedIndex(selectedIndex);

            targetDropdown.SelectionChanged +=
                (index, _) => selectedIndex = index;

            section.AddChild(
                new UiLabel("Parent")
                {
                    FontSize = 12.0f
                });

            section.AddChild(targetDropdown);
        }
        else
        {
            section.AddChild(
                new UiLabel("No other entities available.")
                {
                    FontSize = 11.0f,
                    Color = new UiColor(175, 165, 140, 255)
                });
        }

        var buttons = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 4.0f,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        var setParentButton = new UiButton("Set Parent")
        {
            Width = 100.0f,
            Height = 30.0f,
            Enabled = candidates.Length > 0
        };

        var clearParentButton = new UiButton("Clear Parent")
        {
            Width = 100.0f,
            Height = 30.0f,
            Enabled = hasParent
        };

        var errorLabel = new UiLabel(string.Empty)
        {
            FontSize = 11.0f,
            Color = new UiColor(230, 130, 130, 255)
        };

        setParentButton.Clicked += () =>
        {
            if (selectedIndex < 0 || selectedIndex >= candidates.Length)
            {
                return;
            }

            try
            {
                document.Execute(
                    new SetTransformParent2DCommand(
                        document.World,
                        document.GetEntityReference(entity),
                        candidates[selectedIndex]));

                Refresh();
            }
            catch (Exception exception) when (
                exception is ArgumentException or
                    InvalidOperationException or
                    OverflowException)
            {
                errorLabel.Text = exception.Message;
            }
        };

        clearParentButton.Clicked += () =>
        {
            try
            {
                document.Execute(
                    new SetTransformParent2DCommand(
                        document.World,
                        document.GetEntityReference(entity),
                        null));

                Refresh();
            }
            catch (Exception exception) when (
                exception is ArgumentException or
                    InvalidOperationException or
                    OverflowException)
            {
                errorLabel.Text = exception.Message;
            }
        };

        buttons.AddChild(setParentButton);
        buttons.AddChild(clearParentButton);

        section.AddChild(buttons);
        section.AddChild(errorLabel);

        _toolbar.AddChild(section);
    }

    private void AddHierarchyNode(
        Editor.Documents.EditorDocument document,
        EditorHierarchyNode node,
        int depth,
        IReadOnlyDictionary<
            EntityId,
            List<EditorHierarchyNode>> children,
        HashSet<EntityId> rendered)
    {
        if (node.Id is not EntityId entity)
        {
            return;
        }

        if (!rendered.Add(
                entity))
        {
            return;
        }

        var selected =
            document.EntitySelection.Contains(
                entity);

        var prefix =
            selected
                ? "> "
                : "  ";

        var button =
            new UiButton(
                prefix + node.Name)
            {
                Height = 30.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                Margin =
                    new UiThickness(
                        depth * 16.0f,
                        0.0f,
                        0.0f,
                        0.0f)
            };

        button.Clicked +=
            () =>
            {
                if (!document.World.EcsWorld.Exists(
                        entity))
                {
                    document.EntitySelection.Clear();
                    return;
                }

                document.EntitySelection.Set(
                    entity);
            };

        button.RightClicked += pointer =>
    OpenEntityContextMenu(
        document,
        entity,
        pointer.Position);

        _items.AddChild(
            button);

        if (!children.TryGetValue(
                entity,
                out var childNodes))
        {
            return;
        }

        foreach (var child in childNodes)
        {
            AddHierarchyNode(
                document,
                child,
                depth + 1,
                children,
                rendered);
        }
    }
}