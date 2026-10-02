namespace Engine.Editor.UI.Dialogs;

public interface IEditorFileDialogService
{
    string? ShowOpenFile(
        string? initialDirectory = null);

    string? ShowSaveFile(
        string? initialDirectory = null);
}