using Engine.Editor.Actions;
using Engine.Editor.Assets;
using Engine.Editor.Documents;
using Engine.Editor.Inspection;
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

        ComponentTypes =
    new EditorComponentTypeRegistry();

        ActionContext =
            new EditorActionContext(
                this);

        Actions.Register(
            new UndoEditorAction());

        Actions.Register(
            new RedoEditorAction());

        AssetBrowser = null;
    }

    public EditorAssetBrowser? AssetBrowser { get; private set; }
    public EditorComponentTypeRegistry ComponentTypes { get; }

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

        try
        {
            Session.Open(
                document);

            return document;
        }
        catch
        {
            document.Dispose();
            throw;
        }
    }

    public void InitializeAssetBrowser(
    IEditorAssetSource source,
    string rootPath)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        if (AssetBrowser is not null)
        {
            throw new InvalidOperationException(
                "Asset browser has already been initialized.");
        }

        AssetBrowser =
            new EditorAssetBrowser(
                source,
                rootPath);
    }
}