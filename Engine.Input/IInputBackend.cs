namespace Engine.Input;

public interface IInputBackend
{
    void Update();

    float GetValue(InputBinding binding);
}