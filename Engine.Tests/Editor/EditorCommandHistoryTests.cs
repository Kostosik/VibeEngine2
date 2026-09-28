using Engine.Editor.Commands;

namespace Engine.Tests.Editor;

public sealed class EditorCommandHistoryTests
{
    [Fact]
    public void UndoThenExecuteNewCommand_StaysDirty()
    {
        var history =
            new EditorCommandHistory();

        history.Execute(
            new TestCommand());

        history.MarkSaved();

        history.Undo();

        history.Execute(
            new TestCommand());

        Assert.True(
            history.IsDirty);

        Assert.False(
            history.CanRedo);
    }

    [Fact]
    public void FailedExecute_PreservesRedoBranch()
    {
        var history =
            new EditorCommandHistory();

        history.Execute(
            new TestCommand());

        history.Undo();

        Assert.True(
            history.CanRedo);

        Assert.Throws<InvalidOperationException>(
            () =>
                history.Execute(
                    new FailingCommand()));

        Assert.True(
            history.CanRedo);

        history.Redo();

        Assert.False(
            history.CanRedo);
    }

    [Fact]
    public void UndoAndRedo_BackToSavedState_IsClean()
    {
        var history =
            new EditorCommandHistory();

        history.Execute(
            new TestCommand());

        history.MarkSaved();

        history.Undo();

        Assert.True(
            history.IsDirty);

        history.Redo();

        Assert.False(
            history.IsDirty);
    }

    private sealed class TestCommand : IEditorCommand
    {
        public void Execute()
        {
        }

        public void Undo()
        {
        }
    }

    private sealed class FailingCommand : IEditorCommand
    {
        public void Execute()
        {
            throw new InvalidOperationException(
                "Expected test failure.");
        }

        public void Undo()
        {
        }
    }
}