using Engine.Core.Math;
using Engine.Editor.Commands;
using Engine.Editor.Documents;
using Engine.Editor.Inspection;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;
using System.Globalization;

namespace Engine.Editor.UI.Inspection;

internal sealed class Fixed32InspectorPropertyEditor
    : IInspectorPropertyEditor
{
    public int Priority => 200;

    public bool CanEdit(Type propertyType) =>
        propertyType == typeof(Fixed32);

    public UiWidget Create(
        EditorDocument document,
        EditorProperty property)
    {
        var current =
            (Fixed32)(property.GetValue() ?? Fixed32.Zero);

        var editor =
            new UiStackPanel
            {
                Orientation = UiOrientation.Horizontal,
                Spacing = 6.0f,
                VerticalAlignment = UiVerticalAlignment.Top
            };

        editor.AddChild(
            new UiLabel(property.DisplayName)
            {
                Width = 90.0f,
                FontSize = 12.0f
            });

        var textBox =
            new UiTextBox(
                current.ToFloat().ToString(
                    "0.#####",
                    CultureInfo.InvariantCulture))
            {
                Width = 140.0f,
                Height = 32.0f,
                FontSize = 12.0f,
                Enabled = !property.IsReadOnly
            };

        if (!property.IsReadOnly)
        {
            textBox.Submitted += text =>
            {
                if (!TryParseFixed32(text, out var value))
                {
                    return;
                }

                document.Execute(
                    new SetEditorPropertyCommand(
                        property,
                        value,
                        document));
            };
        }

        editor.AddChild(textBox);

        return editor;
    }

    internal static bool TryParseFixed32(
        string text,
        out Fixed32 value)
    {
        value = Fixed32.Zero;

        if (!float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsed) ||
            !float.IsFinite(parsed))
        {
            return false;
        }

        try
        {
            value = Fixed32.FromFloat(parsed);
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }
}

internal sealed class FixedVector2InspectorPropertyEditor
    : IInspectorPropertyEditor
{
    public int Priority => 200;

    public bool CanEdit(Type propertyType) =>
        propertyType == typeof(FixedVector2);

    public UiWidget Create(
        EditorDocument document,
        EditorProperty property)
    {
        var current =
            property.GetValue() is FixedVector2 vector
                ? vector
                : FixedVector2.Zero;

        var editor =
            new UiStackPanel
            {
                Orientation = UiOrientation.Horizontal,
                Spacing = 4.0f,
                VerticalAlignment = UiVerticalAlignment.Top
            };

        editor.AddChild(
            new UiLabel(property.DisplayName)
            {
                Width = 78.0f,
                FontSize = 12.0f
            });

        var xBox =
            CreateCoordinateBox(current.X);

        var yBox =
            CreateCoordinateBox(current.Y);

        xBox.Submitted += text =>
        {
            if (!Fixed32InspectorPropertyEditor.TryParseFixed32(
                    text,
                    out var x))
            {
                return;
            }

            var latest = ReadCurrentValue(property);

            SetValue(
                document,
                property,
                new FixedVector2(x, latest.Y));
        };

        yBox.Submitted += text =>
        {
            if (!Fixed32InspectorPropertyEditor.TryParseFixed32(
                    text,
                    out var y))
            {
                return;
            }

            var latest = ReadCurrentValue(property);

            SetValue(
                document,
                property,
                new FixedVector2(latest.X, y));
        };

        editor.AddChild(
            new UiLabel("X")
            {
                FontSize = 12.0f,
                VerticalAlignment = UiVerticalAlignment.Center
            });

        editor.AddChild(xBox);

        editor.AddChild(
            new UiLabel("Y")
            {
                FontSize = 12.0f,
                VerticalAlignment = UiVerticalAlignment.Center
            });

        editor.AddChild(yBox);

        return editor;
    }

    private static UiTextBox CreateCoordinateBox(
        Fixed32 value)
    {
        return new UiTextBox(
            value.ToFloat().ToString(
                "0.#####",
                CultureInfo.InvariantCulture))
        {
            Width = 62.0f,
            Height = 32.0f,
            FontSize = 12.0f
        };
    }

    private static FixedVector2 ReadCurrentValue(
        EditorProperty property)
    {
        return property.GetValue() is FixedVector2 value
            ? value
            : FixedVector2.Zero;
    }

    private static void SetValue(
        EditorDocument document,
        EditorProperty property,
        FixedVector2 value)
    {
        if (property.IsReadOnly)
        {
            return;
        }

        document.Execute(
            new SetEditorPropertyCommand(
                property,
                value,
                document));
    }
}