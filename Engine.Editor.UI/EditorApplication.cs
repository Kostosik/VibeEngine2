using Engine.Core.Application;
using Engine.Core.Time;
using Engine.Editor;
using Engine.Editor.Documents;
using Engine.Editor.UI.Shell;
using Engine.UI.Core;
using Engine.Worlds;

namespace Engine.Editor.UI;

public sealed class EditorApplication :
    IApplication
{
    public EditorApplication(
        EditorContext editor,
        UiSystem ui)
    {
        ArgumentNullException.ThrowIfNull(
            editor);

        ArgumentNullException.ThrowIfNull(
            ui);

        Editor = editor;
        Ui = ui;

        UiHost =
            new EditorUiHost(
                editor,
                ui);

        MainShell =
            new EditorMainShell(
                editor);

        UiHost.Root.AddChild(
            MainShell);
    }

    public EditorContext Editor { get; }

    public UiSystem Ui { get; }

    public EditorUiHost UiHost { get; }

    public EditorMainShell MainShell { get; }

    public EditorDocument OpenDocument(
        World world)
    {
        var document =
            Editor.OpenDocument(
                world);

        MainShell.Refresh();

        return document;
    }

    public void Initialize()
    {
        MainShell.Refresh();
    }

    public void Update(
        TimeSnapshot time)
    {
        MainShell.Refresh();

        Ui.Update(
            time.Delta.TotalSeconds);
    }

    public void FixedUpdate(
        SimulationTime time)
    {
    }

    public void Render(
        double interpolationAlpha)
    {
        Ui.Render();
    }

    public void Shutdown()
    {
    }
}