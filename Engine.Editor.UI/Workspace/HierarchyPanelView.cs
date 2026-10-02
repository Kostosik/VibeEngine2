using Engine.ECS.Entities;
using Engine.Editor;
using Engine.Editor.Commands;
using Engine.Editor.Hierarchy;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class HierarchyPanelView :
    UiPanel
{
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

        AddChild(
            _items);
    }

    public EditorContext Editor { get; }

    public void Refresh()
    {
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

        var toolbar =
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

        toolbar.AddChild(
            createButton);

        toolbar.AddChild(
            deleteButton);

        _items.AddChild(
            toolbar);

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

        var children =
            new Dictionary<
                EntityId,
                List<EditorHierarchyNode>>();

        var nodeByEntity =
            nodes
                .OfType<EditorHierarchyNode>()
                .Where(
                    node =>
                        node.Id is EntityId)
                .ToDictionary(
                    node =>
                        (EntityId)node.Id);

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

        foreach (var node in nodes)
        {
            if (node.Id is not EntityId entity)
            {
                continue;
            }

            if (node.ParentId is EntityId parent &&
                nodeByEntity.ContainsKey(parent))
            {
                continue;
            }

            AddHierarchyNode(
                document,
                node,
                0,
                children,
                new HashSet<EntityId>());
        }

        foreach (var node in nodes)
        {
            if (node.Id is not EntityId entity ||
                children.ContainsKey(entity))
            {
                continue;
            }

            if (node.ParentId is EntityId parent &&
                nodeByEntity.ContainsKey(parent))
            {
                AddHierarchyNode(
                    document,
                    node,
                    0,
                    children,
                    new HashSet<EntityId>());
            }
        }
    }

    private void AddHierarchyNode(
        Editor.Documents.EditorDocument document,
        EditorHierarchyNode node,
        int depth,
        IReadOnlyDictionary<
            EntityId,
            List<EditorHierarchyNode>> children,
        HashSet<EntityId> path)
    {
        if (node.Id is not EntityId entity)
        {
            return;
        }

        if (!path.Add(
                entity))
        {
            return;
        }

        var selected =
            document.EntitySelection.Contains(
                entity);

        var prefix =
            selected
                ? "● "
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

        if (children.TryGetValue(
                entity,
                out var childNodes))
        {
            foreach (var child in childNodes)
            {
                AddHierarchyNode(
                    document,
                    child,
                    depth + 1,
                    children,
                    new HashSet<EntityId>(
                        path));
            }
        }

        path.Remove(
            entity);
    }
}