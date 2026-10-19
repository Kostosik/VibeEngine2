using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Editor.Commands;
using Engine.Editor.Hierarchy;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class HierarchyPanelView :
    UiPanel
{
    private readonly UiStackPanel _toolbar;
    private readonly UiScrollView _scrollView;
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

        _toolbar =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical,

                Spacing = 4.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Top
            };

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

        _scrollView =
            new UiScrollView
            {
                VerticalScrolling = true,
                HorizontalScrolling = false,
                ShowHorizontalScrollBar = false,
                ShowVerticalScrollBar = true,
                ScrollSpeed = 32.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Stretch
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
        _toolbar.ClearChildren();
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

        var createButton =
            new UiButton("Create")
            {
                Width = 90.0f,
                Height = 30.0f
            };

        createButton.Clicked +=
            () =>
            {
                var command =
                    new CreateEditorEntityCommand(
                        document);

                document.Execute(
                    command);

                if (command.Reference is not null)
                {
                    document.EntitySelection.Set(
                        command.Entity);
                }
            };

        var deleteButton =
            new UiButton("Delete")
            {
                Width = 90.0f,
                Height = 30.0f,

                Enabled =
                    document.EntitySelection.Count == 1
            };

        deleteButton.Clicked +=
            () =>
            {
                if (document.EntitySelection.Count != 1)
                {
                    return;
                }

                var entity =
                    document.EntitySelection.Items.First();

                if (!document.World.EcsWorld.Exists(
                        entity))
                {
                    document.EntitySelection.Clear();
                    return;
                }

                document.Execute(
                    new DeleteEditorEntityCommand(
                        document,
                        entity));

                document.EntitySelection.Clear();
            };


        var actions =
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

        actions.AddChild(createButton);
        actions.AddChild(deleteButton);

        _toolbar.AddChild(actions);

        if (document.EntitySelection.Count == 1)
        {
            var renameEntity =
                document.EntitySelection.Items.First();

            if (document.World.EcsWorld.Exists(renameEntity))
            {
                var renameRow =
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

                var nameBox =
                    new UiTextBox
                    {
                        Width = 156.0f,
                        Height = 30.0f,
                        FontSize = 12.0f,
                        MaxLength = 128,
                        Placeholder = "New entity name"
                    };

                var renameButton =
                    new UiButton("Rename")
                    {
                        Width = 76.0f,
                        Height = 30.0f,
                        FontSize = 12.0f
                    };

                void CommitRename()
                {
                    var newName =
                        nameBox.Text.Trim();

                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        return;
                    }

                    if (!document.World.EcsWorld.Exists(renameEntity))
                    {
                        Refresh();
                        return;
                    }

                    if (document.World.EcsWorld.Has<EntityName>(
                            renameEntity))
                    {
                        var existingName =
                            document.World.EcsWorld.Get<EntityName>(
                                renameEntity);

                        if (existingName.IsValid &&
                            string.Equals(
                                existingName.Value,
                                newName,
                                StringComparison.Ordinal))
                        {
                            nameBox.SetText(string.Empty);
                            return;
                        }
                    }

                    var reference =
                        document.GetEntityReference(renameEntity);

                    document.Execute(
                        new RenameEditorEntityCommand(
                            document.World,
                            reference,
                            newName));

                    Refresh();
                }

                renameButton.Clicked +=
                    CommitRename;

                nameBox.Submitted +=
                    _ => CommitRename();

                renameRow.AddChild(nameBox);
                renameRow.AddChild(renameButton);

                _toolbar.AddChild(renameRow);
            }
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