using Engine.Input;

namespace Engine.UI.Input;

public sealed class UiKeyEvent
{
    public UiKeyEvent(
        TextInputKey key)
    {
        Key = key;
    }

    public TextInputKey Key { get; }

    public bool Handled { get; set; }
}