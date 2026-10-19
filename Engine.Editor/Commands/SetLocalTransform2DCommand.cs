using Engine.ECS.Components;
using Engine.Editor.Documents;
using Engine.Editor.Entities;
using Engine.Worlds;

namespace Engine.Editor.Commands;

public sealed class SetLocalTransform2DCommand : IEditorCommand
{
    private readonly World _world;
    private readonly EditorDocument _document;
    private readonly EditorEntityReference _reference;
    private readonly LocalTransform2D _oldValue;
    private readonly LocalTransform2D _newValue;

    public SetLocalTransform2DCommand(
        EditorDocument document,
        EditorEntityReference reference,
        LocalTransform2D oldValue,
        LocalTransform2D newValue)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(reference);

        var world = document.World;

        reference.EnsureWorld(world);

        if (!reference.IsAlive)
        {
            throw new ArgumentException(
                "Editor entity reference is not alive.",
                nameof(reference));
        }

        if (!world.EcsWorld.Has<LocalTransform2D>(
                reference.Entity))
        {
            throw new InvalidOperationException(
                "Entity does not have LocalTransform2D.");
        }

        _document = document;
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

    private void SetValue(LocalTransform2D value)
    {
        if (!_reference.IsAlive)
        {
            throw new InvalidOperationException(
                "Editor entity reference is no longer alive.");
        }

        if (!_world.EcsWorld.Has<LocalTransform2D>(
                _reference.Entity))
        {
            throw new InvalidOperationException(
                "Entity no longer has LocalTransform2D.");
        }

        _world.EcsWorld.Get<LocalTransform2D>(
            _reference.Entity) = value;

        _document.RefreshTransforms();
    }
}