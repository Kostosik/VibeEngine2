using Engine.Core.Assets;
using Engine.Editor.Inspection;
using Engine.Core.Math;

namespace Engine.Editor.UI.Authoring;

public sealed class EditorUiInspector
{
    private readonly EditorUiDocument _document;

    public EditorUiInspector(
        EditorUiDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        _document = document;
    }

    public bool TryGetSelectedElement(
        out EditorUiElement? element)
    {
        if (_document.Selection.Count != 1)
        {
            element = null;
            return false;
        }

        var id =
            _document.Selection.Items.First();

        return _document.TryGetElement(
            id,
            out element);
    }

    public IReadOnlyList<EditorProperty> GetProperties(
        EditorUiElement element)
    {
        ArgumentNullException.ThrowIfNull(
            element);

        var properties =
            new List<EditorProperty>();

        if (element.Type is
    EditorUiElementType.Label or
    EditorUiElementType.Button or
    EditorUiElementType.TextBox or
    EditorUiElementType.Toggle or
    EditorUiElementType.Dropdown)
        {
            properties.Add(
                new EditorProperty(
                    "Text",
                    typeof(string),
                    () => element.Text,
                    value =>
                    {
                        element.Text =
                            value as string
                            ?? string.Empty;
                    },
                    displayName: "Text",
                    order: 20));
        }

        if (element.Type ==
            EditorUiElementType.Image)
        {
            properties.Add(
                new EditorProperty(
                    "Texture",
                    typeof(string),
                    () =>
                        element.Texture?.Value
                        ?? string.Empty,
                    value =>
                    {
                        var path =
                            value as string;

                        element.Texture =
                            string.IsNullOrWhiteSpace(path)
                                ? null
                                : new AssetPath(path);
                    },
                    displayName: "Texture",
                    order: 20));
        }

        properties.Add(
            new EditorProperty(
                "Name",
                typeof(string),
                () => element.Name,
                value =>
                {
                    var name =
                        value as string;

                    if (string.IsNullOrWhiteSpace(
                            name))
                    {
                        throw new ArgumentException(
                            "UI element name cannot be empty.",
                            nameof(value));
                    }

                    element.Name = name;
                },
                displayName: "Name",
                order: 0));

        properties.Add(
            new EditorProperty(
                "Type",
                typeof(string),
                () => element.Type.ToString(),
                displayName: "Type",
                order: 1));

        properties.Add(
           new EditorProperty(
               "AnchorX",
               typeof(float),
               () => element.Layout.Anchor.X,
               value =>
               {
                   var x =
                       Convert.ToSingle(
                           value,
                           System.Globalization.CultureInfo.InvariantCulture);

                   if (x < 0.0f ||
                       x > 1.0f)
                   {
                       throw new ArgumentOutOfRangeException(
                           nameof(value),
                           "Anchor must be between 0 and 1.");
                   }

                   element.Layout =
                       element.Layout with
                       {
                           Anchor =
                               new Vector2(
                                   x,
                                   element.Layout.Anchor.Y)
                       };
               },
               displayName: "Anchor X",
               order: 10));

        properties.Add(
            new EditorProperty(
                "AnchorY",
                typeof(float),
                () => element.Layout.Anchor.Y,
                value =>
                {
                    var y =
                        Convert.ToSingle(
                            value,
                            System.Globalization.CultureInfo.InvariantCulture);

                    if (y < 0.0f ||
                        y > 1.0f)
                    {
                        throw new ArgumentOutOfRangeException(
                            nameof(value),
                            "Anchor must be between 0 and 1.");
                    }

                    element.Layout =
                        element.Layout with
                        {
                            Anchor =
                                new Vector2(
                                    element.Layout.Anchor.X,
                                    y)
                        };
                },
                displayName: "Anchor Y",
                order: 11));

        properties.Add(
            new EditorProperty(
                "OffsetX",
                typeof(float),
                () => element.Layout.Offset.X,
                value =>
                {
                    var x =
                        Convert.ToSingle(
                            value,
                            System.Globalization.CultureInfo.InvariantCulture);

                    element.Layout =
                        element.Layout with
                        {
                            Offset =
                                new Vector2(
                                    x,
                                    element.Layout.Offset.Y)
                        };
                },
                displayName: "Offset X",
                order: 12));

        properties.Add(
            new EditorProperty(
                "OffsetY",
                typeof(float),
                () => element.Layout.Offset.Y,
                value =>
                {
                    var y =
                        Convert.ToSingle(
                            value,
                            System.Globalization.CultureInfo.InvariantCulture);

                    element.Layout =
                        element.Layout with
                        {
                            Offset =
                                new Vector2(
                                    element.Layout.Offset.X,
                                    y)
                        };
                },
                displayName: "Offset Y",
                order: 13));

        properties.Add(
            new EditorProperty(
                "Width",
                typeof(float),
                () => element.Layout.Size.X,
                value =>
                {
                    var width =
                        Convert.ToSingle(
                            value,
                            System.Globalization.CultureInfo.InvariantCulture);

                    if (width < 0.0f)
                    {
                        throw new ArgumentOutOfRangeException(
                            nameof(value),
                            "UI element width cannot be negative.");
                    }

                    element.Layout =
                        element.Layout with
                        {
                            Size =
                                new Vector2(
                                    width,
                                    element.Layout.Size.Y)
                        };
                },
                displayName: "Width",
                order: 14));

        properties.Add(
            new EditorProperty(
                "Height",
                typeof(float),
                () => element.Layout.Size.Y,
                value =>
                {
                    var height =
                        Convert.ToSingle(
                            value,
                            System.Globalization.CultureInfo.InvariantCulture);

                    if (height < 0.0f)
                    {
                        throw new ArgumentOutOfRangeException(
                            nameof(value),
                            "UI element height cannot be negative.");
                    }

                    element.Layout =
                        element.Layout with
                        {
                            Size =
                                new Vector2(
                                    element.Layout.Size.X,
                                    height)
                        };
                },
                displayName: "Height",
                order: 15));

        return properties
            .OrderBy(
                static property => property.Order)
            .ToArray();
    }
}