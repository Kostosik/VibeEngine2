namespace Engine.Editor.Panels;

public interface IEditorPanel
{
    string Id { get; }

    string Title { get; }

    bool IsOpen { get; set; }
}