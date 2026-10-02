using Engine.Editor.UI.Dialogs;
using System.Windows.Forms;

namespace Editor.Sandbox;

internal sealed class WindowsFileDialogService :
    IEditorFileDialogService
{
    public string? ShowOpenFile(
        string? initialDirectory = null)
    {
        return RunOnSta(
            () =>
            {
                using var dialog =
                    new OpenFileDialog
                    {
                        Title = "Open World",
                        Filter =
                            "World files (*.world)|*.world|All files (*.*)|*.*",
                        CheckFileExists = true,
                        Multiselect = false,
                        InitialDirectory =
                            GetInitialDirectory(
                                initialDirectory)
                    };

                return dialog.ShowDialog() ==
                       DialogResult.OK
                    ? dialog.FileName
                    : null;
            });
    }

    public string? ShowSaveFile(
        string? initialDirectory = null)
    {
        return RunOnSta(
            () =>
            {
                using var dialog =
                    new SaveFileDialog
                    {
                        Title = "Save World",
                        Filter =
                            "World files (*.world)|*.world|All files (*.*)|*.*",
                        OverwritePrompt = true,
                        AddExtension = true,
                        DefaultExt = "world",
                        InitialDirectory =
                            GetInitialDirectory(
                                initialDirectory)
                    };

                return dialog.ShowDialog() ==
                       DialogResult.OK
                    ? dialog.FileName
                    : null;
            });
    }

    private static string? RunOnSta(
        Func<string?> action)
    {
        ArgumentNullException.ThrowIfNull(
            action);

        string? result = null;
        Exception? exception = null;

        var thread =
            new Thread(
                () =>
                {
                    try
                    {
                        result =
                            action();
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                });

        thread.SetApartmentState(
            ApartmentState.STA);

        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            throw new InvalidOperationException(
                "System file dialog failed.",
                exception);
        }

        return result;
    }

    private static string? GetInitialDirectory(
        string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Directory.Exists(path)
            ? Path.GetFullPath(path)
            : null;
    }
}