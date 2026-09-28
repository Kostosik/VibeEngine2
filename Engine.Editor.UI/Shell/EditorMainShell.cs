using Engine.Core.Math;
using Engine.Editor;
using Engine.Editor.UI.Authoring;
using Engine.Editor.UI.Workspace;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Shell;

public sealed class EditorMainShell :
    UiPanel
{
    private readonly UiPanel _menuBar;
    private readonly UiPanel _workspace;

    private readonly UiLabel _documentLabel;

    private readonly EditorWorkspaceView _workspaceView;
    private readonly EditorUiWorkspaceView _uiWorkspaceView;

    private UiButton _undoButton;
    private UiButton _redoButton;
    private UiButton _worldButton;
    private UiButton _uiButton;

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

        UiDocument =
            uiDocument;

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

        AddChild(
            _workspace);

        AddChild(
            _documentLabel);
    }
    public event Action? SaveRequested;
    public EditorContext Editor { get; }

    public EditorUiDocument UiDocument { get; }

    public UiPanel Workspace =>
        _workspace;

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        const float menuHeight = 32.0f;
        const float documentHeight = 24.0f;

        _menuBar.Measure(
            context,
            new Vector2(
                availableSize.X,
                menuHeight));

        _workspace.Measure(
            context,
            new Vector2(
                availableSize.X,
                MathF.Max(
                    0.0f,
                    availableSize.Y -
                    menuHeight -
                    documentHeight)));

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
        const float documentHeight = 24.0f;

        _menuBar.Arrange(
            new UiRect(
                finalRect.X,
                finalRect.Y,
                finalRect.Width,
                menuHeight));

        _workspace.Arrange(
            new UiRect(
                finalRect.X,
                finalRect.Y +
                menuHeight,
                finalRect.Width,
                MathF.Max(
                    0.0f,
                    finalRect.Height -
                    menuHeight -
                    documentHeight)));

        _documentLabel.Arrange(
            new UiRect(
                finalRect.X,
                finalRect.Bottom -
                documentHeight,
                finalRect.Width,
                documentHeight));
    }

    public void Refresh()
    {
        var worldDocument =
            Editor.ActiveDocument;

        var uiActive =
            _uiWorkspaceView.Visible;

        if (uiActive)
        {
            _documentLabel.Text =
                UiDocument.IsDirty
                    ? "UI • Unsaved changes"
                    : "UI • Saved";

            _undoButton.Enabled =
                UiDocument.CommandHistory.CanUndo;

            _redoButton.Enabled =
                UiDocument.CommandHistory.CanRedo;
        }
        else
        {
            _documentLabel.Text =
                worldDocument is null
                    ? "No document"
                    : worldDocument.IsDirty
                        ? "World • Unsaved changes"
                        : "World • Saved";

            _undoButton.Enabled =
                worldDocument?.CommandHistory.CanUndo == true;

            _redoButton.Enabled =
                worldDocument?.CommandHistory.CanRedo == true;
        }

        _workspaceView.Refresh();
        _uiWorkspaceView.Refresh();
    }

    private void ShowWorld()
    {
        _workspaceView.Visible = true;
        _uiWorkspaceView.Visible = false;

        Refresh();
    }

    private void ShowUi()
    {
        _workspaceView.Visible = false;
        _uiWorkspaceView.Visible = true;

        Refresh();
    }

    private void Undo()
    {
        if (_uiWorkspaceView.Visible)
        {
            UiDocument.Undo();
        }
        else
        {
            Editor.Actions.Execute(
                "edit.undo",
                Editor.ActionContext);
        }

        Refresh();
    }

    private void Redo()
    {
        if (_uiWorkspaceView.Visible)
        {
            UiDocument.Redo();
        }
        else
        {
            Editor.Actions.Execute(
                "edit.redo",
                Editor.ActionContext);
        }

        Refresh();
    }

    private UiPanel CreateMenuBar()
    {
        var panel =
            new UiPanel
            {
                Background =
                    new UiColor(
                        32,
                        32,
                        32,
                        255),

                Padding =
                    new UiThickness(
                        6.0f,
                        3.0f,
                        6.0f,
                        3.0f),

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Top
            };

        var buttons =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Horizontal,

                Spacing = 4.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Left,

                VerticalAlignment =
                    UiVerticalAlignment.Stretch
            };

        var fileButton =
            new UiButton("Save")
            {
                Width = 80.0f,
                Height = 30.0f
            };

        fileButton.Clicked +=
            () =>
            {
                SaveRequested?.Invoke();
            };

        buttons.AddChild(
            fileButton);

        buttons.AddChild(
            new UiButton("Edit"));

        _worldButton =
            new UiButton("World")
            {
                Width = 80.0f,
                Height = 30.0f
            };

        _worldButton.Clicked +=
            ShowWorld;

        _uiButton =
            new UiButton("UI")
            {
                Width = 80.0f,
                Height = 30.0f
            };

        _uiButton.Clicked +=
            ShowUi;

        _undoButton =
            new UiButton("Undo")
            {
                Width = 80.0f,
                Height = 30.0f
            };

        _undoButton.Clicked +=
            Undo;

        _redoButton =
            new UiButton("Redo")
            {
                Width = 80.0f,
                Height = 30.0f
            };

        _redoButton.Clicked +=
            Redo;

        buttons.AddChild(
            _worldButton);

        buttons.AddChild(
            _uiButton);

        buttons.AddChild(
            _undoButton);

        buttons.AddChild(
            _redoButton);

        buttons.AddChild(
            new UiButton("View"));

        buttons.AddChild(
            new UiButton("Tools"));

        panel.AddChild(
            buttons);

        return panel;
    }
}