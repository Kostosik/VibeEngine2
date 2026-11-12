
using Engine.Input;

namespace Game.Sandbox;

internal sealed class Sandbox3DPreviewControls
{
    public InputActionMap ActionMap { get; } = new();

    public InputAction Forward { get; } = new("PreviewForward");
    public InputAction Backward { get; } = new("PreviewBackward");

    public InputAction Left { get; } = new("PreviewLeft");
    public InputAction Right { get; } = new("PreviewRight");

    public InputAction Up { get; } = new("PreviewUp");
    public InputAction Down { get; } = new("PreviewDown");

    public Sandbox3DPreviewControls()
    {
        Bind(Forward, "Keyboard.W");
        Bind(Backward, "Keyboard.S");

        Bind(Left, "Keyboard.A");
        Bind(Right, "Keyboard.D");

        Bind(Up, "Keyboard.E");
        Bind(Down, "Keyboard.Q");
    }

    private void Bind(InputAction action, string path)
    {
        ActionMap.Bind(
            action,
            new InputBinding(path));
    }
}
