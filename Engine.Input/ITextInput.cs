namespace Engine.Input;

public interface ITextInput
{
    IReadOnlyList<char> Characters { get; }

    bool IsPressed(TextInputKey key);
}