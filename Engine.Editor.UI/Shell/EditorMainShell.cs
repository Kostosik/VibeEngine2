using Engine.Core.Math;
using Engine.Editor;
using Engine.Editor.UI.Workspace;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Shell;

public sealed class EditorMainShell : UiPanel
{
    private readonly UiPanel _menuBar;
    private readonly UiPanel _workspace;
    private readonly UiLabel _documentLabel;
    private readonly EditorWorkspaceView _workspaceView;
    private UiButton _undoButton;
    private UiButton _redoButton;
    public EditorMainShell(
        EditorContext editor)
    {
        ArgumentNullException.ThrowIfNull(
            editor);

        Editor = editor;

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
        editor);

        _workspace.AddChild(
            _workspaceView);
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

    public EditorContext Editor { get; }

    public UiPanel Workspace =>
        _workspace;

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        var menuHeight =
            32.0f;

        var documentHeight =
            24.0f;

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
                finalRect.Y + menuHeight,
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
        var document =
            Editor.ActiveDocument;

        _documentLabel.Text =
            document is null
                ? "No document"
                : document.IsDirty
                    ? "Unsaved changes"
                    : "Saved";

        _undoButton.Enabled =
            document?.CommandHistory.CanUndo == true;

        _redoButton.Enabled =
            document?.CommandHistory.CanRedo == true;

        _workspaceView.Refresh();
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

        buttons.AddChild(
            new UiButton("File"));

        buttons.AddChild(
            new UiButton("Edit"));

        _undoButton =
    new UiButton("Undo")
    {
        Width = 80.0f,
        Height = 30.0f
    };

        _undoButton.Clicked +=
            () =>
            {
                Editor.Actions.Execute(
                    "edit.undo",
                    Editor.ActionContext);
            };

        _redoButton =
            new UiButton("Redo")
            {
                Width = 80.0f,
                Height = 30.0f
            };

        _redoButton.Clicked +=
            () =>
            {
                Editor.Actions.Execute(
                    "edit.redo",
                    Editor.ActionContext);
            };

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