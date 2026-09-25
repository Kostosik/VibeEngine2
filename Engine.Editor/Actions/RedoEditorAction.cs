namespace Engine.Editor.Actions;

public sealed class RedoEditorAction :
    IEditorAction
{
    public string Id =>
        "edit.redo";

    public string Name =>
        "Redo";

    public bool CanExecute(
        EditorActionContext context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        return context.ActiveDocument?.CommandHistory.CanRedo
               == true;
    }

    public void Execute(
        EditorActionContext context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        context.ActiveDocument?.Redo();
    }
}