namespace Engine.Editor.Actions;

public interface IEditorAction
{
    string Id { get; }

    string Name { get; }

    bool CanExecute(
        EditorActionContext context);

    void Execute(
        EditorActionContext context);
}