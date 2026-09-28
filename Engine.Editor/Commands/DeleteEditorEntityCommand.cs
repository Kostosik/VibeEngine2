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

    private WorldPosition _position;
    private bool _hadSpatialPosition;

    private List<(Type Type, object Value)>? _components;
    private bool _captured;

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
    }

    private void Capture()
    {
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
    }
}