using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Authoring;

internal sealed class EditorUiCanvasElementRenderer
{
    private readonly EditorUiCanvasImageRenderer _imageRenderer;

    public EditorUiCanvasElementRenderer(
        ITextureResourceManager? textures,
        EditorContext? editor,
        ITextureManager? projectTextureManager)
    {
        _imageRenderer = new EditorUiCanvasImageRenderer(
            textures,
            editor,
            projectTextureManager);
    }

    public void Render(
        UiRenderContext context,
        EditorUiDocument document,
        Vector2 canvasOrigin,
        float scale,
        Func<EditorUiElement, EditorUiLayout> layoutResolver)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(layoutResolver);

        RenderElement(
            context,
            document,
            document.Root,
            Vector2.Zero,
            layoutResolver(document.Root).Size,
            canvasOrigin,
            scale,
            layoutResolver);
    }

    private void RenderElement(
        UiRenderContext context,
        EditorUiDocument document,
        EditorUiElement element,
        Vector2 parentOrigin,
        Vector2 parentSize,
        Vector2 canvasOrigin,
        float scale,
        Func<EditorUiElement, EditorUiLayout> layoutResolver)
    {
        if (!element.Visible)
        {
            return;
        }

        var layout = layoutResolver(element);
        var localRect = layout.Resolve(parentSize);

        var elementOrigin = new Vector2(
            parentOrigin.X + localRect.X,
            parentOrigin.Y + localRect.Y);

        var rect = new UiRect(
            canvasOrigin.X + elementOrigin.X * scale,
            canvasOrigin.Y + elementOrigin.Y * scale,
            localRect.Width * scale,
            localRect.Height * scale);

        var rotationCenter = new Vector2(
            rect.X + rect.Width / 2.0f,
            rect.Y + rect.Height / 2.0f);

        context.PushRotation(
            rotationCenter,
            layout.RotationDegrees);

        try
        {
            if (!ReferenceEquals(element, document.Root))
            {
                context.DrawRectangle(
                    rect,
                    GetElementBackground(element),
                    filled: true);

                if (element.Type == EditorUiElementType.Image)
                {
                    _imageRenderer.Draw(
                        context,
                        element,
                        rect);
                }

                context.DrawRectangle(
                    rect,
                    document.Selection.Contains(element.Id)
                        ? new UiColor(235, 235, 235, 255)
                        : new UiColor(105, 105, 105, 255),
                    filled: false,
                    layer: 1);

                if (element.Type == EditorUiElementType.Slider)
                {
                    var trackHeight = MathF.Max(
                        2.0f,
                        rect.Height * 0.25f);

                    var knobWidth = MathF.Max(
                        6.0f,
                        rect.Height * 0.5f);

                    var value = Math.Clamp(
                        element.SliderValue,
                        0.0f,
                        1.0f);

                    var trackY = rect.Y +
                        (rect.Height - trackHeight) / 2.0f;

                    context.DrawRectangle(
                        new UiRect(
                            rect.X,
                            trackY,
                            rect.Width,
                            trackHeight),
                        new UiColor(45, 45, 45, 255),
                        filled: true,
                        layer: 2);

                    var fillWidth = rect.Width * value;

                    if (fillWidth > 0.0f)
                    {
                        context.DrawRectangle(
                            new UiRect(
                                rect.X,
                                trackY,
                                fillWidth,
                                trackHeight),
                            new UiColor(90, 160, 220, 255),
                            filled: true,
                            layer: 3);
                    }

                    context.DrawRectangle(
                        new UiRect(
                            rect.X + fillWidth - knobWidth / 2.0f,
                            rect.Y,
                            knobWidth,
                            rect.Height),
                        new UiColor(220, 220, 220, 255),
                        filled: true,
                        layer: 4);
                }

                if (ShouldDrawText(element) &&
                    !string.IsNullOrEmpty(element.Text))
                {
                    var textSize = context.MeasureText(
                        element.Text,
                        12.0f);

                    var textPosition = UiTextLayout.CalculatePosition(
                        rect,
                        textSize,
                        UiTextHorizontalAlignment.Center,
                        UiTextVerticalAlignment.Center);

                    context.DrawText(
                        element.Text,
                        textPosition,
                        12.0f,
                        new UiColor(235, 235, 235, 255),
                        layer: 2);
                }

                if (element.Type is not EditorUiElementType.Image
                    and not EditorUiElementType.Slider
                    and not EditorUiElementType.ProgressBar &&
                    string.IsNullOrEmpty(element.Text) &&
                    rect.Width >= 80.0f &&
                    rect.Height >= 26.0f)
                {
                    var text = element.Name;

                    var textSize = context.MeasureText(
                        text,
                        11.0f);

                    var textPosition = UiTextLayout.CalculatePosition(
                        rect,
                        textSize,
                        UiTextHorizontalAlignment.Center,
                        UiTextVerticalAlignment.Center);

                    context.DrawText(
                        text,
                        textPosition,
                        11.0f,
                        new UiColor(190, 190, 190, 255),
                        layer: 2);
                }

                if (element.Type == EditorUiElementType.ProgressBar)
                {
                    var value = Math.Clamp(
                        element.ProgressBarValue,
                        0.0f,
                        1.0f);

                    context.DrawRectangle(
                        rect,
                        new UiColor(35, 35, 35, 230),
                        filled: true,
                        layer: 2);

                    var fillWidth = rect.Width * value;

                    if (fillWidth > 0.0f)
                    {
                        context.DrawRectangle(
                            new UiRect(
                                rect.X,
                                rect.Y,
                                fillWidth,
                                rect.Height),
                            new UiColor(80, 200, 100, 255),
                            filled: true,
                            layer: 3);
                    }
                }
            }

            foreach (var child in element.Children.OrderBy(
                         static child => child.ZIndex))
            {
                RenderElement(
                    context,
                    document,
                    child,
                    elementOrigin,
                    new Vector2(
                        localRect.Width,
                        localRect.Height),
                    canvasOrigin,
                    scale,
                    layoutResolver);
            }
        }
        finally
        {
            context.PopRotation();
        }
    }

    private void ReleaseResources()
    {
        _imageRenderer.ReleaseResources();
    }

    public void Dispose()
    {
        ReleaseResources();
    }

    private static bool ShouldDrawText(
        EditorUiElement element)
    {
        return element.Type is
            EditorUiElementType.Label or
            EditorUiElementType.Button or
            EditorUiElementType.TextBox or
            EditorUiElementType.Toggle or
            EditorUiElementType.Dropdown;
    }

    private static UiColor GetElementBackground(
        EditorUiElement element)
    {
        return element.Type switch
        {
            EditorUiElementType.Panel =>
                new UiColor(60, 70, 85, 255),

            EditorUiElementType.Label =>
                new UiColor(70, 70, 70, 255),

            EditorUiElementType.Button =>
                new UiColor(65, 85, 105, 255),

            EditorUiElementType.Image =>
                new UiColor(75, 65, 90, 255),

            EditorUiElementType.TextBox =>
                new UiColor(55, 80, 75, 255),

            EditorUiElementType.Toggle =>
                new UiColor(75, 80, 55, 255),

            EditorUiElementType.Dropdown =>
                new UiColor(80, 65, 55, 255),

            EditorUiElementType.ScrollView =>
                new UiColor(55, 75, 85, 255),

            _ => new UiColor(60, 60, 60, 255)
        };
    }
}