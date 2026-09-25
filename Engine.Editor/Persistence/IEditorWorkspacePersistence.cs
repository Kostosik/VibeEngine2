using Engine.Editor.Workspace;

namespace Engine.Editor.Persistence;

public interface IEditorWorkspacePersistence
{
    void Save(
        string path,
        EditorWorkspaceState state);

    EditorWorkspaceState Load(
        string path);
}