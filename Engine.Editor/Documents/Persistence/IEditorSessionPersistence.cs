using Engine.Editor.Documents;

namespace Engine.Editor.Persistence;

public interface IEditorSessionPersistence
{
    void Save(
        string path,
        EditorSessionState state);

    EditorSessionState Load(
        string path);
}