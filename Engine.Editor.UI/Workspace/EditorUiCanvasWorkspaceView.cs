
using Engine.Core.Math;
using Engine.Editor.UI.Authoring;
using Engine.Graphics.Resources;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;
using System.Globalization;

namespace Engine.Editor.UI.Workspace;

internal sealed class EditorUiCanvasWorkspaceView : UiPanel
{
    private const float ToolbarHeight = 38.0f;

    private readonly UiStackPanel _toolbar;
    private readonly UiToggle _gridToggle;
    private readonly UiToggle _snapToggle;
    private readonly UiLabel _gridStepLabel;
    private readonly UiTextBox _gridStepInput;
    public EditorUiCanvasPreviewView Preview { get; }

    public EditorUiCanvasWorkspaceView(
        EditorUiDocument document,
        ITextureResourceManager? textures,
        EditorContext? editor,
        ITextureManager? projectTextureManager)
    {
        Preview = new EditorUiCanvasPreviewView(
            document,
            textures,
            editor,
            projectTextureManager);

        _toolbar = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 8.0f,
            Height = ToolbarHeight,
            HorizontalAlignment = UiHorizontalAlignment.Stretch
        };

        _gridToggle = new UiToggle("Show Grid")
        {
            Width = 130.0f,
            Height = 30.0f,
            FontSize = 13.0f
        };

        _gridToggle.ValueChanged += value =>
            Preview.ShowGrid = value;

        _gridToggle.SetValue(true);
        _snapToggle = new UiToggle("Snap to Grid")
        {
            Width = 130.0f,
            Height = 30.0f,
            FontSize = 13.0f
        };

        _snapToggle.ValueChanged += value =>
            Preview.SnapToGrid = value;

        _snapToggle.SetValue(true);

        _gridStepLabel = new UiLabel("Grid Step")
        {
            Width = 68.0f,
            Height = 30.0f,
            FontSize = 13.0f,
            VerticalTextAlignment = UiTextVerticalAlignment.Center
        };

        _gridStepInput = new UiTextBox(
            EditorUiCanvasGrid.Step.ToString(
                "G",
                CultureInfo.CurrentCulture))
        {
            Width = 76.0f,
            Height = 30.0f,
            FontSize = 13.0f,
            MaxLength = 16,
            Placeholder = "20"
        };

        _gridStepInput.TextChanged += OnGridStepTextChanged;
        _gridStepInput.Submitted += OnGridStepSubmitted;

        _toolbar.AddChild(_snapToggle);
        _toolbar.AddChild(_gridToggle);
        _toolbar.AddChild(_gridStepLabel);
        _toolbar.AddChild(_gridStepInput);
        AddChild(_toolbar);
        AddChild(Preview);
    }

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        _toolbar.Measure(
            context,
            new Vector2(availableSize.X, ToolbarHeight));

        Preview.Measure(
            context,
            new Vector2(
                availableSize.X,
                MathF.Max(0.0f, availableSize.Y - ToolbarHeight)));

        return availableSize;
    }

    private void OnGridStepTextChanged(string text)
    {
        if (TryParseGridStep(text, out var step))
        {
            EditorUiCanvasGrid.Step = step;
        }
    }

    private void OnGridStepSubmitted(string text)
    {
        _gridStepInput.SetText(
            EditorUiCanvasGrid.Step.ToString(
                "G",
                CultureInfo.CurrentCulture));
    }

    private static bool TryParseGridStep(
        string text,
        out float step)
    {
        if (!float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.CurrentCulture,
                out step) &&
            !float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out step))
        {
            return false;
        }

        return float.IsFinite(step) &&
               step >= EditorUiCanvasGrid.MinimumStep;
    }

    protected override void ArrangeCore(UiRect finalRect)
    {
        var content = finalRect.Deflate(Padding);
        var toolbarHeight = MathF.Min(
            ToolbarHeight,
            content.Height);

        _toolbar.Arrange(
            new UiRect(
                content.X,
                content.Y,
                content.Width,
                toolbarHeight));

        Preview.Arrange(
            new UiRect(
                content.X,
                content.Y + toolbarHeight,
                content.Width,
                MathF.Max(0.0f, content.Height - toolbarHeight)));
    }
}
