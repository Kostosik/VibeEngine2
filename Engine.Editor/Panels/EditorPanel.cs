namespace Engine.Editor.Panels;

public sealed class EditorPanel :
    IEditorPanel
{
    public EditorPanel(
        string id,
        string title,
        bool isOpen = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Id = id;
        Title = title;
        IsOpen = isOpen;
    }

    public string Id { get; }

    public string Title { get; }

    public bool IsOpen { get; set; }
}