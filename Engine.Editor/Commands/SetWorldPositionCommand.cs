using Engine.Editor.Entities;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Editor.Commands;

public sealed class SetWorldPositionCommand : IEditorCommand
{
    private readonly World _world;
    private readonly EditorEntityReference _reference;
    private readonly WorldPosition _oldPosition;
    private readonly WorldPosition _newPosition;

    public SetWorldPositionCommand(
        World world,
        EditorEntityReference reference,
        WorldPosition oldPosition,
        WorldPosition newPosition)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(reference);

        reference.EnsureWorld(
    world);

        if (!reference.IsAlive)
        {
            throw new ArgumentException(
                "Editor entity reference is not alive.",
                nameof(reference));
        }

        _world = world;
        _reference = reference;
        _oldPosition = oldPosition;
        _newPosition = newPosition;
    }

    public void Execute()
    {
        _world.SpatialEntities.SetPosition(
            _reference.Entity,
            _newPosition);
    }

    public void Undo()
    {
        _world.SpatialEntities.SetPosition(
            _reference.Entity,
            _oldPosition);
    }
}