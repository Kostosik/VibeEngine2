using Engine.Editor.Documents;

namespace Engine.Editor.Actions;

public sealed class ValidateEditorAction :
    IEditorAction
{
    public string Id =>
        "validation.validate";

    public string Name =>
        "Validate";

    public bool CanExecute(
        EditorActionContext context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        return context.ActiveDocument is not null;
    }

    public void Execute(
        EditorActionContext context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        context.ActiveDocument?.Validate();
    }
}