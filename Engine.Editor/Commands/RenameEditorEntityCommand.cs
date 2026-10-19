
using Engine.ECS.Components;
using Engine.Editor.Entities;
using Engine.Worlds;

namespace Engine.Editor.Commands;

public sealed class RenameEditorEntityCommand : IEditorCommand
{
    private readonly World _world;
    private readonly EditorEntityReference _reference;
    private readonly EntityName _newName;

    private bool _originalStateCaptured;
    private bool _hadOriginalName;
    private EntityName _originalName;

    public RenameEditorEntityCommand(
        World world,
        EditorEntityReference reference,
        string newName)
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

        _world = world;
        _reference = reference;
        _newName = new EntityName(newName);
    }

    public void Execute()
    {
        EnsureEntityAlive();

        var entity = _reference.Entity;
        var ecsWorld = _world.EcsWorld;

        // Сохраняем исходное состояние только при первом
        // Execute. При Redo исходное имя не перезаписывается.
        if (!_originalStateCaptured)
        {
            _hadOriginalName =
                ecsWorld.Has<EntityName>(entity);

            if (_hadOriginalName)
            {
                _originalName =
                    ecsWorld.Get<EntityName>(entity);
            }

            _originalStateCaptured = true;
        }

        if (ecsWorld.Has<EntityName>(entity))
        {
            ecsWorld.Get<EntityName>(entity) = _newName;
        }
        else
        {
            ecsWorld.Add(
                entity,
                _newName);
        }
    }

    public void Undo()
    {
        EnsureEntityAlive();

        if (!_originalStateCaptured)
        {
            throw new InvalidOperationException(
                "Rename command has not been executed.");
        }

        var entity = _reference.Entity;
        var ecsWorld = _world.EcsWorld;

        if (_hadOriginalName)
        {
            if (ecsWorld.Has<EntityName>(entity))
            {
                ecsWorld.Get<EntityName>(entity) =
                    _originalName;
            }
            else
            {
                ecsWorld.Add(
                    entity,
                    _originalName);
            }
        }
        else
        {
            ecsWorld.Remove<EntityName>(entity);
        }
    }

    private void EnsureEntityAlive()
    {
        if (!_reference.IsAlive)
        {
            throw new InvalidOperationException(
                "Entity reference is no longer alive.");
        }
    }
}
