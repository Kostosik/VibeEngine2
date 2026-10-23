namespace Engine.Input;

public interface ITextInput
{
    IReadOnlyList<char> Characters { get; }
    bool IsDown(TextInputKey key) => IsPressed(key);
    bool IsPressed(TextInputKey key);
}