using Engine.Core.Application;
using Engine.Core.Time;
using Engine.Editor.UI;
using Engine.Tooling.Debugging;

namespace Editor.Sandbox;

internal sealed class EditorToolingApplication :
IApplication
{
    private readonly EditorApplication _editor;
    private readonly DebugConsoleOverlay _consoleOverlay;
public EditorToolingApplication(
    EditorApplication editor,
    DebugConsoleOverlay consoleOverlay)
    {
        ArgumentNullException.ThrowIfNull(
            editor);

        ArgumentNullException.ThrowIfNull(
            consoleOverlay);

        _editor = editor;
        _consoleOverlay = consoleOverlay;
    }

    public void Initialize()
    {
        _editor.Initialize();
    }

    public void Update(
        TimeSnapshot time)
    {
        _editor.Update(
            time);

        _consoleOverlay.Update();
    }

    public void FixedUpdate(
        SimulationTime time)
    {
        _editor.FixedUpdate(
            time);
    }

    public void Render(
        double interpolationAlpha)
    {
        _editor.Render(
            interpolationAlpha);

        _consoleOverlay.Render();
    }

    public void Shutdown()
    {
        _editor.Shutdown();
    }
}
