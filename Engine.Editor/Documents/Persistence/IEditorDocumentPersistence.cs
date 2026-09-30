using Engine.Editor.Documents;
using Engine.Worlds;

namespace Engine.Editor.Documents.Persistence;

public interface IEditorDocumentPersistence
{
    void Save(
        string path,
        EditorDocument document);

    World Load(
        string path);
}