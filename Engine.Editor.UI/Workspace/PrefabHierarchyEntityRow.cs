using Engine.ECS.Components;
using Engine.ECS.Prefabs;
using Engine.Editor.Commands;
using Engine.Editor.Documents;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

internal sealed class PrefabHierarchyEntityRow :
    UiPanel,
    IUiDropTarget
{
    private readonly PrefabDocument _document;
    private readonly PrefabEntityId _entityId;
    private readonly Action<PrefabEntityId, PrefabEntityId> _onDropped;
    private readonly Action<string>? _onDropRejected;

    public PrefabHierarchyEntityRow(
        PrefabDocument document,
        PrefabEntityId entityId,
        string displayText,
        int depth,
        bool isRoot,
        bool hasChildren,
        bool isExpanded,
        Action toggleExpansion,
        Action selectEntity,
        Action<PrefabEntityId, PrefabEntityId> onDropped,
        Action<string>? onDropRejected = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(toggleExpansion);
        ArgumentNullException.ThrowIfNull(selectEntity);
        ArgumentNullException.ThrowIfNull(onDropped);

        _document = document;
        _entityId = entityId;
        _onDropped = onDropped;
        _onDropRejected = onDropRejected;

        Height = 29.0f;
        HorizontalAlignment = UiHorizontalAlignment.Stretch;
        VerticalAlignment = UiVerticalAlignment.Top;
        Background = UiColor.Transparent;

        Margin = new UiThickness(
            depth * 16.0f,
            0.0f,
            0.0f,
            0.0f);

        var content = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 3.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        var expandButton = new UiButton(
            hasChildren
                ? (isExpanded ? "▼" : "►")
                : " ")
        {
            Width = 24.0f,
            Height = 29.0f,
            FontSize = 11.0f,
            Enabled = hasChildren
        };

        if (hasChildren)
        {
            expandButton.Clicked += toggleExpansion;
        }

        content.AddChild(expandButton);

        var dragHandle = new PrefabHierarchyEntityDragHandle(
            entityId,
            displayText,
            isRoot);

        dragHandle.Button.Clicked += selectEntity;

        content.AddChild(dragHandle);

        AddChild(content);
    }

    public bool CanAcceptDrop(UiDragPayload payload)
    {
        if (payload is not UiDragPayload<uint> entityPayload)
        {
            return false;
        }

        var draggedId = new PrefabEntityId(entityPayload.Value);

        if (!draggedId.IsValid ||
            draggedId == _document.Asset.RootEntity ||
            draggedId == _entityId)
        {
            return false;
        }

        var asset = _document.Asset;

        var dragged = asset.Entities.FirstOrDefault(
            entity => entity.Id == draggedId);

        var target = asset.Entities.FirstOrDefault(
            entity => entity.Id == _entityId);

        if (dragged is null || target is null)
        {
            return false;
        }

        // Both entities must participate in the transform hierarchy.
        if (!HasWorldTransform(dragged) ||
            !HasWorldTransform(target))
        {
            return false;
        }

        if (TryGetParentId(dragged, out var currentParent) &&
            currentParent == _entityId)
        {
            return false;
        }

        return !WouldCreateCycle(
            asset,
            draggedId,
            target);
    }

    public void OnDragEnter(UiDragContext context)
    {
        ShowDropFeedback();
    }

    public void OnDragOver(UiDragContext context)
    {
        ShowDropFeedback();
    }

    public void OnDragLeave(UiDragContext context)
    {
        Background = UiColor.Transparent;
    }

    public bool OnDrop(UiDragContext context)
    {
        if (context.Payload is not UiDragPayload<uint> payload ||
            !CanAcceptDrop(context.Payload))
        {
            return false;
        }

        var draggedId = new PrefabEntityId(payload.Value);

        try
        {
            _document.CommandHistory.Execute(
                new ReparentPrefabEntityCommand(
                    _document,
                    draggedId,
                    _entityId));

            _onDropped(
                draggedId,
                _entityId);

            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException or
                InvalidOperationException or
                OverflowException)
        {
            _onDropRejected?.Invoke(exception.Message);
            return false;
        }
    }

    private void ShowDropFeedback()
    {
        Background = new UiColor(45, 105, 65, 255);
    }

    private static bool HasWorldTransform(
        PrefabEntity entity)
    {
        return entity.Components.Any(
            component =>
                component.ComponentType == typeof(WorldTransform2D));
    }

    private static bool TryGetParentId(
        PrefabEntity entity,
        out PrefabEntityId parentId)
    {
        var component = entity.Components
            .OfType<PrefabComponent<TransformParent2D>>()
            .FirstOrDefault();

        if (component is null)
        {
            parentId = PrefabEntityId.Invalid;
            return false;
        }

        var reference = component.Component.Parent;

        if (!reference.IsValid || reference.Generation != 0)
        {
            parentId = PrefabEntityId.Invalid;
            return true;
        }

        parentId = new PrefabEntityId(reference.Index);
        return true;
    }

    private static bool WouldCreateCycle(
        PrefabAsset asset,
        PrefabEntityId draggedId,
        PrefabEntity target)
    {
        var entities = asset.Entities.ToDictionary(
            entity => entity.Id);

        var visited = new HashSet<PrefabEntityId>();
        var current = target;

        while (true)
        {
            if (current.Id == draggedId)
            {
                return true;
            }

            if (!visited.Add(current.Id))
            {
                return true;
            }

            if (!TryGetParentId(current, out var parentId))
            {
                return false;
            }

            if (!parentId.IsValid ||
                !entities.TryGetValue(parentId, out var parent))
            {
                return true;
            }

            current = parent;
        }
    }
}

internal sealed class PrefabHierarchyEntityDragHandle :
    UiPanel,
    IUiDragSource
{
    private readonly PrefabEntityId _entityId;
    private readonly bool _isRoot;
    private readonly string _normalText;

    public PrefabHierarchyEntityDragHandle(
        PrefabEntityId entityId,
        string displayText,
        bool isRoot)
    {
        _entityId = entityId;
        _isRoot = isRoot;
        _normalText = displayText;

        Height = 29.0f;
        HorizontalAlignment = UiHorizontalAlignment.Stretch;
        VerticalAlignment = UiVerticalAlignment.Top;

        Button = new UiButton(displayText)
        {
            Height = 29.0f,
            FontSize = 13.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        AddChild(Button);
    }

    public UiButton Button { get; }

    public UiDragPayload? CreateDragPayload(
        Engine.Core.Math.Vector2 startPosition)
    {
        if (_isRoot)
        {
            return null;
        }

        return new UiDragPayload<uint>(_entityId.Value);
    }

    public void OnDragStarted(UiDragContext context)
    {
        Button.Text = $"► {_normalText}";
    }

    public void OnDragEnded(
        UiDragContext context,
        UiDragEndReason reason)
    {
        Button.Text = _normalText;
    }
}