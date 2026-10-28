using Engine.Editor.Commands;
using Engine.Editor.Documents;
using Engine.Scripting;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class ScriptAssignmentEditorCommandTests
{
    [Fact]
    public void AssignScript_ExecuteUndoRedo_PreservesOtherAssignments()
    {
        using var ecsWorld = new Engine.ECS.World();

        using var world = new World(
            new ChunkSize(16, 16),
            ecsWorld);

        using var document = new EditorDocument(world);

        var entity = ecsWorld.CreateEntity();

        ecsWorld.Add(
            entity,
            new ScriptAssignmentsComponent(
                new[]
                {
                    new ScriptAssignment("game.player", "{}")
                }));

        var definition = new ScriptDefinition(
            ScriptDefinition.CurrentFormatVersion,
            "game.door",
            """{"speed":2}""");

        var command = new AssignEditorScriptCommand(
            world,
            document.GetEntityReference(entity),
            definition);

        document.Execute(command);

        var assignments =
            ecsWorld.Get<ScriptAssignmentsComponent>(entity);

        Assert.Equal(2, assignments.Count);
        Assert.True(assignments.Contains("game.player"));
        Assert.True(assignments.Contains("game.door"));

        Assert.Equal(
            """{"speed":2}""",
            GetAssignment(assignments, "game.door").ConfigurationJson);

        document.Undo();

        assignments =
            ecsWorld.Get<ScriptAssignmentsComponent>(entity);

        Assert.Single(assignments.Assignments.ToArray());
        Assert.True(assignments.Contains("game.player"));
        Assert.False(assignments.Contains("game.door"));

        document.Redo();

        assignments =
            ecsWorld.Get<ScriptAssignmentsComponent>(entity);

        Assert.Equal(2, assignments.Count);
        Assert.True(assignments.Contains("game.door"));
    }

    private static ScriptAssignment GetAssignment(
        ScriptAssignmentsComponent assignments,
        string scriptId)
    {
        Assert.True(
            assignments.TryGet(scriptId, out var assignment));

        return assignment;
    }
}