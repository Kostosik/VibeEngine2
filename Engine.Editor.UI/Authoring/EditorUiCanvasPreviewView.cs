using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Authoring;

public sealed class EditorUiCanvasPreviewView :
    UiPanel
{
    private const float CanvasPadding = 16.0f;

    private readonly EditorUiDocument _document;

    public EditorUiCanvasPreviewView(
        EditorUiDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        _document = document;

        Background =
            new UiColor(
                22,
                22,
                22,
                255);

        HorizontalAlignment =
            UiHorizontalAlignment.Stretch;

        VerticalAlignment =
            UiVerticalAlignment.Stretch;
    }

    public EditorUiDocument Document =>
        _document;

    protected override void OnRender(
        UiRenderContext context)
    {
        base.OnRender(context);

        var canvasRect =
            GetCanvasRect();

        context.DrawRectangle(
            canvasRect,
            new UiColor(
                38,
                38,
                38,
                255),
            filled: true);

        context.DrawRectangle(
            canvasRect,
            new UiColor(
                100,
                100,
                100,
                255),
            filled: false);

        context.PushClip(
            canvasRect);

        RenderElement(
            context,
            _document.Root,
            Vector2.Zero,
            _document.Root.Layout.Size,
            canvasRect.Position,
            GetScale());

        context.PopClip();
    }

    protected override void OnPointerDown(
        UiPointerEvent pointer)
    {
        if (!Bounds.Contains(
                pointer.Position))
        {
            return;
        }

        var canvasRect =
            GetCanvasRect();

        if (!canvasRect.Contains(
                pointer.Position))
        {
            return;
        }

        var scale =
            GetScale();

        if (scale <= 0.0f)
        {
            return;
        }

        var documentPosition =
            new Vector2(
                (pointer.Position.X -
                 canvasRect.X) /
                scale,

                (pointer.Position.Y -
                 canvasRect.Y) /
                scale);

        var selected =
            FindElementAt(
                _document.Root,
                documentPosition,
                Vector2.Zero,
                _document.Root.Layout.Size);

        if (selected is null)
        {
            return;
        }

        _document.Selection.Set(
            selected.Id);

        pointer.Handled = true;
    }

    private void RenderElement(
    UiRenderContext context,
    EditorUiElement element,
    Vector2 parentOrigin,
    Vector2 parentSize,
    Vector2 canvasOrigin,
    float scale)
    {
        var localRect =
            element.Layout.Resolve(
                parentSize);

        var elementOrigin =
            new Vector2(
                parentOrigin.X +
                localRect.X,
                parentOrigin.Y +
                localRect.Y);

        var rect =
            new UiRect(
                canvasOrigin.X +
                elementOrigin.X *
                scale,

                canvasOrigin.Y +
                elementOrigin.Y *
                scale,

                localRect.Width *
                scale,

                localRect.Height *
                scale);

        if (!ReferenceEquals(
                element,
                _document.Root))
        {
            context.DrawRectangle(
                rect,
                GetElementBackground(
                    element),
                filled: true);

            context.DrawRectangle(
                rect,
                _document.Selection.Contains(
                        element.Id)
                    ? new UiColor(
                        235,
                        235,
                        235,
                        255)
                    : new UiColor(
                        105,
                        105,
                        105,
                        255),
                filled: false,
                layer: 1);

            if (ShouldDrawText(element) &&
                !string.IsNullOrEmpty(
                    element.Text))
            {
                var textSize =
                    context.MeasureText(
                        element.Text,
                        12.0f);

                var textPosition =
                    UiTextLayout.CalculatePosition(
                        rect,
                        textSize,
                        UiTextHorizontalAlignment.Center,
                        UiTextVerticalAlignment.Center);

                context.DrawText(
                    element.Text,
                    textPosition,
                    12.0f,
                    new UiColor(
                        235,
                        235,
                        235,
                        255),
                    layer: 2);
            }

            if (string.IsNullOrEmpty(
                    element.Text) &&
                rect.Width >= 80.0f &&
                rect.Height >= 26.0f)
            {
                var text =
                    element.Name;

                var textSize =
                    context.MeasureText(
                        text,
                        11.0f);

                var textPosition =
                    UiTextLayout.CalculatePosition(
                        rect,
                        textSize,
                        UiTextHorizontalAlignment.Center,
                        UiTextVerticalAlignment.Center);

                context.DrawText(
                    text,
                    textPosition,
                    11.0f,
                    new UiColor(
                        190,
                        190,
                        190,
                        255),
                    layer: 2);
            }
        }

        foreach (var child in element.Children)
        {
            RenderElement(
                context,
                child,
                elementOrigin,
                new Vector2(
                    localRect.Width,
                    localRect.Height),
                canvasOrigin,
                scale);
        }
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
    private EditorUiElement? FindElementAt(
    EditorUiElement element,
    Vector2 point,
    Vector2 parentOrigin,
    Vector2 parentSize)
    {
        var localRect =
            element.Layout.Resolve(
                parentSize);

        var elementOrigin =
            new Vector2(
                parentOrigin.X +
                localRect.X,
                parentOrigin.Y +
                localRect.Y);

        for (var i =
             element.Children.Count - 1;
             i >= 0;
             i--)
        {
            var child =
                element.Children[i];

            var hit =
                FindElementAt(
                    child,
                    point,
                    elementOrigin,
                    new Vector2(
                        localRect.Width,
                        localRect.Height));

            if (hit is not null)
            {
                return hit;
            }
        }

        if (point.X >= elementOrigin.X &&
            point.Y >= elementOrigin.Y &&
            point.X <=
                elementOrigin.X +
                localRect.Width &&
            point.Y <=
                elementOrigin.Y +
                localRect.Height)
        {
            return element;
        }

        return null;
    }

    private UiRect GetCanvasRect()
    {
        var rootSize =
            _document.Root.Layout.Size;

        var availableWidth =
            MathF.Max(
                0.0f,
                Bounds.Width -
                CanvasPadding * 2.0f);

        var availableHeight =
            MathF.Max(
                0.0f,
                Bounds.Height -
                CanvasPadding * 2.0f);

        if (rootSize.X <= 0.0f ||
            rootSize.Y <= 0.0f)
        {
            return new UiRect(
                Bounds.X,
                Bounds.Y,
                0.0f,
                0.0f);
        }

        var scale =
            MathF.Min(
                availableWidth /
                rootSize.X,
                availableHeight /
                rootSize.Y);

        var width =
            rootSize.X *
            scale;

        var height =
            rootSize.Y *
            scale;

        return new UiRect(
            Bounds.X +
            (Bounds.Width - width) /
            2.0f,

            Bounds.Y +
            (Bounds.Height - height) /
            2.0f,

            width,
            height);
    }

    private float GetScale()
    {
        var rootSize =
            _document.Root.Layout.Size;

        if (rootSize.X <= 0.0f ||
            rootSize.Y <= 0.0f)
        {
            return 0.0f;
        }

        var availableWidth =
            MathF.Max(
                0.0f,
                Bounds.Width -
                CanvasPadding * 2.0f);

        var availableHeight =
            MathF.Max(
                0.0f,
                Bounds.Height -
                CanvasPadding * 2.0f);

        return MathF.Min(
            availableWidth /
            rootSize.X,
            availableHeight /
            rootSize.Y);
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

            _ =>
                new UiColor(
                    60,
                    60,
                    60,
                    255)
        };
    }
}