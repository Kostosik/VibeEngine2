using Engine.ECS.Entities;
using Engine.Editor.Commands;
using Engine.Editor.Entities;
using Engine.Editor.Inspection;
using Engine.Editor.Selection;
using Engine.Editor.Viewport;
using Engine.Tooling.Validation;
using Engine.Worlds;

namespace Engine.Editor.Documents;

public sealed class EditorDocument
{
    private readonly Dictionary<EntityId, EditorEntityReference>
    _entityReferences = new();
    public EditorViewport Viewport { get; }
    public EditorInspector Inspector { get; }
    public EditorPropertyProviderRegistry PropertyProviders { get; }
    public EditorDocument(
        World world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        World = world;

        EntitySelection =
            new SelectionSet<EntityId>();

        CommandHistory =
            new EditorCommandHistory();

        Validation =
            new ValidationService();

        PropertyProviders =
            new EditorPropertyProviderRegistry();

        PropertyProviders.Register(
            new ReflectionPropertyProvider());

        Viewport =
    new EditorViewport();

        PropertyProviders.Register(
            new EcsComponentPropertyProvider());

        Inspector =
            new EditorInspector(
                world,
                PropertyProviders);
    }

    public World World { get; }

    public SelectionSet<EntityId> EntitySelection { get; }

    public EditorCommandHistory CommandHistory { get; }

    public ValidationService Validation { get; }

    public bool IsDirty =>
        CommandHistory.IsDirty;

    public EditorEntityReference GetEntityReference(
    EntityId entity)
    {
        if (!World.EcsWorld.Exists(entity))
        {
            throw new InvalidOperationException(
                $"Entity {entity.Index} does not exist.");
        }

        if (_entityReferences.TryGetValue(
                entity,
                out var existing))
        {
            return existing;
        }

        var reference =
            new EditorEntityReference(entity);

        _entityReferences.Add(
            entity,
            reference);

        return reference;
    }

    internal void RemapEntityReference(
        EditorEntityReference reference,
        EntityId newEntity)
    {
        ArgumentNullException.ThrowIfNull(reference);

        if (!World.EcsWorld.Exists(newEntity))
        {
            throw new InvalidOperationException(
                $"Entity {newEntity.Index} does not exist.");
        }

        if (reference.IsAlive)
        {
            _entityReferences.Remove(
                reference.Entity);
        }

        if (_entityReferences.ContainsKey(newEntity))
        {
            throw new InvalidOperationException(
                $"An editor reference already exists for entity {newEntity.Index}.");
        }

        reference.SetEntity(newEntity);

        _entityReferences.Add(
            newEntity,
            reference);
    }

    internal void InvalidateEntityReference(
        EditorEntityReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        if (reference.IsAlive)
        {
            _entityReferences.Remove(
                reference.Entity);
        }

        reference.SetEntity(
            EntityId.Invalid);
    }

    public void Execute(
        IEditorCommand command)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        CommandHistory.Execute(
            command);
    }

    public void Undo()
    {
        CommandHistory.Undo();
    }

    public void Redo()
    {
        CommandHistory.Redo();
    }

    public void MarkSaved()
    {
        CommandHistory.MarkSaved();
    }
}