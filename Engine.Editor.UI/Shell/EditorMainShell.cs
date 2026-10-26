using Engine.Core.Diagnostics;
using Engine.Core.Math;
using Engine.Editor.Assets;
using Engine.Editor.Documents;
using Engine.Editor.UI.Authoring;
using Engine.Editor.UI.Dialogs;
using Engine.Editor.UI.Inspection;
using Engine.Editor.UI.Workspace;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.Tooling.Debugging;
using Engine.Tooling.Validation;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Shell;

public sealed class EditorMainShell :
    UiPanel
{
    private readonly UiButton _validateButton;
    private readonly UiButton _playButton;
    private readonly UiButton _stopButton;
    private readonly UiButton _restartButton;
    private readonly UiButton _hotReloadButton;
    private readonly UiButton _closeButton;
    private readonly UiButton _toolsButton;
    private readonly EditorAssetPickerDialog? _assetPickerDialog;
    private readonly UiPanel _menuBar;
    private readonly UiPanel _toolbar;
    private bool _isGameRunning;
    private readonly DebugConsole? _debugConsole;
    private readonly UiPanel _workspace;
    private readonly UiLabel _documentLabel;
    public event Action<EditorAssetEntry>? AssetActivated;
    private readonly EditorWorkspaceView _workspaceView;
    private readonly UiOverlayLayer _overlays;
    private readonly ITextureResourceManager _assetPreviewTextures;
    private readonly ITextureManager? _filePreviewTextureManager;
    private EditorUiWorkspaceView _uiWorkspaceView;
    private ParticleEffectEditorPanelView? _particleEffectWorkspaceView;
    public event Action? NewProjectRequested;
    public event Action? OpenProjectRequested;
    public event Action? OpenProjectInIdeRequested;
    public event Action? PlayRequested;
    public event Action? StopRequested;
    public event Action? RestartRequested;
    public event Action? HotReloadRequested;

    public UiWidget ConsoleOutputWidget =>
    _workspaceView.Console.ConsoleOutputWidget;

    public string ConsoleSelectedText =>
        _workspaceView.Console.SelectedText;

    public bool IsUiMode =>
        Editor.Session.ActiveEditorDocument
            is EditorUiDocument;

    public bool CanUndo =>
    Editor.Session.ActiveEditorDocument switch
    {
        EditorUiDocument document =>
            document.CommandHistory.CanUndo,

        EditorDocument document =>
            document.CommandHistory.CanUndo,

        _ => false
    };

    public bool CanRedo =>
        Editor.Session.ActiveEditorDocument switch
        {
            EditorUiDocument document =>
                document.CommandHistory.CanRedo,

            EditorDocument document =>
                document.CommandHistory.CanRedo,

            _ => false
        };

    public event Action? OpenRequested;
    public event Action? SaveRequested;
    public event Action? SaveAllRequested;
    public event Action? SaveAsRequested;
    public event Action? CloseRequested;
    public event Action<UiRect>? ToolsRequested;
    public event Action<UiRect>? ViewRequested;
    public event Action<UiRect>? EditRequested;
    public event Action<string?>? AssetCreationRequested;
    private readonly UiScrollView _documentTabs;

    public EditorMainShell(
        EditorContext editor,
        ITextureResourceManager assetPreviewTextures,
        EditorUiDocument? uiDocument, 
        UiOverlayLayer overlays, 
        DebugConsole? console = null, 
        InspectorPropertyEditorRegistry? propertyEditors = null, 
        ITextureManager? filePreviewTextureManager = null, UiFocusManager? focus = null)
    {
        ArgumentNullException.ThrowIfNull(
            editor);

        ArgumentNullException.ThrowIfNull(
            assetPreviewTextures);
        _debugConsole = console;
        Editor =
            editor;
        _overlays = overlays;
        _assetPreviewTextures = assetPreviewTextures;
        _filePreviewTextureManager = filePreviewTextureManager;
        Background =
            new UiColor(
                24,
                24,
                24,
                255);

        HorizontalAlignment =
            UiHorizontalAlignment.Stretch;

        VerticalAlignment =
            UiVerticalAlignment.Stretch;

        var menuBar =
            CreateMenuBar();

        _menuBar = menuBar.Panel;
        _toolsButton = menuBar.ToolsButton;

        var toolbar =
            CreateToolbar();

        _toolbar = toolbar.Panel;
        _validateButton = toolbar.ValidateButton;
        _playButton = toolbar.PlayButton;
        _stopButton = toolbar.StopButton;
        _restartButton = toolbar.RestartButton;
        _hotReloadButton = toolbar.HotReloadButton;
        _closeButton = toolbar.CloseButton;

        _documentTabs = new UiScrollView
        {
            Background = new UiColor(28, 28, 28, 255),

            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top,

            HorizontalScrolling = true,
            VerticalScrolling = false,

            ShowHorizontalScrollBar = true,
            ShowVerticalScrollBar = false,

            ScrollBarThickness = 10.0f,
            ScrollSpeed = 48.0f
        };

        _workspace =
            new UiPanel
            {
                Background =
                    new UiColor(
                        18,
                        18,
                        18,
                        255),

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Stretch
            };
        _assetPickerDialog =
            focus is null
                ? null
                : new EditorAssetPickerDialog(
                    editor,
                    overlays,
                    focus);

        _workspaceView = new EditorWorkspaceView(
            editor,
            overlays,
            assetPreviewTextures,
            console,
            propertyEditors,
            filePreviewTextureManager,
            _assetPickerDialog);

        _workspaceView.AssetActivated +=
    entry => AssetActivated?.Invoke(entry);
        _workspaceView.AssetCreationRequested +=
    directory => AssetCreationRequested?.Invoke(directory);
        _uiWorkspaceView =
            CreateUiWorkspaceView(
                uiDocument ??
                new EditorUiDocument(
                    "UI Workspace"));

        _workspace.AddChild(
            _workspaceView);

        _workspace.AddChild(
            _uiWorkspaceView);

        _uiWorkspaceView.Visible = false;

        _documentLabel =
            new UiLabel(
                "No document")
            {
                FontSize = 14.0f,

                Color =
                    new UiColor(
                        180,
                        180,
                        180,
                        255),

                HorizontalAlignment =
                    UiHorizontalAlignment.Right,

                VerticalAlignment =
                    UiVerticalAlignment.Center
            };

        AddChild(
            _menuBar);
        AddChild(_toolbar);
        AddChild(_documentTabs);
        AddChild(
            _workspace);

        AddChild(
            _documentLabel);
    }
    public EditorContext Editor { get; }

    public EditorUiDocument UiDocument =>
        _uiWorkspaceView.Document;

    public UiTextBox ConsoleInput =>
    _workspaceView.Console.CommandInput;

    public UiPanel Workspace =>
        _workspace;

    private EditorUiWorkspaceView CreateUiWorkspaceView(
        EditorUiDocument document)
    {
        var view =
            new EditorUiWorkspaceView(
                document,
                Editor,
                _overlays,
                _assetPreviewTextures,
                _filePreviewTextureManager);

        view.AssetActivated +=
            entry => AssetActivated?.Invoke(entry);

        return view;
    }

    protected override Vector2 MeasureCore(
    UiLayoutContext context,
    Vector2 availableSize)
    {
        const float menuHeight = 32.0f;
        const float toolbarHeight = 36.0f;
        const float documentTabsHeight = 38.0f;
        const float documentHeight = 24.0f;

        _menuBar.Measure(
            context,
            new Vector2(
                availableSize.X,
                menuHeight));

        _toolbar.Measure(
    context,
    new Vector2(
        availableSize.X,
        toolbarHeight));

        _documentTabs.Measure(
            context,
            new Vector2(
                availableSize.X,
                documentTabsHeight));

        var workspaceHeight = MathF.Max(
            0.0f,
            availableSize.Y
                - menuHeight
                - toolbarHeight
                - documentTabsHeight
                - documentHeight);

        _workspace.Measure(
            context,
            new Vector2(
                availableSize.X,
                workspaceHeight));

        _documentLabel.Measure(
            context,
            new Vector2(
                availableSize.X,
                documentHeight));

        return availableSize;
    }

    protected override void ArrangeCore(
    UiRect finalRect)
    {
        const float menuHeight = 32.0f;
        const float toolbarHeight = 36.0f;
        const float documentTabsHeight = 38.0f;
        const float documentHeight = 24.0f;

        _menuBar.Arrange(
            new UiRect(
                finalRect.X,
                finalRect.Y,
                finalRect.Width,
                menuHeight));

        _toolbar.Arrange(
    new UiRect(
        finalRect.X,
        finalRect.Y + menuHeight,
        finalRect.Width,
        toolbarHeight));

        _documentTabs.Arrange(
            new UiRect(
                finalRect.X,
                finalRect.Y + menuHeight + toolbarHeight,
                finalRect.Width,
                documentTabsHeight));

        _workspace.Arrange(
            new UiRect(
                finalRect.X,
                finalRect.Y + menuHeight
                    + toolbarHeight
                    + documentTabsHeight,
                finalRect.Width,
                MathF.Max(
                    0.0f,
                    finalRect.Height
                        - menuHeight
                        - toolbarHeight
                        - documentTabsHeight
                        - documentHeight)));

        _documentLabel.Arrange(
            new UiRect(
                finalRect.X,
                finalRect.Bottom -
                documentHeight,
                finalRect.Width,
                documentHeight));
    }

    private void BindParticleEffectDocument(
    ParticleEffectDocument? document)
    {
        if (ReferenceEquals(
                _particleEffectWorkspaceView?.Document,
                document))
        {
            return;
        }

        if (_particleEffectWorkspaceView is not null)
        {
            _particleEffectWorkspaceView.ReleaseResources();

            _workspace.RemoveChild(
                _particleEffectWorkspaceView);

            _particleEffectWorkspaceView = null;
        }

        if (document is null)
        {
            return;
        }

        _particleEffectWorkspaceView =
            new ParticleEffectEditorPanelView(
                document,
                Editor,
                _filePreviewTextureManager,
                _assetPickerDialog);

        _workspace.AddChild(
            _particleEffectWorkspaceView);
    }

    private void BindUiDocument(
    EditorUiDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (ReferenceEquals(
                _uiWorkspaceView.Document,
                document))
        {
            return;
        }

        _uiWorkspaceView.ReleaseResources();

        _workspace.RemoveChild(
            _uiWorkspaceView);

        _uiWorkspaceView =
            CreateUiWorkspaceView(document);

        _workspace.AddChild(
            _uiWorkspaceView);
    }

    public void ReleaseResources()
    {
        _workspaceView.ReleaseResources();
        _uiWorkspaceView.ReleaseResources();
        BindParticleEffectDocument(null);
    }

    public void Refresh()
    {
        var activeDocument =
            Editor.Session.ActiveEditorDocument;

        var worldDocument =
            activeDocument as EditorDocument;

        var uiDocument =
            activeDocument as EditorUiDocument;

        var particleEffectDocument =
            activeDocument as ParticleEffectDocument;

        BindParticleEffectDocument(particleEffectDocument);

        RefreshDocumentTabs();

        if (activeDocument is EditorUiDocument activeUiDocument)
        {
            BindUiDocument(activeUiDocument);
        }

        _workspaceView.Visible =
            worldDocument is not null ||
            activeDocument is null;

        _uiWorkspaceView.Visible =
            uiDocument is not null;

        if (_particleEffectWorkspaceView is not null)
        {
            _particleEffectWorkspaceView.Visible =
                particleEffectDocument is not null;
        }

        _validateButton.Enabled =
            worldDocument is not null;

        _closeButton.Enabled =
            activeDocument is not null;

        string documentStatus;

        if (worldDocument is not null)
        {
            documentStatus = BuildDocumentStatus(
                worldDocument,
                worldDocument.IsDirty
                    ? "Unsaved changes"
                    : "Saved");
        }
        else if (uiDocument is not null)
        {
            documentStatus =
                $"UI • {uiDocument.DisplayName} • " +
                (uiDocument.IsDirty
                    ? "Unsaved changes"
                    : "Saved");
        }
        else if (particleEffectDocument is not null)
        {
            documentStatus =
                $"Particle Effect • {particleEffectDocument.DisplayName} • " +
                (particleEffectDocument.IsDirty
                    ? "Unsaved changes"
                    : "Saved");
        }
        else
        {
            documentStatus = "No document";
        }

        var projectStatus =
            Editor.ActiveGameProject is { } project
                ? $"Project: {project.Name}"
                : "No project";

        _documentLabel.Text =
            $"{projectStatus} • {documentStatus}";

        UpdateGameControls();

        _workspaceView.Refresh();
        _uiWorkspaceView.Refresh();
    }

    private static string BuildDocumentStatus(
    EditorDocument document,
    string saveState)
    {
        var result =
            document.LastValidationResult;

        if (result is null)
        {
            return
                $"World • {saveState} • Validation: Not run";
        }

        var errors =
            result.Issues.Count(
                static issue =>
                    issue.Severity ==
                    ValidationSeverity.Error);

        var warnings =
            result.Issues.Count(
                static issue =>
                    issue.Severity ==
                    ValidationSeverity.Warning);

        if (errors > 0)
        {
            return
                $"World • {saveState} • Validation: {errors} error(s)";
        }

        if (warnings > 0)
        {
            return
                $"World • {saveState} • Validation: {warnings} warning(s)";
        }

        return
            $"World • {saveState} • Validation: OK";
    }

    private void RefreshDocumentTabs()
    {
        _documentTabs.ClearChildren();

        var documents =
            Editor.Session.AllDocuments;

        if (documents.Count == 0)
        {
            return;
        }

        var activeDocument =
            Editor.Session.ActiveEditorDocument;

        var tabs =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Horizontal,

                Spacing = 2.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Left,

                VerticalAlignment =
                    UiVerticalAlignment.Stretch
            };

        foreach (var document in documents)
        {
            var name =
                string.IsNullOrWhiteSpace(
                    document.FilePath)
                    ? document.DisplayName
                    : Path.GetFileName(
                        document.FilePath);

            if (string.IsNullOrWhiteSpace(name))
            {
                name = document.DocumentTypeId;
            }

            var isActive =
                ReferenceEquals(
                    document,
                    activeDocument);

            var prefix =
                isActive
                    ? "> "
                    : "  ";

            var dirty =
                document.IsDirty
                    ? " *"
                    : string.Empty;

            var button =
                new UiButton(
                    prefix +
                    name +
                    dirty)
                {
                    Width = 180.0f,
                    Height = 28.0f
                };

            button.Clicked +=
                () =>
                {
                    Editor.Session.Activate(
                        document);

                    Refresh();
                };

            tabs.AddChild(
                button);
        }

        _documentTabs.SetContent(tabs);
    }

    private void ShowWorld()
    {
        if (Editor.Session.ActiveEditorDocument
            is not EditorDocument)
        {
            var document =
                Editor.Session.Documents.LastOrDefault();

            if (document is not null)
            {
                Editor.Session.Activate(document);
            }
        }

        Refresh();
    }

    private void ShowUi()
    {
        if (Editor.Session.AllDocuments.Contains(UiDocument))
        {
            Editor.Session.Activate(UiDocument);
        }

        Refresh();
    }

    public void Undo()
    {
        switch (Editor.Session.ActiveEditorDocument)
        {
            case EditorUiDocument uiDocument:
                uiDocument.Undo();
                break;

            case EditorDocument:
                Editor.Actions.Execute(
                    "edit.undo",
                    Editor.ActionContext);
                break;
        }

        Refresh();
    }

    public void Redo()
    {
        switch (Editor.Session.ActiveEditorDocument)
        {
            case EditorUiDocument uiDocument:
                uiDocument.Redo();
                break;

            case EditorDocument:
                Editor.Actions.Execute(
                    "edit.redo",
                    Editor.ActionContext);
                break;
        }

        Refresh();
    }
    private (UiPanel Panel, UiButton ToolsButton) CreateMenuBar()
    {
        var panel = new UiPanel
        {
            Background = new UiColor(32, 32, 32, 255),
            Padding = new UiThickness(8, 2, 8, 2),
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        var buttons = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 4.0f,
            HorizontalAlignment = UiHorizontalAlignment.Left,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        UiButton AddMenuButton(
            string text,
            float width,
            Action<UiRect> requested)
        {
            var button = new UiButton(text)
            {
                Width = width,
                Height = 28.0f
            };

            button.Clicked += () =>
                requested(button.Bounds);

            buttons.AddChild(button);

            return button;
        }

        AddMenuButton(
            "Edit",
            64.0f,
            bounds => EditRequested?.Invoke(bounds));

        AddMenuButton(
            "View",
            64.0f,
            bounds => ViewRequested?.Invoke(bounds));

        var toolsButton = AddMenuButton(
            "Tools",
            72.0f,
            bounds => ToolsRequested?.Invoke(bounds));

        panel.AddChild(buttons);

        return (panel, toolsButton);
    }

    private (
        UiPanel Panel,
        UiButton ValidateButton,
        UiButton PlayButton,
        UiButton StopButton,
        UiButton RestartButton,
        UiButton HotReloadButton,
        UiButton CloseButton) CreateToolbar()
    {
        var panel = new UiPanel
        {
            Background = new UiColor(28, 28, 28, 255),
            Padding = new UiThickness(8, 3, 8, 3),
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        var buttons = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 6.0f,
            HorizontalAlignment = UiHorizontalAlignment.Left,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        UiButton AddButton(
            string text,
            float width,
            Action action)
        {
            var button = new UiButton(text)
            {
                Width = width,
                Height = 30.0f
            };

            button.Clicked += action;
            buttons.AddChild(button);

            return button;
        }

        void AddSeparator()
        {
            buttons.AddChild(new UiPanel
            {
                Width = 1.0f,
                Height = 22.0f,
                Background = new UiColor(70, 70, 70, 255),
                VerticalAlignment = UiVerticalAlignment.Center
            });
        }

        AddButton("New Project", 112.0f,
        () => NewProjectRequested?.Invoke());

        AddButton(
        "Open Project",
        112.0f,
        () => OpenProjectRequested?.Invoke());

        AddButton(
        "Open IDE",
        88.0f,
        () => OpenProjectInIdeRequested?.Invoke());

        AddSeparator();

        var playButton = AddButton(
            "Play",
            64.0f,
            () => PlayRequested?.Invoke());
        var hotReloadButton = AddButton(
    "Hot Reload",
    100.0f,
    () => HotReloadRequested?.Invoke());
        var stopButton = AddButton(
            "Stop",
            64.0f,
            () => StopRequested?.Invoke());

        var restartButton = AddButton(
            "Restart",
            80.0f,
            () => RestartRequested?.Invoke());


        AddButton("Open", 64.0f,
            () => OpenRequested?.Invoke());

        AddButton("Save", 64.0f,
            () => SaveRequested?.Invoke());
        AddButton("Save All", 84.0f,
    () => SaveAllRequested?.Invoke());

        AddButton("Save As", 78.0f,
            () => SaveAsRequested?.Invoke());

        var closeButton = AddButton(
            "Close",
            64.0f,
            () => CloseRequested?.Invoke());

        AddSeparator();

        var validateButton = AddButton(
            "Validate",
            80.0f,
            () =>
            {
                var document =
                    Editor.Session.ActiveEditorDocument as EditorDocument;

                if (document is null)
                {
                    return;
                }

                Editor.Actions.Execute(
                    "validation.validate",
                    Editor.ActionContext);

                var result =
                    document.LastValidationResult;

                if (result is null)
                {
                    _debugConsole?.WriteError(
                        "Validation did not produce a result.");

                    Refresh();
                    return;
                }

                if (result.Issues.Count == 0)
                {
                    _debugConsole?.WriteLine(
                        $"Validation passed for '{document.DisplayName}'.");
                }
                else
                {
                    _debugConsole?.Write(
                        result.IsValid
                            ? DiagnosticLevel.Warning
                            : DiagnosticLevel.Error,
                        $"Validation finished for '{document.DisplayName}' " +
                        $"with {result.Issues.Count} issue(s).");

                    foreach (var issue in result.Issues)
                    {
                        var level =
                            issue.Severity switch
                            {
                                ValidationSeverity.Info =>
                                    DiagnosticLevel.Info,

                                ValidationSeverity.Warning =>
                                    DiagnosticLevel.Warning,

                                ValidationSeverity.Error =>
                                    DiagnosticLevel.Error,

                                _ =>
                                    DiagnosticLevel.Info
                            };

                        _debugConsole?.Write(
                            level,
                            $"[{issue.Code}] {issue.Message}",
                            issue.Code);
                    }
                }

                Refresh();
            });

        panel.AddChild(buttons);

        return (
            panel,
            validateButton,
            playButton,
            stopButton,
            restartButton,
            hotReloadButton,
            closeButton);
    }


    public void SetGameRunning(bool isRunning)
    {
        if (_isGameRunning == isRunning)
        {
            return;
        }

        _isGameRunning = isRunning;

        UpdateGameControls();
    }

    private void UpdateGameControls()
    {
        var hasProject =
            Editor.ActiveGameProject is not null;

        _playButton.Enabled =
            hasProject && !_isGameRunning;

        _stopButton.Enabled =
            hasProject && _isGameRunning;

        _restartButton.Enabled =
            hasProject && _isGameRunning;
        _hotReloadButton.Enabled =
    hasProject && !_isGameRunning;
    }
}