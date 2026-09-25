namespace Engine.Editor.Actions;

public sealed class UndoEditorAction :
    IEditorAction
{
    public string Id =>
        "edit.undo";

    public string Name =>
        "Undo";

    public bool CanExecute(
        EditorActionContext context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        return context.ActiveDocument?.CommandHistory.CanUndo
               == true;
    }

    public void Execute(
        EditorActionContext context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        context.ActiveDocument?.Undo();
    }
}