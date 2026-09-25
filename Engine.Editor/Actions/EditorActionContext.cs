using Engine.Editor.Documents;
using Engine.Editor.Workspace;

namespace Engine.Editor.Actions;

public sealed class EditorActionContext
{
    public EditorActionContext(
        EditorContext editor)
    {
        ArgumentNullException.ThrowIfNull(
            editor);

        Editor = editor;
    }

    public EditorContext Editor { get; }

    public EditorSession Session =>
        Editor.Session;

    public EditorWorkspace Workspace =>
        Editor.Workspace;

    public EditorDocument? ActiveDocument =>
        Editor.ActiveDocument;
}