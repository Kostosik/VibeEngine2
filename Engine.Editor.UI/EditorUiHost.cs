using Engine.Editor;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI;

public sealed class EditorUiHost
{
    public EditorUiHost(
        EditorContext editor,
        UiSystem ui)
    {
        ArgumentNullException.ThrowIfNull(
            editor);

        ArgumentNullException.ThrowIfNull(
            ui);

        Editor =
            editor;

        Ui =
            ui;

        Root =
            new UiCanvas();

        Ui.Root.AddChild(
            Root);
    }

    public EditorContext Editor { get; }

    public UiSystem Ui { get; }

    public UiCanvas Root { get; }
}