using Engine.Core.Application;
using Engine.Core.Time;
using Engine.Editor;
using Engine.Editor.Documents;
using Engine.Editor.UI.Shell;
using Engine.Input;
using Engine.UI.Core;
using Engine.Worlds;

namespace Engine.Editor.UI;

public sealed class EditorApplication :
    IApplication
{
    private bool _uiDirty = true;

    public EditorApplication(
        EditorContext editor,
        UiSystem ui,
        IInputBackend input)
    {
        ArgumentNullException.ThrowIfNull(
            editor);

        ArgumentNullException.ThrowIfNull(
            ui);

        ArgumentNullException.ThrowIfNull(
            input);

        Editor = editor;
        Ui = ui;
        Input = input;

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

    public IInputBackend Input { get; }

    public EditorUiHost UiHost { get; }

    public EditorMainShell MainShell { get; }

    public EditorDocument OpenDocument(
        World world)
    {
        var document =
            Editor.OpenDocument(
                world);

        SubscribeDocument(
            document);

        MarkUiDirty();

        return document;
    }

    public void Initialize()
    {
        MarkUiDirty();
        RefreshIfNeeded();
    }

    public void Update(
        TimeSnapshot time)
    {
        Input.Update();

        RefreshIfNeeded();

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

    private void SubscribeDocument(
        EditorDocument document)
    {
        document.EntitySelection.Changed +=
            MarkUiDirty;

        document.CommandHistory.Changed +=
            MarkUiDirty;
    }

    private void MarkUiDirty()
    {
        _uiDirty = true;
    }

    private void RefreshIfNeeded()
    {
        if (!_uiDirty)
        {
            return;
        }

        _uiDirty = false;

        MainShell.Refresh();
    }
}