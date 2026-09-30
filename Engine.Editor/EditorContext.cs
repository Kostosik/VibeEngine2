using Engine.Editor.Actions;
using Engine.Editor.Assets;
using Engine.Editor.Documents;
using Engine.Editor.Inspection;
using Engine.Editor.Panels;
using Engine.Editor.Persistence;
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

        RegisterDefaultPanels();
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

    public void SaveWorkspace(
    IEditorWorkspacePersistence persistence,
    string path)
    {
        ArgumentNullException.ThrowIfNull(
            persistence);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            path);

        persistence.Save(
            path,
            Workspace.CaptureState());
    }

    public void LoadWorkspace(
        IEditorWorkspacePersistence persistence,
        string path)
    {
        ArgumentNullException.ThrowIfNull(
            persistence);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            path);

        var state =
            persistence.Load(
                path);

        Workspace.RestoreState(
            state);
    }
    private void RegisterDefaultPanels()
    {
        Workspace.RegisterPanel(
            new EditorPanel(
                "Hierarchy",
                "Hierarchy"));

        Workspace.RegisterPanel(
            new EditorPanel(
                "Viewport",
                "Viewport"));

        Workspace.RegisterPanel(
            new EditorPanel(
                "Inspector",
                "Inspector"));

        Workspace.RegisterPanel(
            new EditorPanel(
                "AssetBrowser",
                "Asset Browser"));

        Workspace.RegisterPanel(
            new EditorPanel(
                "AssetPreview",
                "Asset Preview"));

        Workspace.Layout.GetPanel(
            "Hierarchy")
            .SetArea(
                EditorDockArea.Left);

        Workspace.Layout.GetPanel(
            "Hierarchy")
            .SetSize(
                280.0f);

        Workspace.Layout.GetPanel(
            "Viewport")
            .SetArea(
                EditorDockArea.Center);

        Workspace.Layout.GetPanel(
            "Viewport")
            .SetActive(
                true);

        Workspace.Layout.GetPanel(
            "Inspector")
            .SetArea(
                EditorDockArea.Right);

        Workspace.Layout.GetPanel(
            "Inspector")
            .SetSize(
                300.0f);

        Workspace.Layout.GetPanel(
            "AssetBrowser")
            .SetArea(
                EditorDockArea.Bottom);

        Workspace.Layout.GetPanel(
            "AssetBrowser")
            .SetSize(
                220.0f);

        Workspace.Layout.GetPanel(
            "AssetPreview")
            .SetArea(
                EditorDockArea.Bottom);

        Workspace.Layout.GetPanel(
            "AssetPreview")
            .SetOrder(
                1);

        Workspace.Layout.GetPanel(
            "AssetPreview")
            .SetSize(
                220.0f);
    }
}