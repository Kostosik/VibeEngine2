namespace Engine.Editor.UI;

public interface IClipboardService
{
    void SetText(string text);

    string? GetText();
}