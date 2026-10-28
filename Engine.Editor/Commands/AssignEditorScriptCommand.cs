using Engine.Editor.Entities;
using Engine.Scripting;
using Engine.Worlds;

namespace Engine.Editor.Commands;

public sealed class AssignEditorScriptCommand : IEditorCommand
{
    private readonly World _world;
    private readonly EditorEntityReference _reference;
    private readonly ScriptDefinition _definition;

    private ScriptAssignmentsComponent _previousAssignments;
    private bool _hadPreviousAssignments;
    private bool _captured;
    private bool _executed;

    public AssignEditorScriptCommand(
        World world,
        EditorEntityReference reference,
        ScriptDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(definition);

        reference.EnsureWorld(world);

        if (!reference.IsAlive)
        {
            throw new ArgumentException(
                "Editor entity reference is not alive.",
                nameof(reference));
        }

        _world = world;
        _reference = reference;
        _definition = definition;
    }

    public void Execute()
    {
        EnsureEntityExists();

        var ecsWorld = _world.EcsWorld;
        var entity = _reference.Entity;

        var hasAssignments =
            ecsWorld.Has<ScriptAssignmentsComponent>(entity);

        var current =
            hasAssignments
                ? ecsWorld.Get<ScriptAssignmentsComponent>(entity)
                : default;

        if (hasAssignments &&
            current.Contains(_definition.ScriptId))
        {
            throw new InvalidOperationException(
                $"Script '{_definition.ScriptId}' is already assigned " +
                $"to entity '{entity}'.");
        }

        if (!_captured)
        {
            _hadPreviousAssignments = hasAssignments;
            _previousAssignments = current;
            _captured = true;
        }

        var assignment = new ScriptAssignment(
            _definition.ScriptId,
            _definition.DefaultConfigurationJson);

        var updated = hasAssignments
            ? current.WithAdded(assignment)
            : new ScriptAssignmentsComponent(
                new[] { assignment });

        ecsWorld.Add(entity, updated);

        _executed = true;
    }

    public void Undo()
    {
        if (!_executed)
        {
            throw new InvalidOperationException(
                "Script assignment has not been executed.");
        }

        EnsureEntityExists();

        var ecsWorld = _world.EcsWorld;
        var entity = _reference.Entity;

        if (_hadPreviousAssignments)
        {
            ecsWorld.Add(
                entity,
                _previousAssignments);
        }
        else
        {
            ecsWorld.Remove<ScriptAssignmentsComponent>(
                entity);
        }

        _executed = false;
    }

    private void EnsureEntityExists()
    {
        if (!_reference.IsAlive ||
            !_world.EcsWorld.Exists(_reference.Entity))
        {
            throw new InvalidOperationException(
                "Editor entity reference is not alive.");
        }
    }
}