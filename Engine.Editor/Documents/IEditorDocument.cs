namespace Engine.Editor.Documents;

public interface IEditorDocument
{
    /// <summary>
    /// Stable identifier of the document type.
    /// </summary>
    string DocumentTypeId { get; }

    /// <summary>
    /// Name shown in the editor UI.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Path of the document, if it has been saved.
    /// </summary>
    string? FilePath { get; }

    bool IsDirty { get; }

    void MarkSaved();
}