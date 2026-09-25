using Engine.Editor.Actions;
using Engine.Editor.Documents;
using Engine.Editor.Workspace;
using Engine.Worlds;

namespace Engine.Editor;

public sealed class EditorContext
{
    public EditorContext()
    {
        Session =
            new EditorSession();

        Workspace =
            new EditorWorkspace(
                Session);
        Actions =
    new EditorActionRegistry();

        ActionContext =
            new EditorActionContext(
                this);

        Actions.Register(
            new UndoEditorAction());

        Actions.Register(
            new RedoEditorAction());
    }

    public EditorActionRegistry Actions { get; }

    public EditorActionContext ActionContext { get; }

    public EditorSession Session { get; }

    public EditorWorkspace Workspace { get; }

    public EditorDocument? ActiveDocument =>
        Session.ActiveDocument;

    public EditorDocument OpenDocument(
    World world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        var document =
            new EditorDocument(
                world);

        Session.Open(
            document);

        return document;
    }
}