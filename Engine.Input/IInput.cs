namespace Engine.Input;

public interface IInput
{
    void Update();

    bool IsDown(InputAction action);

    bool IsPressed(InputAction action);

    bool IsReleased(InputAction action);

    float GetValue(InputAction action);
}