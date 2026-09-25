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
            currentValue?.ToString()
            ?? string.Empty;

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