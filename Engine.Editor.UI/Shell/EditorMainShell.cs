using Engine.Core.Math;
using Engine.Editor.Documents;
using Engine.Editor.UI.Authoring;
using Engine.Editor.UI.Workspace;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.Tooling.Validation;
using Engine.UI.Controls;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Shell;

public sealed class EditorMainShell :
    UiPanel
{
    private UiButton _validateButton;
    private readonly UiPanel _menuBar;
    private readonly UiPanel _toolbar;
    private readonly UiPanel _workspace;
    private UiButton _closeButton;
    private readonly UiLabel _documentLabel;

    private readonly EditorWorkspaceView _workspaceView;
    private EditorUiWorkspaceView _uiWorkspaceView;

    public bool IsUiMode =>
        Editor.Session.ActiveEditorDocument
            is EditorUiDocument;

    public event Action? NewRequested;
    public event Action? OpenRequested;
    public event Action? SaveRequested;
    public event Action? SaveAsRequested;
    public event Action? CloseRequested;
    public event Action<UiRect>? ToolsRequested;
    public event Action<UiRect>? ViewRequested;
    public event Action<UiRect>? EditRequested;

    private readonly UiPanel _documentTabs;
    private UiButton _worldButton;
    private UiButton _uiButton;
    private UiButton _toolsButton;
    public EditorMainShell(
        EditorContext editor,
        ITextureResourceManager assetPreviewTextures,
        EditorUiDocument uiDocument)
    {
        ArgumentNullException.ThrowIfNull(
            editor);

        ArgumentNullException.ThrowIfNull(
            assetPreviewTextures);

        ArgumentNullException.ThrowIfNull(
            uiDocument);

        Editor =
            editor;

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

        _menuBar =
            CreateMenuBar();
        _toolbar = CreateToolbar();
        _documentTabs =
    new UiPanel
    {
        Background =
            new UiColor(
                28,
                28,
                28,
                255),

        HorizontalAlignment =
            UiHorizontalAlignment.Stretch,

        VerticalAlignment =
            UiVerticalAlignment.Top
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

        _workspaceView =
            new EditorWorkspaceView(
                editor,
                assetPreviewTextures);

        _uiWorkspaceView =
            new EditorUiWorkspaceView(
                uiDocument);

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



    public UiPanel Workspace =>
        _workspace;

    protected override Vector2 MeasureCore(
    UiLayoutContext context,
    Vector2 availableSize)
    {
        const float menuHeight = 32.0f;
        const float toolbarHeight = 36.0f;
        const float documentTabsHeight = 30.0f;
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
        const float documentTabsHeight = 30.0f;
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

        _workspace.RemoveChild(
            _uiWorkspaceView);

        _uiWorkspaceView =
            new EditorUiWorkspaceView(document);

        _workspace.AddChild(
            _uiWorkspaceView);
    }

    public void Refresh()
    {
        var activeDocument =
            Editor.Session.ActiveEditorDocument;

        _workspaceView.Visible =
            activeDocument is EditorDocument;

        _uiWorkspaceView.Visible =
            activeDocument is EditorUiDocument;

        RefreshDocumentTabs();

        if (activeDocument is EditorUiDocument activeUiDocument)
        {
            BindUiDocument(activeUiDocument);
        }

        var worldDocument =
            activeDocument as EditorDocument;

        var uiDocument =
            activeDocument as EditorUiDocument;

        _uiWorkspaceView.Visible =
            uiDocument is not null;

        _workspaceView.Visible =
            worldDocument is not null ||
            activeDocument is null;

        _validateButton.Enabled =
            worldDocument is not null;

        _closeButton.Enabled =
            worldDocument is not null;

        if (worldDocument is not null)
        {
            _documentLabel.Text =
                BuildDocumentStatus(
                    worldDocument,
                    worldDocument.IsDirty
                        ? "Unsaved changes"
                        : "Saved");
        }
        else if (uiDocument is not null)
        {
            _documentLabel.Text =
                $"UI • {uiDocument.DisplayName} • " +
                (uiDocument.IsDirty
                    ? "Unsaved changes"
                    : "Saved");
        }
        else
        {
            _documentLabel.Text = "No document";
        }



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

        _documentTabs.AddChild(
            tabs);
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
private UiPanel CreateMenuBar()
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
            Action<UiRect>? requested)
        {
            var button = new UiButton(text)
            {
                Width = width,
                Height = 28.0f
            };

            if (requested is not null)
            {
                button.Clicked += () =>
                    requested(button.Bounds);
            }

            buttons.AddChild(button);
            return button;
        }

        AddMenuButton("Edit", 64.0f, EditRequested);
        AddMenuButton("View", 64.0f, ViewRequested);

        _toolsButton = AddMenuButton(
            "Tools",
            72.0f,
            ToolsRequested);

        panel.AddChild(buttons);

        return panel;
    }

private UiPanel CreateToolbar()
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

        AddButton("New", 64.0f,
            () => NewRequested?.Invoke());

        AddButton("Open", 64.0f,
            () => OpenRequested?.Invoke());

        AddButton("Save", 64.0f,
            () => SaveRequested?.Invoke());

        AddButton("Save As", 78.0f,
            () => SaveAsRequested?.Invoke());

        _closeButton = AddButton(
            "Close",
            64.0f,
            () => CloseRequested?.Invoke());

        AddSeparator();

        _worldButton = AddButton(
            "World",
            72.0f,
            ShowWorld);

        _uiButton = AddButton(
            "UI",
            56.0f,
            ShowUi);

        _validateButton = AddButton(
            "Validate",
            80.0f,
            () =>
            {
                Editor.Actions.Execute(
                    "validation.validate",
                    Editor.ActionContext);

                Refresh();
            });

        panel.AddChild(buttons);

        return panel;
    }
}