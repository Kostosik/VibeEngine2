namespace Engine.Input;

public interface IInputDevice
{
    InputSnapshot Current { get; }

    InputSnapshot Previous { get; }

    void Update();
}