using Engine.Editor.Actions;
using Engine.Editor.Assets;
using Engine.Editor.Components;
using Engine.Editor.Documents;
using Engine.Editor.Documents.Persistence;
using Engine.Editor.Inspection;
using Engine.Editor.Panels;
using Engine.Editor.Persistence;
using Engine.Editor.Projects;
using Engine.Editor.Workspace;
using Engine.Worlds;

namespace Engine.Editor;

public sealed class EditorContext
{
    private string? _defaultAssetBrowserRootPath;

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
        ComponentDependencies =
    EditorComponentDependencyRegistry.CreateDefault();
        ActionContext =
            new EditorActionContext(
                this);

        Actions.Register(
            new UndoEditorAction());

        Actions.Register(
            new RedoEditorAction());
        Actions.Register(
    new ValidateEditorAction());

        AssetBrowser = null;
    }
    public EditorComponentDependencyRegistry ComponentDependencies { get; }
    public EditorAssetBrowser? AssetBrowser { get; private set; }
    public EditorComponentTypeRegistry ComponentTypes { get; }

    public EditorActionRegistry Actions { get; }

    public EditorActionContext ActionContext { get; }

    public EditorSession Session { get; }

    public EditorWorkspace Workspace { get; }

    /// <summary>
    /// The currently selected game project.
    /// This is independent from the active editor document.
    /// </summary>
    public GameProject? ActiveGameProject { get; private set; }

    public event Action<GameProject?>? ActiveGameProjectChanged;
    public event Action<EditorDocument>? WorldDocumentConfiguring;
    public void SetActiveGameProject(GameProject? project)
    {
        if (ReferenceEquals(ActiveGameProject, project))
        {
            return;
        }

        if (ActiveGameProject is not null &&
            project is not null &&
            PathsEqual(
                ActiveGameProject.RootDirectory,
                project.RootDirectory))
        {
            return;
        }

        var assetRootPath =
            project?.AssetsDirectoryPath
            ?? _defaultAssetBrowserRootPath;

        if (AssetBrowser is not null &&
            assetRootPath is not null)
        {
            AssetBrowser.SetRootPath(assetRootPath);
        }

        ActiveGameProject = project;

        ActiveGameProjectChanged?.Invoke(project);
    }

    public EditorDocument? ActiveDocument =>
        Session.ActiveDocument;

    public EditorDocument OpenDocument(
    World world,
    string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(world);

        var document = new EditorDocument(
            world,
            filePath);

        try
        {
            document.SetProject(
                ResolveProjectForDocument(filePath));

            WorldDocumentConfiguring?.Invoke(document);

            Session.Open(document);

            return document;
        }
        catch
        {
            document.Dispose();
            throw;
        }
    }

    private GameProject? ResolveProjectForDocument(
    string? path,
    GameProject? preferredProject = null)
    {
        if (path is null)
        {
            return null;
        }

        if (preferredProject is not null &&
            preferredProject.ContainsPath(path))
        {
            return preferredProject;
        }

        if (ActiveGameProject is not null &&
            ActiveGameProject.ContainsPath(path))
        {
            return ActiveGameProject;
        }

        return null;
    }

    public EditorSessionState CaptureSessionState()
    {
        var paths =
            new List<string>();

        foreach (var document in
                 Session.Documents)
        {
            if (document.FilePath is null)
            {
                continue;
            }

            paths.Add(
                Path.GetFullPath(
                    document.FilePath));
        }

        var activePath =
            ActiveDocument?.FilePath is string filePath
                ? Path.GetFullPath(
                    filePath)
                : null;

        if (activePath is not null &&
            !paths.Contains(
                activePath,
                GetPathComparer()))
        {
            activePath = null;
        }

        return new EditorSessionState(
            paths,
            activePath);
    }

    public void SaveSession(
        IEditorSessionPersistence persistence,
        string path)
    {
        ArgumentNullException.ThrowIfNull(
            persistence);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            path);

        persistence.Save(
            path,
            CaptureSessionState());
    }

    public void LoadSession(
        IEditorSessionPersistence persistence,
        IEditorDocumentFileService files,
        string path)
    {
        ArgumentNullException.ThrowIfNull(
            persistence);

        ArgumentNullException.ThrowIfNull(
            files);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            path);

        var state =
            persistence.Load(
                path);

        if (Session.AllDocuments.Any(
                static document => document.IsDirty))
        {
            throw new InvalidOperationException(
                "Cannot restore an editor session while dirty documents are open.");
        }

        foreach (var document in Session.Documents.ToArray())
        {
            Session.Close(document);
        }

        EditorDocument? activeDocument = null;

        try
        {
            foreach (var documentPath in
                     state.DocumentPaths)
            {
                var document =
                    files.Open(
                        documentPath);

                if (state.ActiveDocumentPath is not null &&
                    PathsEqual(
                        document.FilePath,
                        state.ActiveDocumentPath))
                {
                    activeDocument =
                        document;
                }
            }
        }
        catch
        {
            foreach (var document in Session.Documents.ToArray())
            {
                Session.Discard(document);
            }
            throw;
        }

        if (activeDocument is not null)
        {
            Session.Activate(
                activeDocument);
        }
    }

    private static StringComparer GetPathComparer()
    {
        return OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
    }

    private static bool PathsEqual(
        string? left,
        string? right)
    {
        if (left is null ||
            right is null)
        {
            return false;
        }

        return string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);
    }

    public IEditorDocumentFileService CreateDocumentFileService(
    IEditorDocumentPersistence persistence)
    {
        ArgumentNullException.ThrowIfNull(
            persistence);

        return new EditorDocumentFileService(
            this,
            persistence);
    }

    public EditorDocument OpenDocument(
        IEditorDocumentFileService files,
        string path)
    {
        ArgumentNullException.ThrowIfNull(
            files);

        return files.Open(
            path);
    }

    public void SaveDocument(
        IEditorDocumentFileService files)
    {
        ArgumentNullException.ThrowIfNull(
            files);

        var document =
            ActiveDocument
            ?? throw new InvalidOperationException(
                "There is no active document.");

        files.Save(
            document);
    }

    public void SaveDocumentAs(
        IEditorDocumentFileService files,
        string path)
    {
        ArgumentNullException.ThrowIfNull(
            files);

        var document =
            ActiveDocument
            ?? throw new InvalidOperationException(
                "There is no active document.");

        files.SaveAs(
            document,
            path);
    }

    public bool CloseDocument(
        EditorDocument document,
        EditorDocumentCloseDecision decision,
        IEditorDocumentFileService? files = null)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        if (!Session.Documents.Contains(
                document))
        {
            return false;
        }

        switch (decision)
        {
            case EditorDocumentCloseDecision.Cancel:
                return false;

            case EditorDocumentCloseDecision.Discard:
                return Session.Discard(
                    document);

            case EditorDocumentCloseDecision.Save:
                if (files is null)
                {
                    throw new ArgumentNullException(
                        nameof(files));
                }

                if (document.IsDirty ||
                    document.FilePath is null)
                {
                    files.Save(
                        document);
                }

                return Session.Close(
                    document);
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(decision),
                    decision,
                    null);
        }
    }

    public void SaveDocument(
    EditorDocument document,
    IEditorDocumentPersistence persistence,
    string path)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        ArgumentNullException.ThrowIfNull(
            persistence);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            path);

        if (!Session.Documents.Contains(
                document))
        {
            throw new InvalidOperationException(
                "Document is not open.");
        }

        var project = ResolveProjectForDocument(
    path,
    document.Project);

        persistence.Save(
            path,
            document);

        document.SetFilePath(
            path);

        document.SetProject(project);

        document.MarkSaved();
    }

    public EditorDocument LoadDocument(
        IEditorDocumentPersistence persistence,
        string path)
    {
        ArgumentNullException.ThrowIfNull(
            persistence);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            path);

        var world =
            persistence.Load(
                path);

        return OpenDocument(
            world,
            path);
    }

    public void SetStartupScene(EditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!Session.Documents.Contains(document))
        {
            throw new InvalidOperationException(
                "Document is not open in this editor.");
        }

        var filePath = document.FilePath
            ?? throw new InvalidOperationException(
                "Save the world document before setting it as the startup scene.");

        var project = ActiveGameProject
            ?? throw new InvalidOperationException(
                "Open a game project first.");

        if (document.Project is null ||
            !PathsEqual(
                document.Project.RootDirectory,
                project.RootDirectory))
        {
            throw new InvalidOperationException(
                "The world document must belong to the active game project. " +
                "Save it inside the project or reopen it from that project.");
        }

        project.SetStartupScene(filePath);
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

        _defaultAssetBrowserRootPath =
            Path.GetFullPath(rootPath);

        var activeRootPath =
            ActiveGameProject?.AssetsDirectoryPath
            ?? _defaultAssetBrowserRootPath;

        AssetBrowser =
            new EditorAssetBrowser(
                source,
                activeRootPath);
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
        "Console",
        "Console",
        isOpen: true));

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
    "AssetBrowser")
    .SetActive(true);

        Workspace.Layout.GetPanel(
            "Console")
            .SetArea(EditorDockArea.Bottom);

        Workspace.Layout.GetPanel(
            "Console")
            .SetOrder(2);

        Workspace.Layout.GetPanel(
            "Console")
            .SetSize(280.0f);
    }
}