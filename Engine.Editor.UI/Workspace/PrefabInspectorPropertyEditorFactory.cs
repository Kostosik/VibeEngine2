using Engine.Core.Math;
using Engine.Editor.Inspection;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;
using System.Globalization;

namespace Engine.Editor.UI.Workspace;

internal static class PrefabInspectorPropertyEditorFactory
{
    public static UiWidget Create(EditorProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);

        var type = Nullable.GetUnderlyingType(property.PropertyType)
            ?? property.PropertyType;

        if (type == typeof(FixedVector2))
        {
            return CreateFixedVector2Editor(property);
        }

        if (type == typeof(Fixed32))
        {
            return CreateTextEditor(property, ParseFixed32);
        }

        if (type == typeof(bool))
        {
            return CreateBooleanEditor(property);
        }

        if (type == typeof(string))
        {
            return CreateTextEditor(property);
        }

        if (type == typeof(int))
        {
            return CreateTextEditor(
                property,
                value => int.TryParse(
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
                property,
                value => float.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var result) && float.IsFinite(result)
                    ? result
                    : null);
        }

        if (type == typeof(double))
        {
            return CreateTextEditor(
                property,
                value => double.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var result) && double.IsFinite(result)
                    ? result
                    : null);
        }

        if (type.IsEnum)
        {
            return CreateEnumEditor(property, type);
        }

        return CreateReadOnlyLabel(property);
    }

    private static UiWidget CreateBooleanEditor(EditorProperty property)
    {
        if (property.IsReadOnly)
        {
            return CreateReadOnlyLabel(property);
        }

        var toggle = new UiToggle(property.DisplayName);

        toggle.SetValue((bool)(property.GetValue() ?? false));

        toggle.ValueChanged += value =>
            property.SetValue(value);

        return toggle;
    }

    private static UiWidget CreateTextEditor(
        EditorProperty property,
        Func<string, object?>? parser = null)
    {
        var editor = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 6.0f,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        editor.AddChild(new UiLabel(property.DisplayName)
        {
            Width = 90.0f,
            FontSize = 12.0f
        });

        var textBox = new UiTextBox(FormatValue(property.GetValue()))
        {
            Width = 140.0f,
            Height = 32.0f,
            FontSize = 12.0f,
            Enabled = !property.IsReadOnly
        };

        if (!property.IsReadOnly)
        {
            textBox.Submitted += value =>
            {
                var parsedValue = parser is null
                    ? value
                    : parser(value);

                if (parsedValue is null &&
                    property.PropertyType != typeof(string))
                {
                    return;
                }

                property.SetValue(parsedValue);
            };
        }

        editor.AddChild(textBox);
        return editor;
    }

    private static UiWidget CreateFixedVector2Editor(
        EditorProperty property)
    {
        if (property.IsReadOnly)
        {
            return CreateReadOnlyLabel(property);
        }

        var current = property.GetValue() is FixedVector2 value
            ? value
            : FixedVector2.Zero;

        var row = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 4.0f,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        row.AddChild(new UiLabel(property.DisplayName)
        {
            Width = 75.0f,
            FontSize = 12.0f
        });

        var xBox = new UiTextBox(FormatFixed32(current.X))
        {
            Width = 60.0f,
            Height = 32.0f,
            FontSize = 12.0f
        };

        var yBox = new UiTextBox(FormatFixed32(current.Y))
        {
            Width = 60.0f,
            Height = 32.0f,
            FontSize = 12.0f
        };

        xBox.Submitted += text =>
            SubmitFixedVectorComponent(property, text, isX: true);

        yBox.Submitted += text =>
            SubmitFixedVectorComponent(property, text, isX: false);

        row.AddChild(xBox);
        row.AddChild(yBox);

        return row;
    }

    private static void SubmitFixedVectorComponent(
        EditorProperty property,
        string text,
        bool isX)
    {
        if (ParseFixed32(text) is not Fixed32 value)
        {
            return;
        }

        var current = property.GetValue() is FixedVector2 vector
            ? vector
            : FixedVector2.Zero;

        var updated = isX
            ? current.WithX(value)
            : current.WithY(value);

        property.SetValue(updated);
    }

    private static UiWidget CreateEnumEditor(
        EditorProperty property,
        Type enumType)
    {
        if (property.IsReadOnly)
        {
            return CreateReadOnlyLabel(property);
        }

        var dropdown = new UiDropdown();
        var values = Enum.GetValues(enumType);

        foreach (var value in values)
        {
            dropdown.AddOption(value.ToString()!);
        }

        var currentValue = property.GetValue();

        if (currentValue is not null)
        {
            var index = Array.IndexOf(values, currentValue);

            if (index >= 0)
            {
                dropdown.SetSelectedIndex(index);
            }
        }

        dropdown.SelectionChanged += (index, _) =>
        {
            var value = values.GetValue(index);

            if (value is not null)
            {
                property.SetValue(value);
            }
        };

        var container = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 3.0f
        };

        container.AddChild(new UiLabel(property.DisplayName)
        {
            FontSize = 12.0f
        });

        container.AddChild(dropdown);
        return container;
    }

    private static UiWidget CreateReadOnlyLabel(EditorProperty property)
    {
        return new UiLabel(
            $"{property.DisplayName}: {FormatValue(property.GetValue())}")
        {
            FontSize = 12.0f,
            Color = new Engine.Graphics.Commands.UiColor(
                190, 190, 190, 255)
        };
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "null",
            Fixed32 fixedValue => FormatFixed32(fixedValue),
            float number => number.ToString(
                CultureInfo.InvariantCulture),
            double number => number.ToString(
                CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(
                null,
                CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string FormatFixed32(Fixed32 value)
    {
        return value.ToFloat().ToString(
            "0.###",
            CultureInfo.InvariantCulture);
    }

    private static object? ParseFixed32(string text)
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
}