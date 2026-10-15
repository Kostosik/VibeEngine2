using Engine.Input;

namespace Game.Sandbox;

public sealed class SandboxInputActions
{
    public SandboxInputActions()
    {
        MoveUp = new InputAction("MoveUp");
        MoveDown = new InputAction("MoveDown");
        MoveLeft = new InputAction("MoveLeft");
        MoveRight = new InputAction("MoveRight");

        SwitchMode = new InputAction("SwitchMode");
        Reset = new InputAction("Reset");
        Spawn = new InputAction("Spawn");
        Clear = new InputAction("Clear");
        Force = new InputAction("Force");
        ToggleTrigger = new InputAction("ToggleTrigger");
        Rotate = new InputAction("Rotate");

        Save = new InputAction("Save");
        Load = new InputAction("Load");

        ToggleUiShowcase = new InputAction("ToggleUiShowcase");
        PlayAudio = new InputAction("PlayAudio");

        NetworkTest = new InputAction("NetworkTest");
        NetworkReplicationTest =
            new InputAction("NetworkReplicationTest");

        // Сохраняем прежнее имя действия:
        // его идентификатор не меняется.
        PlayerReplicationTest =
            new InputAction("PlayerReplication");

        ConsoleScroll = new InputAction("ConsoleScroll");

        Bind(MoveUp, "Keyboard.W");
        Bind(MoveDown, "Keyboard.S");
        Bind(MoveLeft, "Keyboard.A");
        Bind(MoveRight, "Keyboard.D");

        Bind(SwitchMode, "Keyboard.F2");
        Bind(Reset, "Keyboard.F3");
        Bind(Spawn, "Keyboard.F4");
        Bind(Clear, "Keyboard.F5");

        Bind(Save, "Keyboard.F6");
        Bind(Load, "Keyboard.F7");
        Bind(ToggleUiShowcase, "Keyboard.F8");
        Bind(PlayAudio, "Keyboard.F9");

        Bind(NetworkTest, "Keyboard.F10");
        Bind(NetworkReplicationTest, "Keyboard.F11");
        Bind(PlayerReplicationTest, "Keyboard.F12");

        Bind(Force, "Keyboard.Space");
        Bind(ToggleTrigger, "Keyboard.T");
        Bind(Rotate, "Keyboard.R");

        Bind(ConsoleScroll, "Mouse.Scroll");
    }

    public InputActionMap ActionMap { get; } = new();

    public InputAction MoveUp { get; }
    public InputAction MoveDown { get; }
    public InputAction MoveLeft { get; }
    public InputAction MoveRight { get; }

    public InputAction SwitchMode { get; }
    public InputAction Reset { get; }
    public InputAction Spawn { get; }
    public InputAction Clear { get; }
    public InputAction Force { get; }
    public InputAction ToggleTrigger { get; }
    public InputAction Rotate { get; }

    public InputAction Save { get; }
    public InputAction Load { get; }

    public InputAction ToggleUiShowcase { get; }
    public InputAction PlayAudio { get; }

    public InputAction NetworkTest { get; }
    public InputAction NetworkReplicationTest { get; }
    public InputAction PlayerReplicationTest { get; }

    public InputAction ConsoleScroll { get; }

    private void Bind(
        InputAction action,
        string path)
    {
        ActionMap.Bind(
            action,
            new InputBinding(path));
    }
}