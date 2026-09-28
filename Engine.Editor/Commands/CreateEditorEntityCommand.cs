using Engine.ECS.Entities;
using Engine.Editor.Documents;
using Engine.Editor.Entities;
using Engine.Worlds.Spatial;

namespace Engine.Editor.Commands;

public sealed class CreateEditorEntityCommand : IEditorCommand
{
    private readonly EditorDocument _document;

    public CreateEditorEntityCommand(
        EditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _document = document;
    }

    public EditorEntityReference? Reference { get; private set; }

    public EntityId Entity =>
        Reference?.Entity ??
        EntityId.Invalid;

    public void Execute()
    {
        var entity =
            _document.World.SpatialEntities.CreateEntity(
                new WorldPosition(0, 0));

        if (Reference is null)
        {
            Reference =
                _document.GetEntityReference(entity);
        }
        else
        {
            _document.RemapEntityReference(
                Reference,
                entity);
        }
    }

    public void Undo()
    {
        if (Reference is null ||
            !Reference.IsAlive)
        {
            throw new InvalidOperationException(
                "Entity has not been created.");
        }

        _document.World.SpatialEntities.DestroyEntity(
            Reference.Entity);

        _document.InvalidateEntityReference(
            Reference);
    }
}