using Engine.Core.Application;
using Engine.Core.Time;
using Engine.Editor.Assets;
using Engine.Editor.Documents;
using Engine.Editor.Documents.Persistence;
using Engine.Editor.Projects;
using Engine.Editor.UI.Authoring;
using Engine.Editor.UI.Commands;
using Engine.Editor.UI.Dialogs;
using Engine.Editor.UI.Inspection;
using Engine.Editor.UI.Shell;
using Engine.Graphics.Resources;
using Engine.Input;
using Engine.Tooling.Debugging;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.Worlds;
using System.Diagnostics;

namespace Engine.Editor.UI;

public sealed class EditorApplication :
    IApplication
{
    private static readonly InputBinding CopyKey =
    new("Keyboard.C");

    private static readonly InputBinding UndoKey =
    new("Keyboard.Z");

    private static readonly InputBinding RedoKey =
        new("Keyboard.Y");

    private static readonly InputBinding LeftControlKey =
        new("Keyboard.ControlLeft");

    private static readonly InputBinding RightControlKey =
        new("Keyboard.ControlRight");
    public InspectorPropertyEditorRegistry PropertyEditors { get; }
    private bool _undoShortcutWasDown;
    private bool _redoShortcutWasDown;
    private readonly ITextInput? _textInput;
    private readonly EditorNewProjectDialog _newProjectDialog;
    private readonly EditorNewDocumentDialog _newDocumentDialog;

    private readonly IClipboardService? _clipboardService;

    private bool _copyShortcutWasDown;

    private readonly HashSet<EditorUiDocument>
        _subscribedUiDocuments = new();

    private readonly HashSet<EditorDocument> _subscribedDocuments = new();
    private bool _uiDirty = true;
    private readonly IEditorDocumentPersistence?
    _documentPersistence;
    private readonly IEditorFileDialogService _fileDialogs;
    public EditorUiDocument? UiDocument { get; }
    private readonly IEditorDocumentFileService?
    _documentFiles;

    private readonly ProjectCreationService _projectCreationService = new();
    private readonly GameProjectRunner _gameProjectRunner = new();
    private readonly DebugConsole? _debugConsole;
    private readonly RebuildEngineCommand? _rebuildEngineCommand;
    private readonly EditorDocumentCloseDialog
    _documentCloseDialog;

    private bool _applicationClosePending;

    public event Action? ApplicationCloseRequested;

    private IEditorDocument? _pendingCloseDocument;
    private bool _closingDocumentDialogAfterDecision;

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
        EditorUiDocument? uiDocument,
        string uiAssetPath,
        IEditorFileDialogService fileDialogs,
        IEditorDocumentPersistence? documentPersistence = null,
        IEditorDocumentFileService? documentFiles = null,
        DebugConsole? debugConsole = null,
        InspectorPropertyEditorRegistry? propertyEditors = null, ITextureManager? filePreviewTextureManager = null, IClipboardService? clipboard = null)
        : this(
            editor,
            ui,
            input,
            assetPreviewTextures,
            fileDialogs,
            documentPersistence,
            documentFiles,
            debugConsole,
            propertyEditors,
            uiDocument,
            uiAssetPath, filePreviewTextureManager, clipboard)
    {
    }

    public EditorApplication(
        EditorContext editor,
        UiSystem ui,
        IInputBackend input,
        ITextureResourceManager assetPreviewTextures,
        IEditorFileDialogService fileDialogs,
        IEditorDocumentPersistence? documentPersistence = null,
        IEditorDocumentFileService? documentFiles = null,
        DebugConsole? debugConsole = null,
        InspectorPropertyEditorRegistry? propertyEditors = null, ITextureManager? filePreviewTextureManager = null, IClipboardService? clipboard = null)
        : this(
            editor,
            ui,
            input,
            assetPreviewTextures,
            fileDialogs,
            documentPersistence,
            documentFiles,
            debugConsole,
            propertyEditors,
            null,
            null, filePreviewTextureManager,clipboard)
    {
    }

    private EditorApplication(
        EditorContext editor,
        UiSystem ui,
        IInputBackend input,
        ITextureResourceManager assetPreviewTextures,
        IEditorFileDialogService fileDialogs,
        IEditorDocumentPersistence? documentPersistence,
        IEditorDocumentFileService? documentFiles,
        DebugConsole? debugConsole,
        InspectorPropertyEditorRegistry? propertyEditors,
        EditorUiDocument? uiDocument,
        string? uiAssetPath, ITextureManager? filePreviewTextureManager = null, IClipboardService? clipboard = null)
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
    fileDialogs);
        _clipboardService = clipboard;
        if (uiDocument is null &&
    uiAssetPath is not null)
        {
            throw new ArgumentException(
                "A UI asset path requires an initial UI document.",
                nameof(uiAssetPath));
        }

        if (uiDocument is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                uiAssetPath);
        }



        Editor = editor;
        Ui = ui;
        Input = input;
        AssetPreviewTextures = assetPreviewTextures;
        UiDocument = uiDocument;

        _textInput =
    input as ITextInput;

        _fileDialogs =
    fileDialogs;
        _debugConsole = debugConsole;
        UiHost =
            new EditorUiHost(
                editor,
                ui);

        if (_debugConsole is not null)
        {
            _rebuildEngineCommand =
                new RebuildEngineCommand(
                    () => Editor.ActiveGameProject);

            _debugConsole.RegisterCommand(
                _rebuildEngineCommand);
        }

        _errorDialog =
    new EditorErrorDialog(
        ui.Overlays,
        ui.Focus);

        _documentCloseDialog =
    new EditorDocumentCloseDialog(
        ui.Overlays,
        ui.Focus);

        _newDocumentDialog =
    new EditorNewDocumentDialog(
        ui.Overlays,
        ui.Focus);

        _newProjectDialog =
    new EditorNewProjectDialog(
        ui.Overlays,
        ui.Focus,
        fileDialogs);

        _newProjectDialog.CreateRequested += CreateProject;

        _newDocumentDialog.WorldRequested +=
            RequestNewWorldDocument;

        _newDocumentDialog.UiRequested +=
            CreateNewUiDocument;

        _documentCloseDialog.SaveRequested +=
            SavePendingDocument;

        _documentCloseDialog.DiscardRequested +=
            DiscardPendingDocument;

        _documentCloseDialog.Closed +=
            ClearPendingCloseDocument;

        if (UiDocument is not null)
        {
            UiDocument.SetFilePath(
                uiAssetPath!);
        }

        _documentPersistence =
    documentPersistence;

        PropertyEditors =
    propertyEditors ??
    InspectorPropertyEditorRegistry.CreateDefault();

        MainShell =
            new EditorMainShell(
                editor,
                assetPreviewTextures,
                uiDocument,
                ui.Overlays,
                debugConsole,
                PropertyEditors,
                filePreviewTextureManager);

        MainShell.AssetActivated += OnAssetActivated;
        MainShell.SaveRequested +=
            SaveActiveDocument;

        MainShell.NewRequested +=
            RequestNewDocument;

        MainShell.NewProjectRequested +=
            RequestNewProject;

        MainShell.OpenProjectRequested +=
            RequestOpenProject;
        MainShell.OpenProjectInIdeRequested +=
            RequestOpenProjectInIde;

        MainShell.PlayRequested += StartGame;
        MainShell.StopRequested += StopGame;
        MainShell.RestartRequested += RestartGame;
        MainShell.HotReloadRequested += StartGameHotReload;

        MainShell.OpenRequested +=
            RequestOpenDocument;

        MainShell.SaveAsRequested +=
            RequestSaveDocumentAs;

        MainShell.CloseRequested +=
            RequestCloseDocument;

        Editor.Session.DocumentClosed +=
            UnsubscribeDocument;

        Editor.Session.AnyDocumentClosed +=
    UnsubscribeAnyDocument;

        UiHost.Root.AddChild(
            MainShell);

        Editor.Session.ActiveEditorDocumentChanged +=
            OnActiveEditorDocumentChanged;
        Editor.ActiveGameProjectChanged +=
    OnActiveGameProjectChanged;
        Editor.Workspace.Changed +=
    MarkUiDirty;

        if (UiDocument is not null)
        {
            SubscribeUiDocument(
                UiDocument);

            if (!Editor.Session.AllDocuments.Contains(
                    UiDocument))
            {
                Editor.Session.Open(
                    UiDocument);
            }
        }

        if (editor.AssetBrowser is not null)
        {
            editor.AssetBrowser.Changed +=
                MarkUiDirty;
        }

        _documentFiles = documentFiles;
    }

    private void OnActiveGameProjectChanged(GameProject? project)
    {
        MarkUiDirty();
    }

    private void OnActiveEditorDocumentChanged(
    IEditorDocument? document)
    {
        MarkUiDirty();
    }

    private IEditorDocument? FindNextDirtyDocument()
    {
        foreach (var document in Editor.Session.AllDocuments)
        {
            if (document.IsDirty)
            {
                return document;
            }
        }

        return null;
    }

    private void CreateProject(
    ProjectCreationRequest request)
    {
        try
        {
            var projectDirectory =
                _projectCreationService.Create(request);

            var manifestPath = Path.Combine(
                projectDirectory,
                $"{Path.GetFileName(projectDirectory)}.vibeproject");

            var project = GameProject.Open(manifestPath);

            ActivateGameProject(project);

            _debugConsole?.WriteLine(
                $"Game project created and activated: {project.Name}");

            _newProjectDialog.CloseAfterCreation();
        }
        catch (Exception exception)
        {
            _debugConsole?.WriteException(
                exception,
                "Failed to create project.");

            _newProjectDialog.SetMessage(
                exception.Message);
        }
    }

    private void RequestNewProject()
    {
        _newProjectDialog.ShowFor();
    }

    public void ToggleConsole()
    {
        var panel = Editor.Workspace.FindPanel("Console");

        if (panel is null)
        {
            return;
        }

        var layout =
            Editor.Workspace.Layout.GetPanel("Console");

        var shouldOpen =
            !panel.IsOpen || !layout.IsActive;

        if (shouldOpen)
        {
            // Сначала открываем панель.
            Editor.Workspace.SetPanelOpen(
                "Console",
                true);

            // Затем делаем её активной вкладкой.
            Editor.Workspace.ActivatePanel("Console");
        }
        else
        {
            Ui.Focus.ClearFocus();

            Editor.Workspace.SetPanelOpen(
                "Console",
                false);
        }

        // Применяем новое состояние workspace.
        MainShell.Refresh();

        if (shouldOpen)
        {
            Ui.Focus.ClearFocus();
            Ui.Focus.SetFocus(MainShell.ConsoleInput);
        }

        MarkUiDirty();
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
        switch (Editor.Session.ActiveEditorDocument)
        {
            case EditorUiDocument uiDocument:
                SaveUiDocument(uiDocument);
                return;

            case EditorDocument worldDocument:
                SaveWorldDocument(worldDocument);
                return;

            case null:
                return;

            default:
                _errorDialog.ShowError(
                    "Saving this document type is not supported.");
                return;
        }
    }

    private void SaveUiDocument(
        EditorUiDocument document)
    {
        try
        {
            TrySaveUiDocument(document);
        }
        catch (Exception exception)
        {
            _errorDialog.ShowError(
                exception.Message);
        }
    }

    private void SaveWorldDocument(
        EditorDocument document)
    {
        try
        {
            var files =
                _documentFiles
                ?? throw new InvalidOperationException(
                    "Document file service is not configured.");

            if (document.FilePath is null)
            {
                SaveWorldDocumentAs(document);
                return;
            }

            files.Save(document);

            MarkUiDirty();
        }
        catch (Exception exception)
        {
            _errorDialog.ShowError(
                exception.Message);
        }
    }

    private void OnActiveDocumentChanged(
    EditorDocument? document)
    {
        MarkUiDirty();
    }

    private void SaveUiDocument()
    {
        var document = ActiveUiDocument;

        if (document is null)
        {
            return;
        }

        TrySaveUiDocument(document);
    }

    private EditorUiDocument? ActiveUiDocument =>
    Editor.Session.ActiveEditorDocument as EditorUiDocument;

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

    public void Update(TimeSnapshot time)
    {
        Input.Update();

        DrainEngineRebuildEvents();
        DrainGameOutput();

        MainShell.SetGameRunning(
            _gameProjectRunner.IsSessionActive);

        if (_textInput?.IsPressed(TextInputKey.F1) == true)
        {
            ToggleConsole();
        }

        HandleKeyboardShortcuts();

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
        if (_rebuildEngineCommand is not null)
        {
            _debugConsole?.UnregisterCommand(
                _rebuildEngineCommand.Name);

            _rebuildEngineCommand.Dispose();
        }

        _gameProjectRunner.Dispose();
        MainShell.ReleaseResources();
        MainShell.PlayRequested -= StartGame;
        MainShell.StopRequested -= StopGame;
        MainShell.RestartRequested -= RestartGame;
        _newProjectDialog.CreateRequested -= CreateProject;
        MainShell.SaveAllRequested -=
    SaveAllDocuments;
        MainShell.HotReloadRequested -= StartGameHotReload;
        MainShell.OpenProjectRequested -=
    RequestOpenProject;
        MainShell.OpenProjectInIdeRequested -=
    RequestOpenProjectInIde;
        MainShell.AssetActivated -= OnAssetActivated;
        _newDocumentDialog.WorldRequested -=
    RequestNewWorldDocument;
        Editor.ActiveGameProjectChanged -=
    OnActiveGameProjectChanged;
        _newDocumentDialog.UiRequested -=
            CreateNewUiDocument;

        Editor.Session.AnyDocumentClosed -=
    UnsubscribeAnyDocument;

        MainShell.NewProjectRequested -=
    RequestNewProject;

        _newProjectDialog.Close();

        MainShell.NewRequested -=
    RequestNewDocument;

        MainShell.OpenRequested -=
            RequestOpenDocument;

        MainShell.SaveAsRequested -=
            RequestSaveDocumentAs;

        MainShell.SaveAllRequested +=
            SaveAllDocuments;

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
            OnActiveEditorDocumentChanged;

        Editor.Workspace.Changed -=
            MarkUiDirty;

        if (UiDocument is not null)
        {
            UiDocument.Changed -=
                MarkUiDirty;
        }

        _errorDialog.Close();

        if (Editor.AssetBrowser is not null)
        {
            Editor.AssetBrowser.Changed -=
                MarkUiDirty;
        }

        MainShell.SaveRequested -=
            SaveActiveDocument;
    }

    private void RequestOpenProjectInIde()
    {
        var project = Editor.ActiveGameProject;

        if (project is null)
        {
            _errorDialog.ShowError(
                "Open a game project first.");

            return;
        }

        // Prefer the solution when available.
        var targetPath =
            project.SolutionFilePath
            ?? project.ProjectFilePath;

        if (!File.Exists(targetPath))
        {
            _errorDialog.ShowError(
                $"Project file was not found: {targetPath}");

            return;
        }

        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = targetPath,
                    WorkingDirectory = project.RootDirectory,
                    UseShellExecute = true
                });

            _debugConsole?.WriteLine(
                $"Opened project in external IDE: {targetPath}");
        }
        catch (Exception exception)
        {
            _debugConsole?.WriteException(
                exception,
                "Failed to open project in an external IDE.");

            _errorDialog.ShowError(
                "Could not open the project in an external IDE.\n" +
                exception.Message);
        }
    }
    private void HandleKeyboardShortcuts()
    {
        var controlDown =
            Input.GetValue(LeftControlKey) > 0.5f ||
            Input.GetValue(RightControlKey) > 0.5f;

        var copyDown =
    controlDown &&
    Input.GetValue(CopyKey) > 0.5f;
        
        var undoDown =
            controlDown &&
            Input.GetValue(UndoKey) > 0.5f;

        var redoDown =
            controlDown &&
            Input.GetValue(RedoKey) > 0.5f;

        // Don't trigger document commands while a modal,
        // context menu or text box has keyboard focus.
        var shortcutSuppressed =
            Ui.Focus.ActiveScope is not null ||
            Ui.Focus.FocusedWidget is UiTextBox;

        var consoleOutput = MainShell.ConsoleOutputWidget;
        var focusedWidget = Ui.Focus.FocusedWidget;

        var canCopyConsoleSelection =
            Ui.Focus.ActiveScope is null &&
            focusedWidget is not UiTextBox &&
            (ReferenceEquals(focusedWidget, consoleOutput) ||
             consoleOutput.IsHovered);

        if (copyDown &&
            !_copyShortcutWasDown &&
            canCopyConsoleSelection)
        {
            var selectedText = MainShell.ConsoleSelectedText;

            if (selectedText.Length > 0)
            {
                if (_clipboardService is null)
                {
                    _debugConsole?.WriteError(
                        "Clipboard service is not configured.");
                }
                else
                {
                    try
                    {
                        _clipboardService.SetText(selectedText);
                    }
                    catch (Exception exception)
                    {
                        _debugConsole?.WriteException(
                            exception,
                            "Failed to copy console selection.");
                    }
                }
            }
        }

        if (!shortcutSuppressed)
        {
            if (undoDown && !_undoShortcutWasDown)
            {
                MainShell.Undo();
            }

            if (redoDown && !_redoShortcutWasDown)
            {
                MainShell.Redo();
            }

            if (copyDown &&
    !_copyShortcutWasDown &&
    ReferenceEquals(
        Ui.Focus.FocusedWidget,
        MainShell.ConsoleOutputWidget))
            {
                var selectedText = MainShell.ConsoleSelectedText;

                if (selectedText.Length > 0 &&
                    _clipboardService is not null)
                {
                    try
                    {
                        _clipboardService.SetText(selectedText);
                    }
                    catch (Exception exception)
                    {
                        _debugConsole?.WriteException(
                            exception,
                            "Failed to copy console selection.");
                    }
                }
            }
        }

        // Trigger only once per key press, not every frame
        // while the shortcut remains held.
        _undoShortcutWasDown = undoDown;
        _redoShortcutWasDown = redoDown;
        _copyShortcutWasDown = copyDown;
    }

    private void SaveAllDocuments()
    {
        try
        {
            var documents =
                Editor.Session.AllDocuments.ToArray();

            foreach (var document in documents)
            {
                // Clean documents do not need to be saved.
                if (!document.IsDirty)
                {
                    continue;
                }

                switch (document)
                {
                    case EditorUiDocument uiDocument:
                        {
                            if (!TrySaveUiDocument(uiDocument))
                            {
                                // Save As was cancelled.
                                MarkUiDirty();
                                return;
                            }

                            break;
                        }

                    case EditorDocument worldDocument:
                        {
                            var files =
                                _documentFiles
                                ?? throw new InvalidOperationException(
                                    "Document file service is not configured.");

                            if (worldDocument.FilePath is null)
                            {
                                var path =
                                    _fileDialogs.ShowSaveFile();

                                if (path is null)
                                {
                                    // Save As was cancelled.
                                    MarkUiDirty();
                                    return;
                                }

                                files.SaveAs(
                                    worldDocument,
                                    path);
                            }
                            else
                            {
                                files.Save(
                                    worldDocument);
                            }

                            break;
                        }

                    default:
                        throw new NotSupportedException(
                            $"Saving document type " +
                            $"'{document.DocumentTypeId}' is not supported.");
                }
            }

            MarkUiDirty();
        }
        catch (Exception exception)
        {
            // Some earlier documents may already have been saved.
            MarkUiDirty();

            _errorDialog.ShowError(
                exception.Message);
        }
    }

    private void RequestOpenDocument()
    {
        try
        {
            var activeDocument =
                Editor.Session.ActiveEditorDocument;

            var initialDirectory =
                activeDocument?.FilePath is string activePath
                    ? Path.GetDirectoryName(
                        Path.GetFullPath(activePath))
                    : null;

            var path =
                _fileDialogs.ShowOpenFile(initialDirectory);

            if (path is null)
            {
                return;
            }

            OpenDocumentPath(path);
        }
        catch (Exception exception)
        {
            _errorDialog.ShowError(exception.Message);
        }
    }

    private void OpenDocumentPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalizedPath = Path.GetFullPath(path);

        var pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var existingDocument =
            Editor.Session.AllDocuments.FirstOrDefault(
                document =>
                    document.FilePath is string documentPath &&
                    string.Equals(
                        Path.GetFullPath(documentPath),
                        normalizedPath,
                        pathComparison));

        if (existingDocument is not null)
        {
            Editor.Session.Activate(existingDocument);
            MarkUiDirty();
            return;
        }

        switch (Path.GetExtension(normalizedPath).ToLowerInvariant())
        {
            case ".world":
                OpenDocument(normalizedPath);
                return;

            case ".ui":
                var document = EditorUiAssetFile.Load(normalizedPath);

                document.SetFilePath(normalizedPath);

                SubscribeUiDocument(document);

                Editor.Session.Open(document);

                MarkUiDirty();
                return;

            default:
                throw new NotSupportedException(
                    $"Unsupported document type: " +
                    $"'{Path.GetExtension(normalizedPath)}'. " +
                    "Supported extensions are .world and .ui.");
        }
    }

    public void SetActiveWorldAsStartupScene()
    {
        if (Editor.Session.ActiveEditorDocument
            is not EditorDocument document)
        {
            _errorDialog.ShowError(
                "Select a world document first.");

            return;
        }

        try
        {
            Editor.SetStartupScene(document);

            var project = Editor.ActiveGameProject!;

            _debugConsole?.WriteLine(
                $"Startup scene set to '{project.StartupSceneRelativePath}'.");

            MarkUiDirty();
        }
        catch (Exception exception)
        {
            _debugConsole?.WriteException(
                exception,
                "Failed to set the startup scene.");

            _errorDialog.ShowError(exception.Message);
        }
    }


    private void StartGame() =>
        StartGame(GameProjectRunMode.Standard);

    private void StartGameHotReload() =>
        StartGame(GameProjectRunMode.HotReload);

    private void StartGame(GameProjectRunMode mode)
    {
        var project = Editor.ActiveGameProject;

        if (project is null)
        {
            _debugConsole?.WriteError(
                "Open a game project first.");

            return;
        }

        try
        {
            _gameProjectRunner.Start(project, mode);

            _debugConsole?.WriteLine(
                mode == GameProjectRunMode.HotReload
                    ? "Starting game with Hot Reload."
                    : "Starting game normally.");
        }
        catch (Exception exception)
        {
            _debugConsole?.WriteException(
                exception,
                "Failed to start game.");

            _errorDialog.ShowError(
                "Failed to start game.\n" +
                exception.Message);
        }

        MainShell.SetGameRunning(
            _gameProjectRunner.IsSessionActive);
    }


    private void StopGame()
    {
        try
        {
            _gameProjectRunner.Stop();
        }
        catch (Exception exception)
        {
            _debugConsole?.WriteException(
                exception,
                "Failed to stop game.");

            _errorDialog.ShowError(
                "Failed to stop game.\n" +
                exception.Message);
        }
        finally
        {
            MainShell.SetGameRunning(
                _gameProjectRunner.IsRunning);
        }
    }

    private void RestartGame()
    {
        var project = Editor.ActiveGameProject;

        if (project is null)
        {
            _debugConsole?.WriteError(
                "Open a game project first.");

            return;
        }
        var mode =
    _gameProjectRunner.ActiveRunMode
    ?? GameProjectRunMode.Standard;
        try
        {
            if (_gameProjectRunner.IsRunning)
            {
                _gameProjectRunner.Stop();
            }

            _gameProjectRunner.Start(project, mode);
        }
        catch (Exception exception)
        {
            _debugConsole?.WriteException(
                exception,
                "Failed to restart game.");

            _errorDialog.ShowError(
                "Failed to restart game.\n" +
                exception.Message);
        }

        MainShell.SetGameRunning(
            _gameProjectRunner.IsRunning);
    }

    private void DrainGameOutput()
    {
        while (_gameProjectRunner.TryDequeueOutput(
            out var output))
        {
            if (output is null)
            {
                continue;
            }

            if (output.IsError)
            {
                _debugConsole?.WriteError(
                    output.Message);
            }
            else
            {
                _debugConsole?.WriteLine(
                    output.Message);
            }
        }
        if (_gameProjectRunner.TryConsumeHotReloadIdleSignal())
        {
            _gameProjectRunner.Stop();

            _debugConsole?.WriteLine(
                "Hot Reload session stopped because the game exited.");
        }
    }

    private void OnAssetActivated(EditorAssetEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.IsDirectory)
        {
            return;
        }

        try
        {
            switch (Path.GetExtension(entry.Path).ToLowerInvariant())
            {
                case ".ui":
                case ".world":
                    OpenDocumentPath(entry.Path);
                    return;

                default:
                    if (Editor.Workspace.FindPanel("AssetPreview") is not null)
                    {
                        Editor.Workspace.ActivatePanel("AssetPreview");
                    }

                    MarkUiDirty();
                    return;
            }
        }
        catch (Exception exception)
        {
            _errorDialog.ShowError(exception.Message);
        }
    }

    private void RequestSaveDocumentAs()
    {
        switch (Editor.Session.ActiveEditorDocument)
        {
            case EditorUiDocument uiDocument:
                SaveUiDocumentAs(uiDocument);
                return;

            case EditorDocument worldDocument:
                SaveWorldDocumentAs(worldDocument);
                return;

            case null:
                return;

            default:
                _errorDialog.ShowError(
                    "Saving this document type is not supported.");
                return;
        }
    }

    private void SaveUiDocumentAs(
        EditorUiDocument document)
    {
        try
        {
            var initialDirectory =
                document.FilePath is string filePath
                    ? Path.GetDirectoryName(
                        Path.GetFullPath(filePath))
                    : null;

            var path =
                _fileDialogs.ShowSaveUiFile(
                    initialDirectory);

            if (path is null)
            {
                return;
            }

            TrySaveUiDocument(
                document,
                path);
        }
        catch (Exception exception)
        {
            _errorDialog.ShowError(
                exception.Message);
        }
    }

    private void SaveWorldDocumentAs(
        EditorDocument document)
    {
        try
        {
            var files =
                _documentFiles
                ?? throw new InvalidOperationException(
                    "Document file service is not configured.");

            var initialDirectory =
                document.FilePath is string filePath
                    ? Path.GetDirectoryName(
                        Path.GetFullPath(filePath))
                    : null;

            var path =
                _fileDialogs.ShowSaveFile(
                    initialDirectory);

            if (path is null)
            {
                return;
            }

            files.SaveAs(
                document,
                path);

            MarkUiDirty();
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
        var document = FindNextDirtyDocument();

        if (document is null)
        {
            return true;
        }

        _applicationClosePending = true;
        _pendingCloseDocument = document;

        _documentCloseDialog.ShowFor(document);

        return false;
    }

    private void ContinueApplicationClose()
    {
        if (!_applicationClosePending)
        {
            return;
        }

        var document = FindNextDirtyDocument();

        if (document is not null)
        {
            _pendingCloseDocument = document;

            _documentCloseDialog.ShowFor(document);

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
            Editor.Session.ActiveEditorDocument;

        if (document is null)
        {
            return;
        }

        if (!document.IsDirty)
        {
            Editor.Session.Discard(document);
            MarkUiDirty();
            return;
        }

        _pendingCloseDocument = document;

        _documentCloseDialog.ShowFor(document);
    }


    private void SavePendingDocument()
    {
        var document = _pendingCloseDocument;

        if (document is null)
        {
            return;
        }

        var continueApplicationClose =
            _applicationClosePending;

        try
        {
            switch (document)
            {
                case EditorDocument worldDocument:
                    {
                        var files = _documentFiles
                            ?? throw new InvalidOperationException(
                                "Document file service is not configured.");

                        if (worldDocument.FilePath is null)
                        {
                            var path = _fileDialogs.ShowSaveFile();

                            if (path is null)
                            {
                                return;
                            }

                            files.SaveAs(worldDocument, path);
                        }

                        Editor.CloseDocument(
                            worldDocument,
                            EditorDocumentCloseDecision.Save,
                            files);

                        break;
                    }

                case EditorUiDocument uiDocument:
                    {
                        if (!TrySaveUiDocument(uiDocument))
                        {
                            return;
                        }

                        if (!continueApplicationClose)
                        {
                            Editor.Session.Close(uiDocument);
                        }

                        break;
                    }

                default:
                    throw new NotSupportedException(
                        $"Unsupported document type: {document.GetType().FullName}");
            }

            CloseDocumentDialogAfterDecision();

            MarkUiDirty();

            if (continueApplicationClose)
            {
                ContinueApplicationClose();
            }
        }
        catch (Exception exception)
        {
            _errorDialog.ShowError(exception.Message);
        }
    }

    private void RequestOpenProject()
    {
        try
        {
            var selectedPath =
                _fileDialogs.ShowOpenProject(
                    Editor.ActiveGameProject?.RootDirectory);

            if (selectedPath is null)
            {
                return;
            }

            var project =
                GameProject.Open(selectedPath);

            ActivateGameProject(project);

            _debugConsole?.WriteLine(
                $"Game project opened: {project.Name}");
        }
        catch (Exception exception)
        {
            _debugConsole?.WriteException(
                exception,
                "Failed to open game project.");

            _errorDialog.ShowError(
                exception.Message);
        }
    }

    private void ActivateGameProject(GameProject project)
    {
        Directory.CreateDirectory(
            project.AssetsDirectoryPath);

        Editor.SetActiveGameProject(project);
    }

    private void DrainEngineRebuildEvents()
    {
        var command = _rebuildEngineCommand;

        if (command is null || _debugConsole is null)
        {
            return;
        }

        while (command.TryDequeueEvent(out var item))
        {
            if (item is null)
            {
                continue;
            }

            if (item.PreparedUpdate is null)
            {
                _debugConsole.Write(
                    item.Level,
                    item.Message);

                if (item.IsTerminal)
                {
                    command.Complete();
                }

                continue;
            }

            var update = item.PreparedUpdate;

            var activeProject = Editor.ActiveGameProject;

            if (activeProject is null ||
                !SameProjectRoot(
                    activeProject.RootDirectory,
                    update.ProjectRoot))
            {
                _debugConsole.Write(
                    Engine.Core.Diagnostics.DiagnosticLevel.Warning,
                    "The active project changed during the rebuild. " +
                    "The prepared update was discarded.");

                update.Dispose();
                command.Complete();
                continue;
            }

            var wasRunning = _gameProjectRunner.IsSessionActive;

            var runMode =
                _gameProjectRunner.ActiveRunMode
                ?? GameProjectRunMode.Standard;

            var gameStopped = !wasRunning;

            try
            {
                if (wasRunning)
                {
                    try
                    {
                        _gameProjectRunner.Stop();
                    }
                    finally
                    {
                        gameStopped =
                            !_gameProjectRunner.IsSessionActive;
                    }

                    if (!gameStopped)
                    {
                        throw new InvalidOperationException(
                            "The game process could not be stopped.");
                    }
                }

                update.Apply();

                _debugConsole.WriteLine(
                    $"Engine DLLs updated for '{activeProject.Name}'.");
            }
            catch (Exception exception)
            {
                _debugConsole.WriteException(
                    exception,
                    "Failed to apply the engine update.");
            }
            finally
            {
                if (wasRunning &&
                    gameStopped &&
                    update.CanRestartGame &&
                    SameProjectRoot(
                        Editor.ActiveGameProject?.RootDirectory,
                        update.ProjectRoot))
                {
                    // Если обновление откатилось успешно, можно безопасно
                    // запустить прежнюю версию игры.
                    StartGame(runMode);
                }

                update.Dispose();
                command.Complete();

                MainShell.SetGameRunning(
                    _gameProjectRunner.IsSessionActive);
            }
        }
    }

    private static bool SameProjectRoot(
        string? left,
        string? right)
    {
        if (left is null || right is null)
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

    private void CloseDocumentDialogAfterDecision()
    {
        _closingDocumentDialogAfterDecision = true;

        try
        {
            _documentCloseDialog.Close();
        }
        finally
        {
            _closingDocumentDialogAfterDecision = false;
        }
    }

    private void DiscardPendingDocument()
    {
        var document = _pendingCloseDocument;

        if (document is null)
        {
            return;
        }

        var continueApplicationClose =
            _applicationClosePending;

        try
        {
            switch (document)
            {
                case EditorDocument worldDocument:
                    Editor.CloseDocument(
                        worldDocument,
                        EditorDocumentCloseDecision.Discard);
                    break;

                case EditorUiDocument uiDocument:
                    if (continueApplicationClose)
                    {
                        // Keep the document open for application shutdown.
                        uiDocument.MarkSaved();
                    }
                    else
                    {
                        // Close only the active document; discard its changes.
                        Editor.Session.Discard(uiDocument);
                    }

                    break;

                default:
                    throw new NotSupportedException(
                        $"Unsupported document type: {document.GetType().FullName}");
            }

            CloseDocumentDialogAfterDecision();

            MarkUiDirty();

            if (continueApplicationClose)
            {
                ContinueApplicationClose();
            }
        }
        catch (Exception exception)
        {
            _errorDialog.ShowError(exception.Message);
        }
    }
    private void ClearPendingCloseDocument()
    {
        _pendingCloseDocument = null;

        if (!_closingDocumentDialogAfterDecision)
        {
            // Closing the dialog through Cancel or Escape cancels
            // the pending application shutdown.
            _applicationClosePending = false;
        }
    }

    private void RequestNewWorldDocument()
    {
        NewDocumentRequested?.Invoke();
    }

    private void CreateNewUiDocument()
    {
        const string baseName = "UI Document";

        var existingNames =
            Editor.Session.AllDocuments
                .OfType<EditorUiDocument>()
                .Select(static document => document.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var name = baseName;

        for (var suffix = 2;
             existingNames.Contains(name);
             suffix++)
        {
            name = $"{baseName} {suffix}";
        }

        var document =
            new EditorUiDocument(name);

        SubscribeUiDocument(document);

        Editor.Session.Open(document);

        MarkUiDirty();
    }

    private void SubscribeUiDocument(
        EditorUiDocument document)
    {
        if (!_subscribedUiDocuments.Add(document))
        {
            return;
        }

        document.Changed += MarkUiDirty;
    }

    private void UnsubscribeAnyDocument(
        IEditorDocument document)
    {
        if (document is not EditorUiDocument uiDocument)
        {
            return;
        }

        if (!_subscribedUiDocuments.Remove(uiDocument))
        {
            return;
        }

        uiDocument.Changed -= MarkUiDirty;

        MarkUiDirty();
    }

    public void RequestNewDocument()
    {
        _newDocumentDialog.ShowFor();
    }

    public void RequestCloseDocument()
    {
        RequestCloseActiveDocument();
    }

    private bool TrySaveUiDocument(
    EditorUiDocument document,
    string? destinationPath = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        var path = destinationPath ?? document.FilePath;

        if (path is null)
        {
            path = _fileDialogs.ShowSaveUiFile();

            if (path is null)
            {
                return false;
            }
        }

        var normalizedPath = Path.GetFullPath(path);

        EditorUiAssetFile.Save(
            document,
            normalizedPath);

        document.SetFilePath(normalizedPath);
        document.MarkSaved();

        MarkUiDirty();

        return true;
    }
}