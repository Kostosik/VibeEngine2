using Engine.ECS.Components;
using Engine.Editor.Entities;
using Engine.Worlds;

namespace Engine.Editor.Commands;

public sealed class SetWorldTransform2DCommand : IEditorCommand
{
    private readonly World _world;
    private readonly EditorEntityReference _reference;
    private readonly WorldTransform2D _oldValue;
    private readonly WorldTransform2D _newValue;

    public SetWorldTransform2DCommand(
        World world,
        EditorEntityReference reference,
        WorldTransform2D oldValue,
        WorldTransform2D newValue)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(reference);

        reference.EnsureWorld(world);

        if (!reference.IsAlive)
        {
            throw new ArgumentException(
                "Editor entity reference is not alive.",
                nameof(reference));
        }

        EnsureCanModifyWorldTransform(
            world,
            reference.Entity);

        _world = world;
        _reference = reference;
        _oldValue = oldValue;
        _newValue = newValue;
    }

    public void Execute()
    {
        SetValue(_newValue);
    }

    public void Undo()
    {
        SetValue(_oldValue);
    }

    private void SetValue(WorldTransform2D value)
    {
        if (!_reference.IsAlive)
        {
            throw new InvalidOperationException(
                "Editor entity reference is no longer alive.");
        }

        EnsureCanModifyWorldTransform(
            _world,
            _reference.Entity);

        _world.EcsWorld.Get<WorldTransform2D>(
            _reference.Entity) = value;
    }

    private static void EnsureCanModifyWorldTransform(
        World world,
        Engine.ECS.Entities.EntityId entity)
    {
        if (!world.EcsWorld.Has<WorldTransform2D>(entity))
        {
            throw new InvalidOperationException(
                "Entity does not have WorldTransform2D.");
        }

        if (world.EcsWorld.Has<LocalTransform2D>(entity))
        {
            throw new InvalidOperationException(
                "WorldTransform2D is derived for entities with LocalTransform2D.");
        }
    }
}