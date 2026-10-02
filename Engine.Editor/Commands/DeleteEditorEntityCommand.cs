using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Editor.Documents;
using Engine.Editor.Entities;
using Engine.Editor.Inspection;
using Engine.Worlds.Spatial;

namespace Engine.Editor.Commands;

public sealed class DeleteEditorEntityCommand : IEditorCommand
{
    private readonly EditorDocument _document;
    private readonly EditorEntityReference _reference;
    private List<EntityId>? _children;
    private WorldPosition _position;
    private bool _hadSpatialPosition;

    private List<(Type Type, object Value)>? _components;
    private bool _captured;
    private bool _wasSelected;
    public DeleteEditorEntityCommand(
        EditorDocument document,
        EntityId entity)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!document.World.EcsWorld.Exists(entity))
        {
            throw new InvalidOperationException(
                $"Entity {entity.Index} does not exist.");
        }

        _document = document;

        _reference =
            document.GetEntityReference(entity);
    }

    public EditorEntityReference Reference =>
        _reference;

    public EntityId Entity =>
        _reference.Entity;

    public void Execute()
    {
        if (!_captured)
        {
            Capture();
            _captured = true;
        }

        if (!_reference.IsAlive)
        {
            throw new InvalidOperationException(
                "Editor entity reference is not alive.");
        }

        DetachChildren();

        if (_hadSpatialPosition)
        {
            _document.World.SpatialEntities.DestroyEntity(
                _reference.Entity);
        }
        else
        {
            _document.World.EcsWorld.DestroyEntity(
                _reference.Entity);
        }

        _document.InvalidateEntityReference(
            _reference);
    }

    private void DetachChildren()
    {
        if (_children is null)
        {
            return;
        }

        foreach (var child in _children)
        {
            if (!_document.World.EcsWorld.Exists(child) ||
                !_document.World.EcsWorld.Has<TransformParent2D>(child))
            {
                continue;
            }

            if (_document.World.EcsWorld.Get<TransformParent2D>(child).Parent !=
                _reference.Entity)
            {
                continue;
            }

            _document.World.EcsWorld.Remove<TransformParent2D>(child);
        }
    }

    public void Undo()
    {
        if (!_captured)
        {
            throw new InvalidOperationException(
                "Entity state has not been captured.");
        }

        var entity =
            _hadSpatialPosition
                ? _document.World.SpatialEntities.CreateEntity(
                    _position)
                : _document.World.EcsWorld.CreateEntity();

        _document.RemapEntityReference(
            _reference,
            entity);

        if (_components is null)
        {
            return;
        }

        foreach (var component in _components)
        {
            EcsComponentAccessor.Add(
                _document.World,
                entity,
                component.Type,
                component.Value);
        }

        if (_children is null)
        {
            return;
        }

        foreach (var child in _children)
        {
            if (!_document.World.EcsWorld.Exists(child))
            {
                continue;
            }

            if (!_document.World.EcsWorld.Has<TransformParent2D>(child))
            {
                _document.World.EcsWorld.Add(
                    child,
                    new TransformParent2D(entity));

                continue;
            }

            ref var parent =
                ref _document.World.EcsWorld.Get<TransformParent2D>(
                    child);

            parent.Parent = entity;
        }
        if (_wasSelected)
        {
            _document.EntitySelection.Set(
                entity);
        }
    }

    private void Capture()
    {
        _wasSelected =
    _document.EntitySelection.Contains(
        _reference.Entity);

        _hadSpatialPosition =
            _document.World.SpatialEntities.Contains(
                _reference.Entity);

        if (_hadSpatialPosition)
        {
            _position =
                _document.World.SpatialEntities.GetPosition(
                    _reference.Entity);
        }

        _components =
            _document.World.EcsWorld.Inspector
                .GetComponentTypes(
                    _reference.Entity)
                .Where(
                    static type =>
                        type !=
                        typeof(WorldPositionComponent))
                .Select(
                    type =>
                    {
                        if (!_document.World.EcsWorld.Inspector
                                .TryGetComponent(
                                    _reference.Entity,
                                    type,
                                    out var value) ||
                            value is null)
                        {
                            throw new InvalidOperationException(
                                $"Failed to capture component '{type.Name}'.");
                        }

                        return (type, value);
                    })
                .ToList();

        _children =
    _document.World.EcsWorld.Inspector
        .GetEntities()
        .Where(
            child =>
                child != _reference.Entity &&
                _document.World.EcsWorld.Has<TransformParent2D>(
                    child) &&
                _document.World.EcsWorld.Get<TransformParent2D>(
                    child).Parent == _reference.Entity)
        .ToList();
    }
}