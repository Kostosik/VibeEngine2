using Engine.Editor.Documents;

namespace Engine.Editor.Documents.Persistence;

public interface IEditorDocumentFileService
{
    EditorDocument Open(
        string path);

    void Save(
        EditorDocument document);

    void SaveAs(
        EditorDocument document,
        string path);
}