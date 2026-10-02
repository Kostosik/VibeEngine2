namespace Engine.Editor.Panels;

public interface IEditorPanel
{
    string Id { get; }

    string Title { get; }

    bool IsOpen { get; }

    public void SetOpen(
    bool isOpen);
}