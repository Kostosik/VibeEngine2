using Engine.Editor.Commands;
using Engine.Editor.Inspection;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;
using System.Globalization;

namespace Engine.Editor.UI.Authoring;

public sealed class EditorUiInspectorPanelView :
    UiPanel
{
    private readonly EditorUiDocument _document;
    private readonly EditorUiInspector _inspector;
    private readonly UiStackPanel _content;

    public EditorUiInspectorPanelView(
        EditorUiDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        _document = document;
        _inspector =
            new EditorUiInspector(
                document);

        Background =
            new UiColor(
                28,
                28,
                28,
                255);

        Padding =
            new UiThickness(
                6.0f);

        _content =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical,

                Spacing = 6.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Top
            };

        var scroll =
            new UiScrollView
            {
                VerticalScrolling = true,
                HorizontalScrolling = false,
                ScrollSpeed = 32.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Stretch
            };

        scroll.SetContent(
            _content);

        AddChild(
            scroll);
    }

    public EditorUiDocument Document =>
        _document;

    public void Refresh()
    {
        _content.ClearChildren();

        if (!_inspector.TryGetSelectedElement(
                out var element))
        {
            _content.AddChild(
                new UiLabel(
                    "Nothing selected")
                {
                    FontSize = 13.0f,

                    Color =
                        new UiColor(
                            150,
                            150,
                            150,
                            255)
                });

            return;
        }

        _content.AddChild(
            new UiLabel(
                element.Name)
            {
                FontSize = 16.0f,

                Color =
                    new UiColor(
                        220,
                        220,
                        220,
                        255)
            });

        _content.AddChild(
            new UiLabel(
                element.Type.ToString())
            {
                FontSize = 12.0f,

                Color =
                    new UiColor(
                        145,
                        145,
                        145,
                        255)
            });

        foreach (var property in
                 _inspector.GetProperties(
                     element))
        {
            AddProperty(
                property);
        }
    }

    private void AddProperty(
        EditorProperty property)
    {
        if (property.PropertyType == typeof(bool))
        {
            _content.AddChild(
                CreateBooleanProperty(
                    property));

            return;
        }

        if (property.PropertyType == typeof(int))
        {
            _content.AddChild(
                CreateIntegerProperty(
                    property));

            return;
        }

        if (property.Name == "Type")
        {
            _content.AddChild(
                CreateReadOnlyProperty(
                    property));

            return;
        }

        if (property.PropertyType == typeof(string))
        {
            _content.AddChild(
                CreateTextProperty(
                    property));

            return;
        }

        if (property.PropertyType == typeof(float))
        {
            _content.AddChild(
                CreateFloatProperty(
                    property));

            return;
        }

        _content.AddChild(
            new UiLabel(
                $"{property.DisplayName}: unsupported")
            {
                FontSize = 12.0f
            });
    }

    private UiWidget CreateBooleanProperty(
    EditorProperty property)
    {
        var toggle =
            new UiToggle(
                property.DisplayName);

        toggle.SetValue(
            Convert.ToBoolean(
                property.GetValue()));

        toggle.ValueChanged +=
            value =>
            {
                _document.Execute(
                    new SetEditorPropertyCommand(
                        property,
                        value));
            };

        return toggle;
    }

    private UiWidget CreateIntegerProperty(
        EditorProperty property)
    {
        var row =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Horizontal,

                Spacing = 6.0f
            };

        row.AddChild(
            new UiLabel(
                property.DisplayName)
            {
                Width = 70.0f,
                FontSize = 12.0f
            });

        var textBox =
            new UiTextBox(
                Convert.ToInt32(
                    property.GetValue())
                .ToString(
                    CultureInfo.InvariantCulture))
            {
                Width = 150.0f,
                Height = 32.0f,
                FontSize = 12.0f
            };

        textBox.Submitted +=
            value =>
            {
                if (!int.TryParse(
                        value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var parsed))
                {
                    return;
                }

                _document.Execute(
                    new SetEditorPropertyCommand(
                        property,
                        parsed));
            };

        row.AddChild(
            textBox);

        return row;
    }

    private UiWidget CreateReadOnlyProperty(
    EditorProperty property)
    {
        var row =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Horizontal,

                Spacing = 6.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Top
            };

        row.AddChild(
            new UiLabel(
                property.DisplayName)
            {
                Width = 70.0f,
                FontSize = 12.0f
            });

        row.AddChild(
            new UiLabel(
                property.GetValue()?.ToString()
                ?? string.Empty)
            {
                FontSize = 12.0f
            });

        return row;
    }

    private UiWidget CreateTextProperty(
        EditorProperty property)
    {
        var row =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Horizontal,

                Spacing = 6.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Top
            };

        row.AddChild(
            new UiLabel(
                property.DisplayName)
            {
                Width = 70.0f,
                FontSize = 12.0f
            });

        var textBox =
            new UiTextBox(
                property.GetValue()?.ToString()
                ?? string.Empty)
            {
                Width = 150.0f,
                Height = 32.0f,

                FontSize = 12.0f
            };

        textBox.Submitted +=
            value =>
            {
                if (string.IsNullOrWhiteSpace(
                        value))
                {
                    return;
                }

                _document.Execute(
                    new SetEditorPropertyCommand(
                        property,
                        value));
            };

        row.AddChild(
            textBox);

        return row;
    }

    private UiWidget CreateFloatProperty(
        EditorProperty property)
    {
        var row =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Horizontal,

                Spacing = 6.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Top
            };

        row.AddChild(
            new UiLabel(
                property.DisplayName)
            {
                Width = 70.0f,
                FontSize = 12.0f
            });

        var currentValue =
            Convert.ToSingle(
                property.GetValue(),
                CultureInfo.InvariantCulture);

        var textBox =
            new UiTextBox(
                currentValue.ToString(
                    "0.###",
                    CultureInfo.InvariantCulture))
            {
                Width = 150.0f,
                Height = 32.0f,

                FontSize = 12.0f
            };

        textBox.Submitted +=
            value =>
            {
                if (!float.TryParse(
                        value,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var parsed))
                {
                    return;
                }

                if ((property.Name == "Width" ||
                     property.Name == "Height") &&
                    parsed < 0.0f)
                {
                    return;
                }

                _document.Execute(
                    new SetEditorPropertyCommand(
                        property,
                        parsed));
            };

        row.AddChild(
            textBox);

        return row;
    }
}