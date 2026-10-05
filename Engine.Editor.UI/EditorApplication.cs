using Engine.Core.Application;
using Engine.Core.Time;
using Engine.Editor.UI.Dialogs;
using Engine.Editor.Documents;
using Engine.Editor.Documents.Persistence;
using Engine.Editor.UI.Authoring;
using Engine.Editor.UI.Shell;
using Engine.Graphics.Resources;
using Engine.Input;
using Engine.UI.Core;
using Engine.Worlds;

namespace Engine.Editor.UI;

public sealed class EditorApplication :
    IApplication
{
    private readonly HashSet<EditorDocument> _subscribedDocuments = new();
    private bool _uiDirty = true;
    private readonly string _uiAssetPath;
    private readonly IEditorDocumentPersistence?
    _documentPersistence;
    private readonly IEditorFileDialogService _fileDialogs;
    public EditorUiDocument UiDocument { get; }
    private readonly IEditorDocumentFileService?
    _documentFiles;

    private readonly EditorDocumentCloseDialog
    _documentCloseDialog;

    private bool _applicationClosePending;

    public event Action? ApplicationCloseRequested;

    private EditorDocument? _pendingCloseDocument;

    private readonly EditorErrorDialog
    _errorDialog;
    public event Action? NewDocumentRequested;
    public event Action? OpenDocumentRequested;
    public event Action? SaveDocumentAsRequested;
    public EditorApplication(
     EditorContext editor,
     UiSystem ui,
     IInputBackend input,
     ITextureResourceManager assetPreviewTextures,
     EditorUiDocument uiDocument,
     string uiAssetPath, IEditorFileDialogService fileDialogs, IEditorDocumentPersistence? documentPersistence = null, IEditorDocumentFileService? documentFiles = null)
    {
        ArgumentNullException.ThrowIfNull(
            editor);

        ArgumentNullException.ThrowIfNull(
            ui);

        ArgumentNullException.ThrowIfNull(
            input);

        ArgumentNullException.ThrowIfNull(
            assetPreviewTextures);

        ArgumentNullException.ThrowIfNull(
            uiDocument);

        ArgumentNullException.ThrowIfNull(
    fileDialogs);

        Editor = editor;
        Ui = ui;
        Input = input;
        AssetPreviewTextures = assetPreviewTextures;
        UiDocument = uiDocument;

        _fileDialogs =
    fileDialogs;

        UiHost =
            new EditorUiHost(
                editor,
                ui);

        _errorDialog =
    new EditorErrorDialog(
        ui.Overlays,
        ui.Focus);

        _documentCloseDialog =
    new EditorDocumentCloseDialog(
        ui.Overlays,
        ui.Focus);

        _documentCloseDialog.SaveRequested +=
            SavePendingDocument;

        _documentCloseDialog.DiscardRequested +=
            DiscardPendingDocument;

        _documentCloseDialog.Closed +=
            ClearPendingCloseDocument;

        _uiAssetPath = uiAssetPath;
        _documentPersistence =
    documentPersistence;
        MainShell =
            new EditorMainShell(
                editor,
                assetPreviewTextures,
                uiDocument);

        MainShell.SaveRequested +=
            SaveActiveDocument;

        MainShell.NewRequested +=
    RequestNewDocument;

        MainShell.OpenRequested +=
            RequestOpenDocument;

        MainShell.SaveAsRequested +=
            RequestSaveDocumentAs;

        MainShell.CloseRequested +=
            RequestCloseDocument;

        Editor.Session.DocumentClosed +=
    UnsubscribeDocument;
        UiHost.Root.AddChild(
            MainShell);

        Editor.Session.ActiveDocumentChanged +=
    OnActiveDocumentChanged;

        Editor.Workspace.Changed +=
    MarkUiDirty;

        UiDocument.Changed +=
            MarkUiDirty;

        if (editor.AssetBrowser is not null)
        {
            editor.AssetBrowser.Changed +=
                MarkUiDirty;
        }

        _documentFiles = documentFiles;
    }

    public EditorDocument OpenDocument(
    string path)
    {
        var files =
            _documentFiles
            ?? throw new InvalidOperationException(
                "Document file service is not configured.");

        var document =
            Editor.OpenDocument(
                files,
                path);

        SubscribeDocument(
            document);

        MarkUiDirty();

        return document;
    }

    public void SaveDocument()
    {
        var files =
            _documentFiles
            ?? throw new InvalidOperationException(
                "Document file service is not configured.");

        Editor.SaveDocument(
            files);

        MarkUiDirty();
    }

    public void SaveDocumentAs(
        string path)
    {
        var files =
            _documentFiles
            ?? throw new InvalidOperationException(
                "Document file service is not configured.");

        Editor.SaveDocumentAs(
            files,
            path);

        MarkUiDirty();
    }

    public bool CloseDocument(
        EditorDocument document,
        EditorDocumentCloseDecision decision)
    {
        var closed =
            Editor.CloseDocument(
                document,
                decision,
                _documentFiles);

        if (closed)
        {
            MarkUiDirty();
        }

        return closed;
    }

    private void SaveActiveDocument()
    {
        if (MainShell.IsUiMode)
        {
            SaveUiDocument();
            return;
        }

        var document =
            Editor.ActiveDocument;

        if (document is null)
        {
            return;
        }

        if (document.FilePath is null)
        {
            RequestSaveDocumentAs();
            return;
        }

        if (_documentPersistence is null)
        {
            return;
        }

        Editor.SaveDocument(
            document,
            _documentPersistence,
            document.FilePath);

        MarkUiDirty();
    }

    private void OnActiveDocumentChanged(
    EditorDocument? document)
    {
        MarkUiDirty();
    }

    private void SaveUiDocument()
    {
        EditorUiAssetFile.Save(
            UiDocument,
            _uiAssetPath);

        UiDocument.MarkSaved();

        MarkUiDirty();
    }

    public EditorContext Editor { get; }

    public UiSystem Ui { get; }

    public IInputBackend Input { get; }

    public ITextureResourceManager AssetPreviewTextures { get; }

    public EditorUiHost UiHost { get; }

    public EditorMainShell MainShell { get; }

    public EditorDocument OpenDocument(
        World world)
    {
        var document =
            Editor.OpenDocument(
                world);

        SubscribeDocument(
            document);

        MarkUiDirty();

        return document;
    }

    public void Initialize()
    {
        MarkUiDirty();
        RefreshIfNeeded();
    }

    public void Update(
        TimeSnapshot time)
    {
        Input.Update();

        RefreshIfNeeded();

        Ui.Update(
            time.Delta.TotalSeconds);
    }

    public void FixedUpdate(
        SimulationTime time)
    {
    }

    public void Render(
        double interpolationAlpha)
    {
        Ui.Render();
    }

    public void Shutdown()
    {
        MainShell.NewRequested -=
    RequestNewDocument;

        MainShell.OpenRequested -=
            RequestOpenDocument;

        MainShell.SaveAsRequested -=
            RequestSaveDocumentAs;

        MainShell.CloseRequested -=
            RequestCloseDocument;

        _documentCloseDialog.SaveRequested -=
    SavePendingDocument;

        _documentCloseDialog.DiscardRequested -=
            DiscardPendingDocument;

        _documentCloseDialog.Closed -=
            ClearPendingCloseDocument;

        Editor.Session.CloseAll();

        Editor.Session.DocumentClosed -=
            UnsubscribeDocument;

        Editor.Session.ActiveDocumentChanged -=
            OnActiveDocumentChanged;

        Editor.Workspace.Changed -=
    MarkUiDirty;

        UiDocument.Changed -=
            MarkUiDirty;
        _errorDialog.Close();

        if (Editor.AssetBrowser is not null)
        {
            Editor.AssetBrowser.Changed -=
                MarkUiDirty;
        }

        MainShell.SaveRequested -=
            SaveActiveDocument;
    }

    private void RequestOpenDocument()
    {
        if (_documentFiles is null)
        {
            return;
        }

        try
        {
            var path =
                _fileDialogs.ShowOpenFile();

            if (path is null)
            {
                return;
            }

            OpenDocument(
                path);
        }
        catch (Exception exception)
        {
            _errorDialog.ShowError(
                exception.Message);
        }
    }

    private void RequestSaveDocumentAs()
    {
        if (_documentFiles is null)
        {
            return;
        }

        try
        {
            var document =
                Editor.ActiveDocument;

            var initialDirectory =
                document?.FilePath is string filePath
                    ? Path.GetDirectoryName(
                        Path.GetFullPath(
                            filePath))
                    : null;

            var path =
                _fileDialogs.ShowSaveFile(
                    initialDirectory);

            if (path is null)
            {
                return;
            }

            SaveDocumentAs(
                path);
        }
        catch (Exception exception)
        {
            _errorDialog.ShowError(
                exception.Message);
        }
    }


    private void SubscribeDocument(
        EditorDocument document)
    {
        if (!_subscribedDocuments.Add(
                document))
        {
            return;
        }
        document.ValidationChanged +=
    MarkUiDirty;

        document.EntitySelection.Changed +=
            MarkUiDirty;

        document.CommandHistory.Changed +=
            MarkUiDirty;
    }
    private void UnsubscribeDocument(
    EditorDocument document)
    {
        if (!_subscribedDocuments.Remove(
                document))
        {
            return;
        }
        document.ValidationChanged -=
    MarkUiDirty;

        document.EntitySelection.Changed -=
            MarkUiDirty;

        document.CommandHistory.Changed -=
            MarkUiDirty;

        MarkUiDirty();
    }
    private void MarkUiDirty()
    {
        _uiDirty = true;
    }

    public EditorDocument OpenDocument(
    World world,
    string? filePath = null)
    {
        var document =
            Editor.OpenDocument(
                world,
                filePath);

        SubscribeDocument(
            document);

        MarkUiDirty();

        return document;
    }

    public bool RequestApplicationClose()
    {
        var document =
            Editor.Session.Documents
                .FirstOrDefault(
                    static document =>
                        document.IsDirty);

        if (document is null)
        {
            return true;
        }

        _applicationClosePending = true;

        _pendingCloseDocument =
            document;

        _documentCloseDialog.ShowFor(
            document);

        return false;
    }

    private void ContinueApplicationClose()
    {
        if (!_applicationClosePending)
        {
            return;
        }

        var document =
            Editor.Session.Documents
                .FirstOrDefault(
                    static document =>
                        document.IsDirty);

        if (document is not null)
        {
            _pendingCloseDocument =
                document;

            _documentCloseDialog.ShowFor(
                document);

            return;
        }

        _applicationClosePending = false;
        _pendingCloseDocument = null;

        ApplicationCloseRequested?.Invoke();
    }

    private void RefreshIfNeeded()
    {
        if (!_uiDirty)
        {
            return;
        }

        _uiDirty = false;

        MainShell.Refresh();
    }

    private void RequestCloseActiveDocument()
    {
        var document =
            Editor.ActiveDocument;

        if (document is null)
        {
            return;
        }

        if (!document.IsDirty)
        {
            Editor.CloseDocument(
                document,
                EditorDocumentCloseDecision.Discard);

            MarkUiDirty();
            return;
        }

        _pendingCloseDocument =
            document;

        _documentCloseDialog.ShowFor(
            document);
    }

    private void SavePendingDocument()
    {
        var document =
            _pendingCloseDocument;

        if (document is null)
        {
            return;
        }

        try
        {
            if (_documentFiles is null)
            {
                throw new InvalidOperationException(
                    "Document file service is not configured.");
            }

            if (document.FilePath is null)
            {
                _documentCloseDialog.Close();

                SaveDocumentAsRequested?.Invoke();

                return;
            }

            Editor.CloseDocument(
                document,
                EditorDocumentCloseDecision.Save,
                _documentFiles);

            _documentCloseDialog.Close();
            MarkUiDirty();
            ContinueApplicationClose();
        }
        catch (Exception exception)
        {
            _errorDialog.ShowError(
                exception.Message);
        }
    }

    private void DiscardPendingDocument()
    {
        var document =
            _pendingCloseDocument;

        if (document is null)
        {
            return;
        }

        Editor.CloseDocument(
            document,
            EditorDocumentCloseDecision.Discard);

        _documentCloseDialog.Close();
        MarkUiDirty();
        ContinueApplicationClose();
    }
    private void ClearPendingCloseDocument()
    {
        _pendingCloseDocument =
            null;
    }

    public void RequestNewDocument()
    {
        NewDocumentRequested?.Invoke();
    }

    public void RequestCloseDocument()
    {
        RequestCloseActiveDocument();
    }
}