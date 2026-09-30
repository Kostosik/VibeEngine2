using Engine.Editor;
using Engine.Editor.Actions;
using Engine.Editor.Commands;
using Engine.Editor.Documents;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class EditorActionTests
{
    [Fact]
    public void Registry_Execute_DoesNotExecuteActionWhenCanExecuteIsFalse()
    {
        var editor =
            new EditorContext();

        var action =
            new TestAction
            {
                CanExecuteResult = false
            };

        editor.Actions.Register(
            action);

        editor.Actions.Execute(
            action.Id,
            editor.ActionContext);

        Assert.Equal(
            0,
            action.ExecuteCount);
    }

    [Fact]
    public void Registry_Execute_ExecutesActionWhenCanExecuteIsTrue()
    {
        var editor =
            new EditorContext();

        var action =
            new TestAction
            {
                CanExecuteResult = true
            };

        editor.Actions.Register(
            action);

        editor.Actions.Execute(
            action.Id,
            editor.ActionContext);

        Assert.Equal(
            1,
            action.ExecuteCount);

        Assert.Same(
            editor.ActionContext,
            action.LastContext);
    }

    [Fact]
    public void Registry_Execute_UnknownActionThrows()
    {
        var editor =
            new EditorContext();

        Assert.Throws<KeyNotFoundException>(
            () =>
                editor.Actions.Execute(
                    "missing",
                    editor.ActionContext));
    }

    [Fact]
    public void Registry_RejectsDuplicateActionId()
    {
        var registry =
            new EditorActionRegistry();

        registry.Register(
            new TestAction
            {
                CanExecuteResult = true
            });

        Assert.Throws<InvalidOperationException>(
            () =>
                registry.Register(
                    new TestAction
                    {
                        CanExecuteResult = true
                    }));

        Assert.Single(
            registry.Actions);
    }

    [Fact]
    public void UndoAction_UsesActiveDocument()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var editor =
            new EditorContext();

        var document =
            editor.OpenDocument(
                world);

        document.Execute(
            new CreateEditorEntityCommand(
                document));

        Assert.True(
            document.CommandHistory.CanUndo);

        editor.Actions.Execute(
            "edit.undo",
            editor.ActionContext);

        Assert.False(
            document.CommandHistory.CanUndo);

        Assert.True(
            document.CommandHistory.CanRedo);
    }

    [Fact]
    public void RedoAction_UsesActiveDocument()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var editor =
            new EditorContext();

        var document =
            editor.OpenDocument(
                world);

        document.Execute(
            new CreateEditorEntityCommand(
                document));

        editor.Actions.Execute(
            "edit.undo",
            editor.ActionContext);

        Assert.True(
            document.CommandHistory.CanRedo);

        editor.Actions.Execute(
            "edit.redo",
            editor.ActionContext);

        Assert.True(
            document.CommandHistory.CanUndo);

        Assert.False(
            document.CommandHistory.CanRedo);
    }
    [Fact]
    public void UndoAction_CannotExecuteWithoutUndoHistory()
    {
        var editor =
            new EditorContext();

        Assert.True(
            editor.Actions.TryGet(
                "edit.undo",
                out var action));

        Assert.NotNull(action);

        Assert.False(
            action!.CanExecute(
                editor.ActionContext));
    }

    [Fact]
    public void RedoAction_CannotExecuteWithoutRedoHistory()
    {
        var editor =
            new EditorContext();

        Assert.True(
            editor.Actions.TryGet(
                "edit.redo",
                out var action));

        Assert.NotNull(action);

        Assert.False(
            action!.CanExecute(
                editor.ActionContext));
    }

    private sealed class TestAction :
        IEditorAction
    {
        public string Id =>
            "test.action";

        public string Name =>
            "Test";

        public bool CanExecuteResult { get; set; }

        public int ExecuteCount { get; private set; }

        public EditorActionContext? LastContext { get; private set; }

        public bool CanExecute(
            EditorActionContext context)
        {
            ArgumentNullException.ThrowIfNull(
                context);

            return CanExecuteResult;
        }

        public void Execute(
            EditorActionContext context)
        {
            ArgumentNullException.ThrowIfNull(
                context);

            ExecuteCount++;
            LastContext = context;
        }
    }
}