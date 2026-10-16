using Engine.Core.Math;
using Engine.Editor.Commands;
using Engine.Editor.Inspection;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;
using System.Globalization;

namespace Engine.Editor.UI.Workspace;

internal static class InspectorPropertyEditorFactory
{
    public static UiWidget Create(
        Engine.Editor.Documents.EditorDocument document,
        EditorProperty property)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        ArgumentNullException.ThrowIfNull(
            property);

        var type =
            Nullable.GetUnderlyingType(
                property.PropertyType)
            ?? property.PropertyType;

        if (type == typeof(FixedVector2))
        {
            return CreateFixedVector2Editor(
                document,
                property);
        }

        if (type == typeof(Fixed32))
        {
            return CreateTextEditor(
                document,
                property,
                ParseFixed32);
        }

        if (type == typeof(bool))
        {
            return CreateBooleanEditor(
                document,
                property);
        }

        if (type == typeof(string))
        {
            return CreateTextEditor(
                document,
                property);
        }

        if (type == typeof(int))
        {
            return CreateTextEditor(
                document,
                property,
                value =>
                    int.TryParse(
                        value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var result)
                        ? result
                        : null);
        }

        if (type == typeof(float))
        {
            return CreateTextEditor(
                document,
                property,
                value =>
                    float.TryParse(
                        value,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var result)
                        ? result
                        : null);
        }

        if (type == typeof(double))
        {
            return CreateTextEditor(
                document,
                property,
                value =>
                    double.TryParse(
                        value,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var result)
                        ? result
                        : null);
        }

        if (type.IsEnum)
        {
            return CreateEnumEditor(
                document,
                property,
                type);
        }

        return new UiLabel(
            property.GetValue()?.ToString()
            ?? "<unsupported>")
        {
            FontSize = 12.0f
        };
    }

    private static UiWidget CreateBooleanEditor(
        Engine.Editor.Documents.EditorDocument document,
        EditorProperty property)
    {
        var toggle =
            new UiToggle(
                property.DisplayName);

        toggle.SetValue(
            (bool)(
                property.GetValue()
                ?? false));

        if (!property.IsReadOnly)
        {
            toggle.ValueChanged +=
                value =>
                {
                    document.Execute(
                        new SetEditorPropertyCommand(
                            property,
                            value));
                };
        }
        else
        {
            toggle.Enabled = false;
        }

        return toggle;
    }

    private static UiWidget CreateTextEditor(
        Engine.Editor.Documents.EditorDocument document,
        EditorProperty property,
        Func<string, object?>? parser = null)
    {
        var currentValue =
            property.GetValue();

        var text =
            currentValue switch
            {
                float value =>
                    value.ToString(
                        CultureInfo.InvariantCulture),

                double value =>
                    value.ToString(
                        CultureInfo.InvariantCulture),

                Fixed32 value =>
                    value.ToFloat().ToString(
                    "0.###",
                    CultureInfo.InvariantCulture),

                _ =>
                    currentValue?.ToString()
                    ?? string.Empty
            };

        var editor =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Horizontal,

                Spacing = 6.0f,

                VerticalAlignment =
                    UiVerticalAlignment.Top
            };

        editor.AddChild(
            new UiLabel(
                property.DisplayName)
            {
                Width = 90.0f,
                FontSize = 12.0f
            });

        var textBox =
            new UiTextBox(
                text)
            {
                Width = 140.0f,
                Height = 32.0f,

                FontSize = 12.0f,

                Enabled =
                    !property.IsReadOnly
            };

        if (!property.IsReadOnly)
        {
            textBox.Submitted +=
                value =>
                {
                    object? parsedValue;

                    if (parser is null)
                    {
                        parsedValue =
                            value;
                    }
                    else
                    {
                        parsedValue =
                            parser(value);
                    }

                    if (parsedValue is null &&
                        property.PropertyType !=
                        typeof(string))
                    {
                        return;
                    }

                    document.Execute(
                        new SetEditorPropertyCommand(
                            property,
                            parsedValue));
                };
        }

        editor.AddChild(
            textBox);

        return editor;
    }

    private static UiWidget CreateFixedVector2Editor(
    Engine.Editor.Documents.EditorDocument document,
    EditorProperty property)
    {
        var current =
            property.GetValue() is FixedVector2 value
                ? value
                : FixedVector2.Zero;

        var row =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Horizontal,

                Spacing = 4.0f,

                VerticalAlignment =
                    UiVerticalAlignment.Top
            };

        row.AddChild(
            new UiLabel(property.DisplayName)
            {
                Width = 75.0f,
                FontSize = 12.0f
            });

        var xBox =
            new UiTextBox(
                FormatFixed32(current.X))
            {
                Width = 60.0f,
                Height = 32.0f,
                FontSize = 12.0f,
                Enabled = !property.IsReadOnly
            };

        var yBox =
            new UiTextBox(
                FormatFixed32(current.Y))
            {
                Width = 60.0f,
                Height = 32.0f,
                FontSize = 12.0f,
                Enabled = !property.IsReadOnly
            };

        xBox.Submitted +=
            text => SubmitFixedVectorComponent(
                document,
                property,
                text,
                isX: true);

        yBox.Submitted +=
            text => SubmitFixedVectorComponent(
                document,
                property,
                text,
                isX: false);

        row.AddChild(xBox);
        row.AddChild(yBox);

        return row;
    }

    private static void SubmitFixedVectorComponent(
        Engine.Editor.Documents.EditorDocument document,
        EditorProperty property,
        string text,
        bool isX)
    {
        var parsed =
            ParseFixed32(text);

        if (parsed is not Fixed32 value)
        {
            return;
        }

        var current =
            property.GetValue() is FixedVector2 vector
                ? vector
                : FixedVector2.Zero;

        var updated =
            isX
                ? current.WithX(value)
                : current.WithY(value);

        document.Execute(
            new SetEditorPropertyCommand(
                property,
                updated));
    }

    private static string FormatFixed32(
        Fixed32 value)
    {
        return value.ToFloat().ToString(
            "0.###",
            CultureInfo.InvariantCulture);
    }

    private static object? ParseFixed32(
        string text)
    {
        if (!float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var value) ||
            !float.IsFinite(value))
        {
            return null;
        }

        try
        {
            return Fixed32.FromFloat(value);
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    private static UiWidget CreateEnumEditor(
        Engine.Editor.Documents.EditorDocument document,
        EditorProperty property,
        Type enumType)
    {
        var dropdown =
            new UiDropdown();

        var values =
            Enum.GetValues(
                enumType);

        foreach (var value in values)
        {
            dropdown.AddOption(
                value.ToString()!);
        }

        var currentValue =
            property.GetValue();

        if (currentValue is not null)
        {
            var index =
                Array.IndexOf(
                    values,
                    currentValue);

            if (index >= 0)
            {
                dropdown.SetSelectedIndex(
                    index);
            }
        }

        if (!property.IsReadOnly)
        {
            dropdown.SelectionChanged +=
                (index, _) =>
                {
                    var value =
                        values.GetValue(index);

                    document.Execute(
                        new SetEditorPropertyCommand(
                            property,
                            value));
                };
        }
        else
        {
            dropdown.Enabled = false;
        }

        var container =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical,

                Spacing = 3.0f
            };

        container.AddChild(
            new UiLabel(
                property.DisplayName)
            {
                FontSize = 12.0f
            });

        container.AddChild(
            dropdown);

        return container;
    }
}