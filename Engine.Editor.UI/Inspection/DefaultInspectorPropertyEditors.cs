using Engine.Editor.Commands;
using Engine.Editor.Documents;
using Engine.Editor.Inspection;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;
using System.Globalization;

namespace Engine.Editor.UI.Inspection;

internal sealed class BooleanInspectorPropertyEditor
    : IInspectorPropertyEditor
{
    public int Priority => 100;

    public bool CanEdit(Type propertyType) =>
        propertyType == typeof(bool);

    public UiWidget Create(
        EditorDocument document,
        EditorProperty property)
    {
        var toggle =
            new UiToggle(property.DisplayName);

        toggle.SetValue(
            property.GetValue() is true);

        if (property.IsReadOnly)
        {
            toggle.Enabled = false;
        }
        else
        {
            toggle.ValueChanged += value =>
                document.Execute(
                    new SetEditorPropertyCommand(
                        property,
                        value));
        }

        return toggle;
    }
}

internal sealed class ScalarInspectorPropertyEditor
    : IInspectorPropertyEditor
{
    public int Priority => 100;

    public bool CanEdit(Type propertyType) =>
        propertyType == typeof(string) ||
        IsNumeric(propertyType);

    public UiWidget Create(
        EditorDocument document,
        EditorProperty property)
    {
        var type =
            Nullable.GetUnderlyingType(
                property.PropertyType)
            ?? property.PropertyType;

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

                decimal value =>
                    value.ToString(
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
            new UiLabel(property.DisplayName)
            {
                Width = 90.0f,
                FontSize = 12.0f
            });

        var textBox =
            new UiTextBox(text)
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
                object? parsedValue;

                if (type == typeof(string))
                {
                    parsedValue = value;
                }
                else if (!TryParseNumeric(
                             value,
                             type,
                             out parsedValue))
                {
                    return;
                }

                document.Execute(
                    new SetEditorPropertyCommand(
                        property,
                        parsedValue));
            };
        }

        editor.AddChild(textBox);

        return editor;
    }

    private static bool IsNumeric(Type type)
    {
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Byte => true,
            TypeCode.SByte => true,
            TypeCode.Int16 => true,
            TypeCode.UInt16 => true,
            TypeCode.Int32 => true,
            TypeCode.UInt32 => true,
            TypeCode.Int64 => true,
            TypeCode.UInt64 => true,
            TypeCode.Single => true,
            TypeCode.Double => true,
            TypeCode.Decimal => true,
            _ => false
        };
    }

    private static bool TryParseNumeric(
        string text,
        Type type,
        out object? value)
    {
        try
        {
            value =
                Convert.ChangeType(
                    text,
                    type,
                    CultureInfo.InvariantCulture);

            return true;
        }
        catch (FormatException)
        {
            value = null;
            return false;
        }
        catch (OverflowException)
        {
            value = null;
            return false;
        }
    }
}

internal sealed class EnumInspectorPropertyEditor
    : IInspectorPropertyEditor
{
    public int Priority => 100;

    public bool CanEdit(Type propertyType) =>
        propertyType.IsEnum;

    public UiWidget Create(
        EditorDocument document,
        EditorProperty property)
    {
        var enumType =
            Nullable.GetUnderlyingType(
                property.PropertyType)
            ?? property.PropertyType;

        var values =
            Enum.GetValues(enumType);

        var dropdown =
            new UiDropdown();

        foreach (var value in values)
        {
            dropdown.AddOption(value.ToString()!);
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
                dropdown.SetSelectedIndex(index);
            }
        }

        if (property.IsReadOnly)
        {
            dropdown.Enabled = false;
        }
        else
        {
            dropdown.SelectionChanged += (index, _) =>
            {
                var value =
                    values.GetValue(index);

                document.Execute(
                    new SetEditorPropertyCommand(
                        property,
                        value));
            };
        }

        var container =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical,

                Spacing = 3.0f
            };

        container.AddChild(
            new UiLabel(property.DisplayName)
            {
                FontSize = 12.0f
            });

        container.AddChild(dropdown);

        return container;
    }
}

internal sealed class ReadOnlyInspectorPropertyEditor
    : IInspectorPropertyEditor
{
    public int Priority => int.MinValue;

    public bool CanEdit(Type propertyType) => true;

    public UiWidget Create(
        EditorDocument document,
        EditorProperty property)
    {
        var value =
            property.GetValue()?.ToString()
            ?? "<null>";

        return new UiLabel(
            $"{property.DisplayName}: {value}")
        {
            FontSize = 12.0f
        };
    }
}